import { addDays, format, startOfWeek } from "date-fns";
import {
  type Database,
  type Patient,
  type Schedule,
  type Session,
  ScheduleStatus,
  ScheduleType,
  SessionStatus,
} from "@/types";

const uid = () => crypto.randomUUID();
const iso = (d: Date) => format(d, "yyyy-MM-dd");

/** Builds a fresh demo database with a lively current-week agenda. */
export function buildSeed(): Database {
  const companyId = uid();

  const adminUserId = uid();
  const psyUserId = uid();

  const psychologistId = uid();

  // --- Patients ---------------------------------------------------------
  const patientSeeds: Array<Omit<Patient, "id" | "companyId" | "createdAt">> = [
    { name: "Mariana Lopes", email: "mariana.lopes@email.com", cpf: "529.982.247-25", phone: "(11) 98877-1020", birthDate: "1994-03-12", notes: "Ansiedade — sessões semanais." },
    { name: "Rafael Andrade", email: "rafael.andrade@email.com", cpf: "390.533.447-05", phone: "(11) 99120-3344", birthDate: "1988-07-22", notes: "Acompanhamento de luto." },
    { name: "Beatriz Carvalho", email: "bia.carvalho@email.com", cpf: "111.444.777-35", phone: "(21) 98140-5566", birthDate: "2001-11-02" },
    { name: "Gustavo Pereira", email: "gustavo.pereira@email.com", cpf: "457.301.890-06", phone: "(31) 98801-7788", birthDate: "1979-01-30", notes: "Terapia de casal." },
    { name: "Helena Souza", email: "helena.souza@email.com", cpf: "168.995.350-09", phone: "(11) 97000-9911", birthDate: "1996-09-18" },
    { name: "Lucas Martins", email: "lucas.martins@email.com", cpf: "263.946.532-87", phone: "(47) 99888-1212", birthDate: "1992-05-05", notes: "Primeira consulta." },
  ];

  const patients: Patient[] = patientSeeds.map((p) => ({
    ...p,
    id: uid(),
    companyId,
    createdAt: new Date().toISOString(),
  }));

  // --- Schedules + Sessions for the current week ------------------------
  const weekStart = startOfWeek(new Date(), { weekStartsOn: 0 });
  const day = (offset: number) => addDays(weekStart, offset);

  const schedules: Schedule[] = [];
  const sessions: Session[] = [];

  const addSession = (
    dayOffset: number,
    start: string,
    end: string,
    patientIndex: number,
    scheduleStatus: ScheduleStatus,
    sessionStatus: SessionStatus,
    extra?: Partial<Session>,
  ) => {
    const scheduleId = uid();
    const sessionId = uid();
    schedules.push({
      id: scheduleId,
      date: iso(day(dayOffset)),
      start,
      end,
      psychologistId,
      type: ScheduleType.SESSION,
      status: scheduleStatus,
      companyId,
      sessionId,
    });
    sessions.push({
      id: sessionId,
      scheduleId,
      psychologistId,
      patientId: patients[patientIndex].id,
      companyId,
      description: extra?.description ?? "",
      feedback: extra?.feedback ?? "",
      status: sessionStatus,
      createdAt: new Date().toISOString(),
    });
  };

  const addBlock = (dayOffset: number, start: string, end: string, title: string) => {
    schedules.push({
      id: uid(),
      date: iso(day(dayOffset)),
      start,
      end,
      psychologistId,
      type: ScheduleType.BLOCK,
      status: ScheduleStatus.Confirmed,
      companyId,
      title,
    });
  };

  // Monday
  addSession(1, "09:00", "10:00", 0, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  addSession(1, "11:00", "12:00", 1, ScheduleStatus.Pending, SessionStatus.Scheduled);
  addSession(1, "15:00", "16:00", 4, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  // Tuesday
  addSession(2, "10:00", "11:00", 2, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  addSession(2, "14:00", "15:00", 3, ScheduleStatus.Pending, SessionStatus.Scheduled);
  // Wednesday
  addSession(3, "09:00", "10:00", 5, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  addSession(3, "16:00", "17:00", 0, ScheduleStatus.Cancelled, SessionStatus.NoShow);
  // Thursday
  addSession(4, "11:00", "12:00", 1, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  addSession(4, "14:00", "15:00", 4, ScheduleStatus.Confirmed, SessionStatus.Scheduled);
  // Friday
  addSession(5, "10:00", "11:00", 2, ScheduleStatus.Pending, SessionStatus.Scheduled);

  // Lunch blocks Mon–Fri
  for (let d = 1; d <= 5; d++) addBlock(d, "12:00", "13:00", "Almoço");

  // A couple of completed sessions last week (history)
  const lastWeek = (offset: number) => addDays(weekStart, offset - 7);
  const pushHistory = (dayOffset: number, start: string, end: string, patientIndex: number, feedback: string, description: string) => {
    const scheduleId = uid();
    const sessionId = uid();
    schedules.push({
      id: scheduleId,
      date: iso(lastWeek(dayOffset)),
      start,
      end,
      psychologistId,
      type: ScheduleType.SESSION,
      status: ScheduleStatus.Confirmed,
      companyId,
      sessionId,
    });
    sessions.push({
      id: sessionId,
      scheduleId,
      psychologistId,
      patientId: patients[patientIndex].id,
      companyId,
      description,
      feedback,
      status: SessionStatus.Completed,
      createdAt: new Date().toISOString(),
    });
  };
  pushHistory(1, "09:00", "10:00", 0, "Boa evolução, paciente mais tranquila.", "Trabalhamos técnicas de respiração e reestruturação cognitiva.");
  pushHistory(3, "16:00", "17:00", 3, "Casal engajado nos exercícios propostos.", "Sessão de terapia de casal — comunicação não-violenta.");

  return {
    companies: [{ id: companyId, name: "Clínica Bem-Estar" }],
    users: [
      { id: adminUserId, name: "Administração", email: "admin@psycheflow.com", role: "Admin", companyId, createdAt: new Date().toISOString() },
      { id: psyUserId, name: "Dra. Ana Beatriz", email: "ana@psycheflow.com", role: "Psychologist", companyId, createdAt: new Date().toISOString() },
    ],
    psychologists: [
      {
        id: psychologistId,
        userId: psyUserId,
        name: "Dra. Ana Beatriz",
        email: "ana@psycheflow.com",
        licenseNumber: "06123",
        approach: "PSYCHOANALYSIS",
        companyId,
        phone: "(11) 99632-1010",
        workingHours: [
          { dayOfWeek: 1, start: "08:00", end: "18:00" },
          { dayOfWeek: 2, start: "08:00", end: "18:00" },
          { dayOfWeek: 3, start: "08:00", end: "18:00" },
          { dayOfWeek: 4, start: "08:00", end: "18:00" },
          { dayOfWeek: 5, start: "08:00", end: "17:00" },
          { dayOfWeek: 6, start: "09:00", end: "12:00" },
        ],
      },
    ],
    patients,
    schedules,
    sessions,
    documents: [
      {
        id: uid(),
        name: "Atestado de Comparecimento",
        description: "Comprova a presença do paciente na sessão.",
        templateName: "atestado.frx",
        companyId: null,
        fields: [
          { id: uid(), name: "paciente", order: 1, isRequired: true },
          { id: uid(), name: "data", order: 2, isRequired: true },
          { id: uid(), name: "horario", order: 3, isRequired: false, defaultValue: "" },
        ],
      },
      {
        id: uid(),
        name: "Declaração de Acompanhamento Psicológico",
        description: "Declara que o paciente está em acompanhamento.",
        templateName: "declaracao.frx",
        companyId: null,
        fields: [
          { id: uid(), name: "paciente", order: 1, isRequired: true },
          { id: uid(), name: "crp", order: 2, isRequired: true, defaultValue: "06/123" },
          { id: uid(), name: "periodo", order: 3, isRequired: false },
        ],
      },
    ],
    credentials: {
      "ana@psycheflow.com": "Test123$",
      "admin@psycheflow.com": "Test123$",
    },
  };
}
