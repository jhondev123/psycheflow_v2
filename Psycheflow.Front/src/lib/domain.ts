import type {
  AccessAction,
  AiProvider,
  ApproachType,
  DayOfWeek,
  PatientStatus,
  PaymentMethod,
  PaymentStatus,
  RecurrenceType,
  ReportStatus,
  ReportTemplate,
  Role,
  ScheduleStatus,
  SessionStatus,
} from "@/types";

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

/** Dias da API na ordem do JS (`Date.getDay()`: 0 = domingo). */
export const DAYS: DayOfWeek[] = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export function dayIndex(day: DayOfWeek): number {
  return DAYS.indexOf(day);
}

export interface Meta {
  label: string;
  badge: string; // classe CSS do badge
  color: string; // variável CSS usada na agenda
}

export const scheduleStatusMeta: Record<ScheduleStatus, Meta> = {
  Pending: { label: "A confirmar", badge: "badge-warning", color: "var(--warning)" },
  Confirmed: { label: "Confirmada", badge: "badge-success", color: "var(--success)" },
  Cancelled: { label: "Cancelada", badge: "badge-danger", color: "var(--danger)" },
};

export const sessionStatusMeta: Record<SessionStatus, Meta> = {
  Scheduled: { label: "Agendada", badge: "badge-info", color: "var(--info)" },
  Completed: { label: "Concluída", badge: "badge-success", color: "var(--success)" },
  Cancelled: { label: "Cancelada", badge: "badge-danger", color: "var(--danger)" },
  NoShow: { label: "Falta", badge: "badge-warning", color: "var(--warning)" },
};

export const paymentStatusMeta: Record<PaymentStatus, Meta> = {
  Pending: { label: "Pendente", badge: "badge-warning", color: "var(--warning)" },
  Paid: { label: "Pago", badge: "badge-success", color: "var(--success)" },
  Cancelled: { label: "Cancelado", badge: "badge-danger", color: "var(--danger)" },
};

export const patientStatusMeta: Record<PatientStatus, Meta> = {
  Active: { label: "Ativo", badge: "badge-success", color: "var(--success)" },
  Inactive: { label: "Inativo", badge: "", color: "var(--text-soft)" },
};

export const reportStatusMeta: Record<ReportStatus, Meta> = {
  Draft: { label: "Rascunho", badge: "badge-warning", color: "var(--warning)" },
  Finalized: { label: "Finalizado", badge: "badge-success", color: "var(--success)" },
};

export const paymentMethodLabel: Record<PaymentMethod, string> = {
  Pix: "Pix",
  CreditCard: "Cartão de crédito",
  DebitCard: "Cartão de débito",
};

export const recurrenceTypeLabel: Record<RecurrenceType, string> = {
  Weekly: "Semanal",
  Monthly: "Mensal",
};

export const reportTemplateLabel: Record<ReportTemplate, string> = {
  PsychologicalReport: "Laudo psicológico",
  PsychologicalStatement: "Relatório psicológico",
};

export const approachLabel: Record<ApproachType, string> = {
  NotInformed: "Não informada",
  CognitiveBehavioral: "Cognitivo-comportamental",
  Psychoanalysis: "Psicanálise",
  Behavioral: "Comportamental",
  Humanistic: "Humanista",
  Gestalt: "Gestalt-terapia",
  Systemic: "Sistêmica",
  Other: "Outra",
};

export const APPROACHES = Object.keys(approachLabel) as ApproachType[];

export const roleLabel: Record<Role, string> = {
  Admin: "Administrador(a)",
  Manager: "Gestor(a)",
  Psychologist: "Psicólogo(a)",
  Patient: "Paciente",
};

export const aiProviderLabel: Record<AiProvider, string> = {
  Claude: "Claude (Anthropic)",
  OpenAi: "ChatGPT (OpenAI)",
  Gemini: "Gemini (Google)",
};

export const accessActionLabel: Record<AccessAction, string> = {
  Viewed: "Leitura",
  AttachmentDownloaded: "Download de anexo",
  Denied: "Acesso negado",
};

/** Rótulo principal do usuário (o primeiro papel de maior nível). */
export function mainRole(roles: Role[]): string {
  const order: Role[] = ["Admin", "Manager", "Psychologist", "Patient"];
  const role = order.find((r) => roles.includes(r));
  return role ? roleLabel[role] : "";
}
