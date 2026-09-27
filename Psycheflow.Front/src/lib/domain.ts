import { type ApproachType, type Role, ScheduleStatus, ScheduleType, SessionStatus } from "@/types";

export const WEEKDAYS_SHORT = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];
export const WEEKDAYS_LONG = [
  "Domingo",
  "Segunda-feira",
  "Terça-feira",
  "Quarta-feira",
  "Quinta-feira",
  "Sexta-feira",
  "Sábado",
];

interface Meta {
  label: string;
  badge: string; // CSS badge class
  color: string; // CSS var used by the calendar event
}

export const scheduleStatusMeta: Record<ScheduleStatus, Meta> = {
  [ScheduleStatus.Pending]: { label: "Pendente", badge: "badge-warning", color: "var(--warning)" },
  [ScheduleStatus.Confirmed]: { label: "Confirmado", badge: "badge-success", color: "var(--success)" },
  [ScheduleStatus.Cancelled]: { label: "Cancelado", badge: "badge-danger", color: "var(--danger)" },
};

export const sessionStatusMeta: Record<SessionStatus, Meta> = {
  [SessionStatus.Scheduled]: { label: "Agendada", badge: "badge-info", color: "var(--info)" },
  [SessionStatus.InProgress]: { label: "Em andamento", badge: "badge-brand", color: "var(--brand-500)" },
  [SessionStatus.Completed]: { label: "Concluída", badge: "badge-success", color: "var(--success)" },
  [SessionStatus.NoShow]: { label: "Não compareceu", badge: "badge-danger", color: "var(--danger)" },
};

export function scheduleTypeLabel(type: ScheduleType): string {
  return type === ScheduleType.SESSION ? "Sessão" : "Bloqueio";
}

export const approachMeta: Record<ApproachType, string> = {
  None: "Não definida",
  PSYCHOANALYSIS: "Psicanálise",
  BEHAVIORAL: "Comportamental",
};

export const roleMeta: Record<Role, string> = {
  Admin: "Administrador",
  Manager: "Gestor",
  Psychologist: "Psicólogo(a)",
  Patient: "Paciente",
};
