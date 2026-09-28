/* ============================================================
   Cliente HTTP da API Psycheflow.
   - Base: VITE_API_URL (padrão http://localhost:8080) + /api/v1
   - Token JWT guardado no localStorage e enviado como Bearer
   - Erros chegam como ProblemDetails (RFC 9457) e viram ApiError
   ============================================================ */

const BASE_URL = `${(import.meta.env.VITE_API_URL ?? "http://localhost:8080").replace(/\/$/, "")}/api/v1`;
const TOKEN_KEY = "psycheflow.token.v1";

let unauthorizedHandler: (() => void) | null = null;

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null): void {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

/** Chamado quando a API responde 401 (token expirado/inválido): a store faz logout. */
export function onUnauthorized(handler: (() => void) | null): void {
  unauthorizedHandler = handler;
}

/** Erro da API com a mensagem em pt-BR, o `code` estável e os erros por campo (camelCase). */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly code: string | null = null,
    readonly fields: Record<string, string> = {},
  ) {
    super(message);
    this.name = "ApiError";
  }

  /** Mensagem do campo (aceita "address.zipCode" ou só o nome). */
  field(name: string): string | undefined {
    return this.fields[name];
  }
}

/** Mensagem amigável para qualquer erro (ApiError, rede ou desconhecido). */
export function errorMessage(error: unknown, fallback = "Não foi possível concluir a operação."): string {
  if (error instanceof ApiError) return error.message;
  if (error instanceof TypeError) return "Não foi possível falar com o servidor. Verifique se a API está no ar.";
  return fallback;
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
}

async function toApiError(response: Response): Promise<ApiError> {
  let problem: ProblemDetails = {};
  try {
    problem = (await response.json()) as ProblemDetails;
  } catch {
    /* corpo vazio ou não-JSON */
  }

  const fields: Record<string, string> = {};
  for (const [key, messages] of Object.entries(problem.errors ?? {})) {
    if (messages.length > 0) fields[key] = messages[0];
  }

  const firstField = Object.values(fields)[0];
  const message =
    response.status === 422 && firstField && Object.keys(fields).length === 1
      ? firstField
      : problem.detail ?? problem.title ?? defaultMessage(response.status);

  return new ApiError(message, response.status, problem.code ?? null, fields);
}

function defaultMessage(status: number): string {
  switch (status) {
    case 401:
      return "Sua sessão expirou. Entre novamente.";
    case 403:
      return "Você não tem permissão para esta ação.";
    case 404:
      return "Registro não encontrado.";
    case 429:
      return "Muitas tentativas em pouco tempo. Aguarde um instante.";
    default:
      return "Não foi possível concluir a operação.";
  }
}

type Query = Record<string, string | number | boolean | null | undefined>;

function buildUrl(path: string, query?: Query): string {
  const url = new URL(`${BASE_URL}${path}`);
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== "") url.searchParams.set(key, String(value));
  }
  return url.toString();
}

async function send(method: string, path: string, options: { query?: Query; body?: unknown; form?: FormData } = {}): Promise<Response> {
  const headers: Record<string, string> = { Accept: "application/json, application/pdf, */*" };
  const token = getToken();
  if (token) headers.Authorization = `Bearer ${token}`;

  let body: BodyInit | undefined;
  if (options.form) {
    body = options.form;
  } else if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
    body = JSON.stringify(options.body);
  }

  const response = await fetch(buildUrl(path, options.query), { method, headers, body });
  if (response.ok) return response;

  const error = await toApiError(response);
  if (response.status === 401 && token) unauthorizedHandler?.();
  throw error;
}

async function json<T>(response: Response): Promise<T> {
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  get: async <T>(path: string, query?: Query) => json<T>(await send("GET", path, { query })),
  post: async <T = void>(path: string, body?: unknown) => json<T>(await send("POST", path, { body: body ?? {} })),
  put: async <T = void>(path: string, body: unknown) => json<T>(await send("PUT", path, { body })),
  del: async (path: string) => {
    await send("DELETE", path);
  },
  upload: async <T>(path: string, file: File, field = "file") => {
    const form = new FormData();
    form.append(field, file);
    return json<T>(await send("POST", path, { form }));
  },

  /** Baixa um arquivo (PDF, anexo) e dispara o download no navegador. */
  download: async (path: string, query?: Query, fallbackName = "documento.pdf") => {
    const response = await send("GET", path, { query });
    const blob = await response.blob();
    const name = fileNameFrom(response.headers.get("Content-Disposition")) ?? fallbackName;
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = name;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 1000);
  },
};

function fileNameFrom(disposition: string | null): string | null {
  if (!disposition) return null;
  const utf8 = /filename\*=UTF-8''([^;]+)/i.exec(disposition);
  if (utf8) return decodeURIComponent(utf8[1]);
  const plain = /filename="?([^";]+)"?/i.exec(disposition);
  return plain ? plain[1] : null;
}
