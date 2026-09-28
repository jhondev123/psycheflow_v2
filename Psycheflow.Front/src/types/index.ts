/* ============================================================
   Tipos do contrato da API Psycheflow (/api/v1).
   JSON em camelCase, enums como texto, datas "yyyy-MM-dd",
   horas "HH:mm:ss" (a API aceita "HH:mm" na entrada).
   ============================================================ */

export type Role = "Admin" | "Manager" | "Psychologist" | "Patient";

export type ApproachType =
  | "NotInformed"
  | "CognitiveBehavioral"
  | "Psychoanalysis"
  | "Behavioral"
  | "Humanistic"
  | "Gestalt"
  | "Systemic"
  | "Other";

export type DayOfWeek = "Sunday" | "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday";
export type ScheduleStatus = "Pending" | "Confirmed" | "Cancelled";
export type ScheduleType = "Session" | "Block";
export type SessionStatus = "Scheduled" | "Completed" | "Cancelled" | "NoShow";
export type PatientStatus = "Active" | "Inactive";
export type PaymentStatus = "Pending" | "Paid" | "Cancelled";
export type PaymentMethod = "CreditCard" | "DebitCard" | "Pix";
export type RecurrenceType = "Weekly" | "Monthly";
export type ReportTemplate = "PsychologicalReport" | "PsychologicalStatement";
export type ReportStatus = "Draft" | "Finalized";
export type AiProvider = "Claude" | "OpenAi" | "Gemini";
export type AccessAction = "Viewed" | "AttachmentDownloaded" | "Denied";

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/* ---------- Auth / usuários ---------- */
export interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  mustChangePassword: boolean;
}

export interface Me {
  id: string;
  fullName: string;
  email: string;
  roles: Role[];
  companyId: string;
  companyName: string;
  psychologistId: string | null;
  mustChangePassword: boolean;
}

export interface RegisterInput {
  companyName: string;
  fullName: string;
  email: string;
  password: string;
  licenseNumber: string;
  approach: ApproachType;
  phone?: string | null;
}

export interface UserItem {
  id: string;
  fullName: string;
  email: string;
  roles: Role[];
  psychologistId: string | null;
  mustChangePassword: boolean;
  isLockedOut: boolean;
}

export interface CreatedUser {
  id: string;
  fullName: string;
  email: string;
  role: Role;
  psychologistId: string | null;
  temporaryPassword: string;
}

/* ---------- Configurações ---------- */
export interface Settings {
  sessionDurationMinutes: number;
  sessionDefaultPrice: number | null;
  timeZone: string;
}

/* ---------- Psicólogos ---------- */
export interface WorkingHour {
  dayOfWeek: DayOfWeek;
  startTime: string;
  endTime: string;
}

export interface Psychologist {
  id: string;
  userId: string;
  fullName: string;
  email: string;
  licenseNumber: string;
  approach: ApproachType;
  phone: string | null;
  workingHours: WorkingHour[];
}

/* ---------- Pacientes ---------- */
export interface Address {
  zipCode: string;
  street: string;
  number: string;
  complement: string | null;
  neighborhood: string;
  city: string;
  state: string;
}

export interface PatientListItem {
  id: string;
  fullName: string;
  cpf: string;
  email: string;
  phone: string;
  status: PatientStatus;
  lastSessionDate: string | null;
}

export interface Patient {
  id: string;
  fullName: string;
  cpf: string;
  email: string;
  phone: string;
  birthDate: string | null;
  status: PatientStatus;
  address: Address | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
}

/* ---------- Agenda e sessões ---------- */
export interface AgendaItem {
  scheduleId: string;
  psychologistId: string;
  type: ScheduleType;
  status: ScheduleStatus;
  date: string;
  startTime: string;
  endTime: string;
  blockReason: string | null;
  sessionId: string | null;
  patientId: string | null;
  patientName: string | null;
  sessionStatus: SessionStatus | null;
}

export interface Agenda {
  from: string;
  to: string;
  workingHours: { psychologistId: string; fullName: string; hours: WorkingHour[] }[];
  items: AgendaItem[];
}

