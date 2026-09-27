import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  type ApproachType,
  type Database,
  type Patient,
  type Psychologist,
  type Schedule,
  type Session,
  type User,
  ScheduleStatus,
  ScheduleType,
  SessionStatus,
} from "@/types";
import { buildSeed } from "@/data/seed";
import { validateSchedule, type ScheduleInput } from "@/lib/scheduling";

const DB_KEY = "psycheflow.db.v1";
const SESSION_KEY = "psycheflow.session.v1";
const THEME_KEY = "psycheflow.theme.v1";

type Theme = "light" | "dark";
type ToastType = "success" | "error" | "info";
interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

interface Result {
  ok: boolean;
  error?: string;
}

export interface RegisterInput {
  name: string;
  email: string;
  password: string;
  licenseNumber: string;
  approach: ApproachType;
}

export interface PatientInput {
  name: string;
  email: string;
  cpf?: string;
  phone?: string;
  birthDate?: string;
  notes?: string;
}

export interface ProfileInput {
  name: string;
  phone?: string;
  approach: ApproachType;
  licenseNumber: string;
}

interface AppStoreValue {
  db: Database;
  currentUser: User | null;
  activePsychologist: Psychologist | null;
  theme: Theme;
  toasts: Toast[];

  // auth
  login: (email: string, password: string) => Result;
  register: (input: RegisterInput) => Result;
  logout: () => void;

  // patients
  addPatient: (input: PatientInput) => Patient;
  updatePatient: (id: string, input: PatientInput) => void;
  deletePatient: (id: string) => void;

  // schedules
  createSchedule: (input: ScheduleInput) => Result & { schedule?: Schedule };
  setScheduleStatus: (id: string, status: ScheduleStatus) => void;
  moveSchedule: (id: string, date: string, start: string, end: string) => Result;
  deleteSchedule: (id: string) => void;

  // sessions
  completeSession: (sessionId: string, feedback: string, description: string) => void;
  setSessionStatus: (sessionId: string, status: SessionStatus) => void;

  // psychologist
  setWorkingHours: (psychologistId: string, hours: Psychologist["workingHours"]) => void;
  updateProfile: (input: ProfileInput) => void;

  // misc
  notify: (type: ToastType, message: string) => void;
  dismissToast: (id: string) => void;
  toggleTheme: () => void;
  resetData: () => void;
}

const AppStoreContext = createContext<AppStoreValue | null>(null);

function loadDb(): Database {
  try {
    const raw = localStorage.getItem(DB_KEY);
    if (raw) return JSON.parse(raw) as Database;
  } catch {
    /* fall through to seed */
  }
  const seed = buildSeed();
  localStorage.setItem(DB_KEY, JSON.stringify(seed));
  return seed;
}

function loadTheme(): Theme {
  const stored = localStorage.getItem(THEME_KEY);
  return stored === "dark" ? "dark" : "light";
}

