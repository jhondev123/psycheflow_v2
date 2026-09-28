import { Navigate, Route, Routes } from "react-router-dom";
import { useStore } from "@/store/AppStore";
import { CHANGE_PASSWORD_PATH, RequireAuth, RequireRole } from "@/components/RequireAuth";
import { AppLayout } from "@/components/layout/AppLayout";
import { Login } from "@/pages/Login";
import { Register } from "@/pages/Register";
import { ChangePassword } from "@/pages/ChangePassword";
import { Dashboard } from "@/pages/Dashboard";
import { Agenda } from "@/pages/Agenda";
import { Patients } from "@/pages/Patients";
import { PatientDetail } from "@/pages/PatientDetail";
import { Sessions } from "@/pages/Sessions";
import { Payments } from "@/pages/Payments";
import { MedicalRecords } from "@/pages/MedicalRecords";
import { Documents } from "@/pages/Documents";
import { WorkingHours } from "@/pages/WorkingHours";
import { Profile } from "@/pages/Profile";
import { ClinicSettings } from "@/pages/ClinicSettings";
import { Users } from "@/pages/Users";
import { AiSettings } from "@/pages/AiSettings";

/** Quem já está logado não vê as telas de login/cadastro. */
function PublicOnly({ children }: { children: React.ReactNode }) {
  const { me, booting } = useStore();
  if (!booting && me) return <Navigate to="/" replace />;
  return <>{children}</>;
}

export function App() {
  return (
    <Routes>
      <Route path="/login" element={<PublicOnly><Login /></PublicOnly>} />
      <Route path="/register" element={<PublicOnly><Register /></PublicOnly>} />
      <Route path={CHANGE_PASSWORD_PATH} element={<RequireAuth><ChangePassword /></RequireAuth>} />

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
        <Route path="/pacientes/:id" element={<PatientDetail />} />
        <Route path="/sessoes" element={<Sessions />} />
        <Route path="/financeiro" element={<Payments />} />
        <Route path="/prontuarios" element={<RequireRole psychologist><MedicalRecords /></RequireRole>} />
        <Route path="/documentos" element={<Documents />} />
        <Route path="/configuracoes/perfil" element={<RequireRole psychologist><Profile /></RequireRole>} />
        <Route path="/configuracoes/horarios" element={<WorkingHours />} />
        <Route path="/configuracoes/clinica" element={<ClinicSettings />} />
        <Route path="/configuracoes/usuarios" element={<RequireRole management><Users /></RequireRole>} />
        <Route path="/configuracoes/ia" element={<AiSettings />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
