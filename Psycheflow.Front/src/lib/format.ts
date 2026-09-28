import { format, isToday, isTomorrow, isYesterday } from "date-fns";
import { ptBR } from "date-fns/locale";

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/** yyyy-MM-dd de uma data local (sem deslocamento de fuso). */
export function isoDate(d: Date): string {
  return format(d, "yyyy-MM-dd");
}

/** Lê yyyy-MM-dd como data *local* (evita pular um dia pelo fuso). */
export function parseISODate(value: string): Date {
  const [y, m, d] = value.slice(0, 10).split("-").map(Number);
  return new Date(y, m - 1, d);
}

/** "14:00:00" → "14:00". */
export function hm(time: string): string {
  return time.slice(0, 5);
}

export function fmtDateLong(value: string): string {
  return format(parseISODate(value), "EEEE, d 'de' MMMM", { locale: ptBR });
}

export function fmtDateShort(value: string): string {
  return format(parseISODate(value), "dd/MM/yyyy", { locale: ptBR });
}

export function fmtDateTime(value: string): string {
  return format(new Date(value), "dd/MM/yyyy 'às' HH:mm", { locale: ptBR });
}

export function fmtRelativeDay(value: string): string {
  const d = parseISODate(value);
  if (isToday(d)) return "Hoje";
  if (isTomorrow(d)) return "Amanhã";
  if (isYesterday(d)) return "Ontem";
  return format(d, "EEE, dd/MM", { locale: ptBR });
}

const currency = new Intl.NumberFormat("pt-BR", { style: "currency", currency: "BRL" });

export function fmtMoney(value: number | null | undefined): string {
  return value === null || value === undefined ? "—" : currency.format(value);
}

export function fmtBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

export function fmtCpf(cpf: string): string {
  const d = cpf.replace(/\D/g, "");
  return d.length === 11 ? `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}` : cpf;
}

export function fmtPhone(phone: string): string {
  const d = phone.replace(/\D/g, "");
  if (d.length === 11) return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  if (d.length === 10) return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  return phone;
}

export function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}

/** Aceita "150", "150,5" ou "150.50" e devolve número (ou null se vazio/inválido). */
export function parseMoney(value: string): number | null {
  const normalized = value.trim().replace(/\./g, "").replace(",", ".");
  if (!normalized) return null;
  const n = Number(normalized);
  return Number.isFinite(n) ? n : null;
}
