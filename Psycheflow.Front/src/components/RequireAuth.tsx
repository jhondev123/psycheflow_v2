import { Navigate, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useStore } from "@/store/AppStore";
import { Loading } from "@/components/ui/Feedback";

export const CHANGE_PASSWORD_PATH = "/trocar-senha";

/** Exige login; com senha temporária, só a tela de troca de senha fica acessível (como na API). */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { me, booting } = useStore();
  const location = useLocation();

  if (booting) return <Loading label="Abrindo sua sessão…" />;
  if (!me) return <Navigate to="/login" state={{ from: location }} replace />;
  if (me.mustChangePassword && location.pathname !== CHANGE_PASSWORD_PATH) {
    return <Navigate to={CHANGE_PASSWORD_PATH} replace />;
  }
  return <>{children}</>;
}

/** Restringe uma rota a Admin/Manager ou a psicólogos. */
export function RequireRole({ management, psychologist, children }: { management?: boolean; psychologist?: boolean; children: ReactNode }) {
  const { isManagement, isPsychologist } = useStore();
  const allowed = (management && isManagement) || (psychologist && isPsychologist);
  return allowed ? <>{children}</> : <Navigate to="/" replace />;
}
