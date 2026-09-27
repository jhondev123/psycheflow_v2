import { Navigate, useLocation } from "react-router-dom";
import type { ReactNode } from "react";
import { useStore } from "@/store/AppStore";

export function RequireAuth({ children }: { children: ReactNode }) {
  const { currentUser } = useStore();
  const location = useLocation();
  if (!currentUser) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  return <>{children}</>;
}
