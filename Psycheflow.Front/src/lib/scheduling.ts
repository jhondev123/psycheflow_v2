import { type Database, type Schedule, type WorkingHour, ScheduleStatus, ScheduleType } from "@/types";
import { parseISODate } from "@/lib/format";
import { rangeContains, rangesOverlap, toMinutes } from "@/lib/time";

export interface ScheduleInput {
  date: string; // yyyy-MM-dd
  start: string; // HH:mm
  end: string; // HH:mm
  psychologistId: string;
  type: ScheduleType;
  patientId?: string | null;
  status?: ScheduleStatus;
  title?: string;
}

export function workingHoursForDay(hours: WorkingHour[], weekday: number): WorkingHour[] {
  return hours.filter((h) => h.dayOfWeek === weekday);
}

/**
 * Mirrors the API CreateScheduleUseCase business rules.
 * Returns an error message, or null when the input is valid.
 */
export function validateSchedule(
  db: Database,
  input: ScheduleInput,
  opts: { ignoreScheduleId?: string; allowPast?: boolean } = {},
): string | null {
  const start = toMinutes(input.start);
  const end = toMinutes(input.end);

  if (end <= start) {
    return "O horário final deve ser maior que o horário de início.";
  }

  if (!opts.allowPast) {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    if (parseISODate(input.date) < today) {
      return "A data do agendamento não pode ser anterior a hoje.";
    }
  }

  if (input.type === ScheduleType.SESSION && !input.patientId) {
    return "Selecione um paciente para a sessão.";
  }

  const psy = db.psychologists.find((p) => p.id === input.psychologistId);
  if (!psy) return "Psicólogo não encontrado.";

  // Working hours — BLOCK schedules are allowed outside working hours.
  if (input.type === ScheduleType.SESSION) {
    const weekday = parseISODate(input.date).getDay();
    const ranges = workingHoursForDay(psy.workingHours, weekday);
    if (ranges.length === 0) {
      return "O psicólogo não atende neste dia da semana.";
    }
    const fits = ranges.some((r) => rangeContains(toMinutes(r.start), toMinutes(r.end), start, end));
    if (!fits) {
      return "O horário está fora do expediente do psicólogo.";
    }
  }

  // Overlap with existing non-cancelled schedules.
  const clash = db.schedules.some((s) => {
    if (s.id === opts.ignoreScheduleId) return false;
    if (s.psychologistId !== input.psychologistId) return false;
    if (s.date !== input.date) return false;
    if (s.status === ScheduleStatus.Cancelled) return false;
    return rangesOverlap(start, end, toMinutes(s.start), toMinutes(s.end));
  });
  if (clash) {
    return "Este horário já possui outro agendamento.";
  }

  return null;
}

export function schedulesForPsychologist(db: Database, psychologistId: string): Schedule[] {
  return db.schedules.filter((s) => s.psychologistId === psychologistId);
}