export function AppStoreProvider({ children }: { children: ReactNode }) {
  const [db, setDb] = useState<Database>(loadDb);
  const [currentUserId, setCurrentUserId] = useState<string | null>(() =>
    localStorage.getItem(SESSION_KEY),
  );
  const [theme, setTheme] = useState<Theme>(loadTheme);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const timers = useRef<Record<string, number>>({});

  // Persist on change.
  useEffect(() => {
    localStorage.setItem(DB_KEY, JSON.stringify(db));
  }, [db]);

  useEffect(() => {
    if (currentUserId) localStorage.setItem(SESSION_KEY, currentUserId);
    else localStorage.removeItem(SESSION_KEY);
  }, [currentUserId]);

  useEffect(() => {
    localStorage.setItem(THEME_KEY, theme);
    document.documentElement.setAttribute("data-theme", theme);
  }, [theme]);

  const currentUser = useMemo(
    () => db.users.find((u) => u.id === currentUserId) ?? null,
    [db.users, currentUserId],
  );

  const activePsychologist = useMemo(() => {
    if (!currentUser) return null;
    return (
      db.psychologists.find((p) => p.userId === currentUser.id) ??
      db.psychologists.find((p) => p.companyId === currentUser.companyId) ??
      null
    );
  }, [db.psychologists, currentUser]);

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const notify = useCallback(
    (type: ToastType, message: string) => {
      const id = crypto.randomUUID();
      setToasts((prev) => [...prev, { id, type, message }]);
      timers.current[id] = window.setTimeout(() => dismissToast(id), 3600);
    },
    [dismissToast],
  );

  useEffect(() => {
    const t = timers.current;
    return () => {
      Object.values(t).forEach((handle) => clearTimeout(handle));
    };
  }, []);

  // ---- Auth ----------------------------------------------------------
  const login = useCallback(
    (email: string, password: string): Result => {
      const key = email.trim().toLowerCase();
      const user = db.users.find((u) => u.email.toLowerCase() === key);
      if (!user || db.credentials[user.email] !== password) {
        return { ok: false, error: "E-mail ou senha inválidos." };
      }
      setCurrentUserId(user.id);
      return { ok: true };
    },
    [db.users, db.credentials],
  );

  const register = useCallback(
    (input: RegisterInput): Result => {
      const email = input.email.trim().toLowerCase();
      if (db.users.some((u) => u.email.toLowerCase() === email)) {
        return { ok: false, error: "Já existe uma conta com este e-mail." };
      }
      const companyId = db.companies[0]?.id ?? crypto.randomUUID();
      const userId = crypto.randomUUID();
      const newUser: User = {
        id: userId,
        name: input.name.trim(),
        email: input.email.trim(),
        role: "Psychologist",
        companyId,
        createdAt: new Date().toISOString(),
      };
      const newPsy: Psychologist = {
        id: crypto.randomUUID(),
        userId,
        name: input.name.trim(),
        email: input.email.trim(),
        licenseNumber: input.licenseNumber.trim(),
        approach: input.approach,
        companyId,
        workingHours: [1, 2, 3, 4, 5].map((d) => ({ dayOfWeek: d, start: "08:00", end: "18:00" })),
      };
      setDb((prev) => ({
        ...prev,
        users: [...prev.users, newUser],
        psychologists: [...prev.psychologists, newPsy],
        credentials: { ...prev.credentials, [newUser.email]: input.password },
      }));
      setCurrentUserId(userId);
      return { ok: true };
    },
    [db.users, db.companies],
  );

  const logout = useCallback(() => setCurrentUserId(null), []);

  // ---- Patients ------------------------------------------------------
  const addPatient = useCallback(
    (input: PatientInput): Patient => {
      const patient: Patient = {
        id: crypto.randomUUID(),
        companyId: currentUser?.companyId ?? db.companies[0]?.id ?? "",
        createdAt: new Date().toISOString(),
        name: input.name.trim(),
        email: input.email.trim(),
        cpf: input.cpf?.trim() || undefined,
        phone: input.phone?.trim() || undefined,
        birthDate: input.birthDate || undefined,
        notes: input.notes?.trim() || undefined,
      };
      setDb((prev) => ({ ...prev, patients: [...prev.patients, patient] }));
      return patient;
    },
    [currentUser, db.companies],
  );

  const updatePatient = useCallback((id: string, input: PatientInput) => {
    setDb((prev) => ({
      ...prev,
      patients: prev.patients.map((p) =>
        p.id === id
          ? {
              ...p,
              name: input.name.trim(),
              email: input.email.trim(),
              cpf: input.cpf?.trim() || undefined,
              phone: input.phone?.trim() || undefined,
              birthDate: input.birthDate || undefined,
              notes: input.notes?.trim() || undefined,
            }
          : p,
      ),
    }));
  }, []);

  const deletePatient = useCallback((id: string) => {
    setDb((prev) => ({ ...prev, patients: prev.patients.filter((p) => p.id !== id) }));
  }, []);

  // ---- Schedules -----------------------------------------------------
  const createSchedule = useCallback(
    (input: ScheduleInput): Result & { schedule?: Schedule } => {
      const error = validateSchedule(db, input);
      if (error) return { ok: false, error };

      const scheduleId = crypto.randomUUID();
      const companyId = currentUser?.companyId ?? db.companies[0]?.id ?? "";
      let sessionId: string | undefined;
      const newSessions: Session[] = [];

      if (input.type === ScheduleType.SESSION && input.patientId) {
        sessionId = crypto.randomUUID();
        newSessions.push({
          id: sessionId,
          scheduleId,
          psychologistId: input.psychologistId,
          patientId: input.patientId,
          companyId,
          description: "",
          feedback: "",
          status: SessionStatus.Scheduled,
          createdAt: new Date().toISOString(),
        });
      }

      const schedule: Schedule = {
        id: scheduleId,
        date: input.date,
        start: input.start,
        end: input.end,
        psychologistId: input.psychologistId,
        type: input.type,
        status: input.status ?? ScheduleStatus.Pending,
        companyId,
        sessionId: sessionId ?? null,
        title: input.title,
      };

      setDb((prev) => ({
        ...prev,
        schedules: [...prev.schedules, schedule],
        sessions: [...prev.sessions, ...newSessions],
      }));
      return { ok: true, schedule };
    },
    [db, currentUser, db.companies],
  );

  const setScheduleStatus = useCallback((id: string, status: ScheduleStatus) => {
    setDb((prev) => ({
      ...prev,
      schedules: prev.schedules.map((s) => (s.id === id ? { ...s, status } : s)),
    }));
  }, []);

  const moveSchedule = useCallback(
    (id: string, date: string, start: string, end: string): Result => {
      const existing = db.schedules.find((s) => s.id === id);
      if (!existing) return { ok: false, error: "Agendamento não encontrado." };
      const session = db.sessions.find((ss) => ss.id === existing.sessionId);
      const error = validateSchedule(
        db,
        {
          date,
          start,
          end,
          psychologistId: existing.psychologistId,
          type: existing.type,
          patientId: session?.patientId ?? null,
        },
        { ignoreScheduleId: id, allowPast: true },
      );
      if (error) return { ok: false, error };
      setDb((prev) => ({
        ...prev,
        schedules: prev.schedules.map((s) => (s.id === id ? { ...s, date, start, end } : s)),
      }));
      return { ok: true };
    },
    [db],
  );

  const deleteSchedule = useCallback((id: string) => {
    setDb((prev) => {
      const target = prev.schedules.find((s) => s.id === id);
      return {
        ...prev,
        schedules: prev.schedules.filter((s) => s.id !== id),
        sessions: prev.sessions.filter((ss) => ss.id !== target?.sessionId),
      };
    });
  }, []);

  // ---- Sessions ------------------------------------------------------
  const completeSession = useCallback((sessionId: string, feedback: string, description: string) => {
    setDb((prev) => ({
      ...prev,
      sessions: prev.sessions.map((s) =>
        s.id === sessionId ? { ...s, feedback, description, status: SessionStatus.Completed } : s,
      ),
    }));
  }, []);

  const setSessionStatus = useCallback((sessionId: string, status: SessionStatus) => {
    setDb((prev) => ({
      ...prev,
      sessions: prev.sessions.map((s) => (s.id === sessionId ? { ...s, status } : s)),
    }));
  }, []);

  // ---- Psychologist --------------------------------------------------
  const setWorkingHours = useCallback(
    (psychologistId: string, hours: Psychologist["workingHours"]) => {
      setDb((prev) => ({
        ...prev,
        psychologists: prev.psychologists.map((p) =>
          p.id === psychologistId ? { ...p, workingHours: hours } : p,
        ),
      }));
    },
    [],
  );

  const updateProfile = useCallback(
    (input: ProfileInput) => {
      const name = input.name.trim();
      setDb((prev) => ({
        ...prev,
        users: prev.users.map((u) => (u.id === currentUserId ? { ...u, name } : u)),
        psychologists: prev.psychologists.map((p) =>
          p.userId === currentUserId
            ? {
                ...p,
                name,
                phone: input.phone?.trim() || undefined,
                approach: input.approach,
                licenseNumber: input.licenseNumber.trim(),
              }
            : p,
        ),
      }));
    },
    [currentUserId],
  );

  const toggleTheme = useCallback(() => setTheme((t) => (t === "light" ? "dark" : "light")), []);

  const resetData = useCallback(() => {
    const seed = buildSeed();
    setDb(seed);
    setCurrentUserId(null);
  }, []);

  const value: AppStoreValue = {
    db,
    currentUser,
    activePsychologist,
    theme,
    toasts,
    login,
    register,
    logout,
    addPatient,
    updatePatient,
    deletePatient,
    createSchedule,
    setScheduleStatus,
    moveSchedule,
    deleteSchedule,
    completeSession,
    setSessionStatus,
    setWorkingHours,
    updateProfile,
    notify,
    dismissToast,
    toggleTheme,
    resetData,
  };

  return <AppStoreContext.Provider value={value}>{children}</AppStoreContext.Provider>;
}

export function useStore(): AppStoreValue {
  const ctx = useContext(AppStoreContext);
  if (!ctx) throw new Error("useStore must be used within AppStoreProvider");
  return ctx;
}
