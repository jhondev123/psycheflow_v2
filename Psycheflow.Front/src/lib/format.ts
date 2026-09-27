import { format, isToday, isTomorrow, isYesterday } from "date-fns";
import { ptBR } from "date-fns/locale";

export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

/** Parse an ISO yyyy-MM-dd string as a *local* date (avoids TZ shifts). */
export function parseISODate(isoDate: string): Date {
  const [y, m, d] = isoDate.split("-").map(Number);
  return new Date(y, m - 1, d);
}

export function fmtDateLong(isoDate: string): string {
  return format(parseISODate(isoDate), "EEEE, d 'de' MMMM", { locale: ptBR });
}

export function fmtDateShort(isoDate: string): string {
  return format(parseISODate(isoDate), "dd/MM/yyyy", { locale: ptBR });
}

export function fmtRelativeDay(isoDate: string): string {
  const d = parseISODate(isoDate);
  if (isToday(d)) return "Hoje";
  if (isTomorrow(d)) return "Amanhã";
  if (isYesterday(d)) return "Ontem";
  return format(d, "EEE, dd/MM", { locale: ptBR });
}

export function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
