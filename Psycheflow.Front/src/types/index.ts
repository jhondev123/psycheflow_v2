/* ============================================================
   Domain types — mirror the Psycheflow C# API.
   A few extra UI-only fields (name, phone, cpf, notes, title)
   are added; the API stores UserName=email, so we enrich it.
   ============================================================ */

export type Role = "Admin" | "Manager" | "Psychologist" | "Patient";

export type ApproachType = "None" | "PSYCHOANALYSIS" | "BEHAVIORAL";

/** Maps to API ScheduleStatus (int). */
export enum ScheduleStatus {
  Pending = 0,
  Confirmed = 1,
  Cancelled = 2,
}

/** Maps to API ScheduleTypes (int). */
export enum ScheduleType {
  SESSION = 0,
  BLOCK = 1,
}

/** Maps to API SessionStatus (int). */
export enum SessionStatus {
  Scheduled = 0,
  InProgress = 1,
  Completed = 2,
  NoShow = 3,
}

export interface Company {
  id: string;
  name: string;
}

export interface User {
  id: string;
  name: string;
  email: string;
  role: Role;
  companyId: string;
  birthDate?: string; // ISO yyyy-MM-dd
  createdAt: string;
}

/** A single weekday working range for a psychologist. */
export interface WorkingHour {
  dayOfWeek: number; // 0=Sun .. 6=Sat (JS getDay)
  start: string; // "HH:mm"
  end: string; // "HH:mm"
}

export interface Psychologist {
  id: string;
  userId: string;
  name: string;
  email: string;
  licenseNumber: string; // CRP — numeric per API LicenseNumber VO
  approach: ApproachType;
  companyId: string;
  phone?: string;
  workingHours: WorkingHour[];
}

export interface Patient {
  id: string;
  userId?: string;
  name: string;
  email: string;
  cpf?: string;
  phone?: string;
  birthDate?: string; // ISO yyyy-MM-dd
  companyId: string;
  notes?: string;
  createdAt: string;
}

export interface Schedule {
  id: string;
  date: string; // ISO yyyy-MM-dd
  start: string; // "HH:mm"
  end: string; // "HH:mm"
  psychologistId: string;
  type: ScheduleType;
  status: ScheduleStatus;
  companyId: string;
  sessionId?: string | null;
  /** Label for BLOCK schedules (e.g. "Almoço", "Indisponível"). */
  title?: string;
}

export interface Session {
  id: string;
  scheduleId: string;
  psychologistId: string;
  patientId: string;
  companyId: string;
  description: string;
  feedback: string;
  status: SessionStatus;
  createdAt: string;
}

export interface DocumentField {
  id: string;
  name: string;
  order: number;
  isRequired: boolean;
  defaultValue?: string | null;
}

export interface DocumentTemplate {
  id: string;
  name: string;
  description: string;
  templateName: string;
  companyId?: string | null;
  fields: DocumentField[];
}

/** Full in-memory database shape persisted to localStorage. */
export interface Database {
  companies: Company[];
  users: User[];
  psychologists: Psychologist[];
  patients: Patient[];
  schedules: Schedule[];
  sessions: Session[];
  documents: DocumentTemplate[];
  /** email -> password (mock auth only). */
  credentials: Record<string, string>;
}
