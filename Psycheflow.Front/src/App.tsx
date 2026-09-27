import { Navigate, Route, Routes } from "react-router-dom";
import { useStore } from "@/store/AppStore";
import { RequireAuth } from "@/components/RequireAuth";
import { AppLayout } from "@/components/layout/AppLayout";
import { Login } from "@/pages/Login";
import { Register } from "@/pages/Register";
import { Dashboard } from "@/pages/Dashboard";
import { Agenda } from "@/pages/Agenda";
import { Patients } from "@/pages/Patients";
import { Sessions } from "@/pages/Sessions";
import { Documents } from "@/pages/Documents";
import { WorkingHours } from "@/pages/WorkingHours";
import { Profile } from "@/pages/Profile";

/** Redirects authenticated users away from the public auth pages. */
function PublicOnly({ children }: { children: React.ReactNode }) {
  const { currentUser } = useStore();
  if (currentUser) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export function App() {
  return (
    <Routes>
      <Route
        path="/login"
        element={
          <PublicOnly>
            <Login />
          </PublicOnly>
        }
      />
      <Route
        path="/register"
        element={
          <PublicOnly>
            <Register />
          </PublicOnly>
        }
      />

      <Route
        element={
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        }
      >
        <Route path="/" element={<Dashboard />} />
        <Route path="/agenda" element={<Agenda />} />
        <Route path="/pacientes" element={<Patients />} />
        <Route path="/sessoes" element={<Sessions />} />
        <Route path="/documentos" element={<Documents />} />
        <Route path="/configuracoes/horarios" element={<WorkingHours />} />
        <Route path="/configuracoes/perfil" element={<Profile />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
