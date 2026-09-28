/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Endereço da API (sem /api/v1). Padrão: http://localhost:8080. */
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
