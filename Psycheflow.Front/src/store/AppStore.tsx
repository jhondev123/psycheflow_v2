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
import { api, getToken, onUnauthorized, setToken } from "@/lib/api";
import type { AuthResponse, Me, Psychologist, RegisterInput, Settings } from "@/types";

const THEME_KEY = "psycheflow.theme.v1";

type Theme = "light" | "dark";
type ToastType = "success" | "error" | "info";
interface Toast {
  id: string;
  type: ToastType;
  message: string;
}

interface AppStoreValue {
  /** Usuário logado (GET /auth/me). */
  me: Me | null;
  /** Carregando a sessão salva ao abrir o app. */
  booting: boolean;
  /** Perfil de psicólogo do usuário logado, quando existir. */
  psychologist: Psychologist | null;
  settings: Settings | null;
  isManagement: boolean;
  isPsychologist: boolean;
  theme: Theme;
  toasts: Toast[];

  login: (email: string, password: string) => Promise<Me>;
  register: (input: RegisterInput) => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  logout: () => void;
  refreshProfile: () => Promise<void>;
  refreshSettings: () => Promise<void>;

  notify: (type: ToastType, message: string) => void;
  dismissToast: (id: string) => void;
  toggleTheme: () => void;
}

const AppStoreContext = createContext<AppStoreValue | null>(null);

function loadTheme(): Theme {
  return localStorage.getItem(THEME_KEY) === "dark" ? "dark" : "light";
}

export function AppStoreProvider({ children }: { children: ReactNode }) {
  const [me, setMe] = useState<Me | null>(null);
  const [booting, setBooting] = useState(() => getToken() !== null);
  const [psychologist, setPsychologist] = useState<Psychologist | null>(null);
  const [settings, setSettings] = useState<Settings | null>(null);
  const [theme, setTheme] = useState<Theme>(loadTheme);
  const [toasts, setToasts] = useState<Toast[]>([]);
  const timers = useRef<Record<string, number>>({});

  useEffect(() => {
    localStorage.setItem(THEME_KEY, theme);
    document.documentElement.setAttribute("data-theme", theme);
  }, [theme]);

  const dismissToast = useCallback((id: string) => {
    setToasts((prev) => prev.filter((t) => t.id !== id));
  }, []);

  const notify = useCallback(
    (type: ToastType, message: string) => {
      const id = crypto.randomUUID();
      setToasts((prev) => [...prev, { id, type, message }]);
      timers.current[id] = window.setTimeout(() => dismissToast(id), type === "error" ? 6000 : 3600);
    },
    [dismissToast],
  );

  useEffect(() => {
    const t = timers.current;
    return () => Object.values(t).forEach((handle) => clearTimeout(handle));
  }, []);

  const logout = useCallback(() => {
    setToken(null);
    setMe(null);
    setPsychologist(null);
    setSettings(null);
  }, []);

  /** Carrega usuário, perfil de psicólogo e configurações da clínica. */
  const loadSession = useCallback(async (): Promise<Me> => {
    const current = await api.get<Me>("/auth/me");
    setMe(current);
    if (!current.mustChangePassword) {
      const [profile, clinic] = await Promise.all([
        current.psychologistId ? api.get<Psychologist>("/psychologists/me") : Promise.resolve(null),
        api.get<Settings>("/settings"),
      ]);
      setPsychologist(profile);
      setSettings(clinic);
    }
    return current;
  }, []);

  // Sessão salva: valida o token ao abrir o app.
  useEffect(() => {
    onUnauthorized(() => {
      logout();
      notify("info", "Sua sessão expirou. Entre novamente.");
    });
    if (getToken()) {
      loadSession()
        .catch(() => logout())
        .finally(() => setBooting(false));
    }
    return () => onUnauthorized(null);
  }, [loadSession, logout, notify]);

  const startSession = useCallback(
    async (auth: AuthResponse): Promise<Me> => {
      setToken(auth.accessToken);
      return loadSession();
    },
    [loadSession],
  );

  const login = useCallback(
    async (email: string, password: string) =>
      startSession(await api.post<AuthResponse>("/auth/login", { email: email.trim(), password })),
    [startSession],
  );

  const register = useCallback(
    async (input: RegisterInput) => {
      await startSession(await api.post<AuthResponse>("/auth/register", input));
    },
    [startSession],
  );

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      await startSession(await api.post<AuthResponse>("/auth/change-password", { currentPassword, newPassword }));
    },
    [startSession],
  );

  const refreshProfile = useCallback(async () => {
    if (me?.psychologistId) setPsychologist(await api.get<Psychologist>("/psychologists/me"));
    setMe(await api.get<Me>("/auth/me"));
  }, [me?.psychologistId]);

  const refreshSettings = useCallback(async () => {
    setSettings(await api.get<Settings>("/settings"));
  }, []);

  const toggleTheme = useCallback(() => setTheme((t) => (t === "light" ? "dark" : "light")), []);

  const value = useMemo<AppStoreValue>(
    () => ({
      me,
      booting,
      psychologist,
      settings,
      isManagement: !!me && (me.roles.includes("Admin") || me.roles.includes("Manager")),
      isPsychologist: !!me?.psychologistId,
      theme,
      toasts,
      login,
      register,
      changePassword,
      logout,
      refreshProfile,
      refreshSettings,
      notify,
      dismissToast,
      toggleTheme,
    }),
    [me, booting, psychologist, settings, theme, toasts, login, register, changePassword, logout, refreshProfile, refreshSettings, notify, dismissToast, toggleTheme],
  );

  return <AppStoreContext.Provider value={value}>{children}</AppStoreContext.Provider>;
}

export function useStore(): AppStoreValue {
  const ctx = useContext(AppStoreContext);
  if (!ctx) throw new Error("useStore must be used within AppStoreProvider");
  return ctx;
}