export interface SessionListItem {
  id: string;
  psychologistId: string;
  psychologistName: string;
  patientId: string;
  patientName: string;
  date: string;
  startTime: string;
  endTime: string;
  status: SessionStatus;
  scheduleStatus: ScheduleStatus;
}

export interface SessionPaymentSummary {
  id: string;
  amount: number;
  status: PaymentStatus;
}

export interface Session {
  id: string;
  scheduleId: string;
  psychologistId: string;
  psychologistName: string;
  patientId: string;
  patientName: string;
  date: string;
  startTime: string;
  endTime: string;
  durationMinutes: number;
  status: SessionStatus;
  scheduleStatus: ScheduleStatus;
  notes: string | null;
  feedbackScore: number | null;
  feedbackComment: string | null;
  cancellationReason: string | null;
  rescheduleReason: string | null;
  clinicalNotesVisible: boolean;
  recurrenceId: string | null;
  payment: SessionPaymentSummary | null;
  createdAt: string;
  updatedAt: string | null;
}

/* ---------- Financeiro e recorrência ---------- */
export interface Payment {
  id: string;
  sessionId: string;
  patientId: string;
  patientName: string;
  psychologistId: string;
  sessionDate: string;
  sessionStartTime: string;
  sessionStatus: SessionStatus;
  amount: number;
  status: PaymentStatus;
  method: PaymentMethod | null;
  paidAt: string | null;
  notes: string | null;
  cancellationReason: string | null;
}

export interface Recurrence {
  id: string;
  patientId: string;
  psychologistId: string;
  type: RecurrenceType;
  startDate: string;
  endDate: string | null;
  startTime: string;
  durationMinutes: number;
  price: number;
  generatedUntil: string;
  isActive: boolean;
  endReason: string | null;
}

export interface RecurrenceGeneration {
  recurrence: Recurrence;
  scheduled: string[];
  skipped: { date: string; code: string; reason: string }[];
}

/* ---------- Laudos e prontuários ---------- */
export interface PsychologicalReport {
  id: string;
  patientId: string;
  patientName: string;
  psychologistId: string;
  template: ReportTemplate;
  purpose: string | null;
  demand: string | null;
  procedure: string | null;
  analysis: string | null;
  conclusion: string | null;
  includeSessionSummary: boolean;
  status: ReportStatus;
  finalizedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

export interface MedicalRecordListItem {
  id: string;
  patientId: string;
  patientName: string;
  title: string;
  excerpt: string;
  attachmentsCount: number;
  createdAt: string;
  updatedAt: string | null;
}

export interface MedicalRecord {
  id: string;
  patientId: string;
  patientName: string;
  psychologistId: string;
  title: string;
  content: string;
  attachments: Attachment[];
  createdAt: string;
  updatedAt: string | null;
}

export interface AccessEntry {
  at: string;
  userId: string;
  userName: string;
  action: AccessAction;
  attachmentId: string | null;
}

/* ---------- IA ---------- */
export interface AiSettings {
  isEnabled: boolean;
  provider: AiProvider;
  shareSessionNotes: boolean;
  shareFeedbacks: boolean;
  shareMedicalRecords: boolean;
  consentAcceptedAt: string | null;
  availableProviders: AiProvider[];
}

export interface AiSuggestion {
  suggestion: string;
  provider: AiProvider;
  model: string;
  generatedAt: string;
  disclaimer: string;
}

/* ---------- Painel ---------- */
export interface Dashboard {
  today: string;
  weekStart: string;
  weekEnd: string;
  activePatients: number;
  sessionsToday: number;
  confirmedToday: number;
  sessionsThisWeek: number;
  pendingConfirmations: number;
  finance: { pendingPayments: number; pendingAmount: number; receivedThisMonth: number };
  todayItems: Omit<AgendaItem, "date">[];
  upcoming: {
    sessionId: string;
    psychologistId: string;
    patientId: string;
    patientName: string;
    date: string;
    startTime: string;
    endTime: string;
    scheduleStatus: ScheduleStatus;
  }[];
}
