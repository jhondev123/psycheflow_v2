import { NavLink } from "react-router-dom";
import {
  CalendarDays,
  ClipboardList,
  Clock,
  FileText,
  LayoutDashboard,
  LogOut,
  Moon,
  RotateCcw,
  Settings,
  Sun,
  Users,
} from "lucide-react";
import { useStore } from "@/store/AppStore";
import { Avatar } from "@/components/ui/Avatar";
import { roleMeta } from "@/lib/domain";
import { format } from "date-fns";
const navClass = ({ isActive }: { isActive: boolean }) => `nav-link ${isActive ? "active" : ""}`;

export function Sidebar({ open, onNavigate }: { open: boolean; onNavigate: () => void }) {
  const { currentUser, activePsychologist, db, theme, toggleTheme, logout, resetData, notify } =
    useStore();

  const today = format(new Date(), "yyyy-MM-dd");
  const todayCount = activePsychologist
    ? db.schedules.filter(
        (s) => s.psychologistId === activePsychologist.id && s.date === today && s.status !== 2,
      ).length
    : 0;

  return (
    <aside className={`sidebar ${open ? "open" : ""}`}>
      <div className="brand">
        <img src="/favicon.svg" className="brand-logo" alt="" />
        <div>
          <div className="brand-name">Psycheflow</div>
          <div className="brand-sub">Gestão clínica</div>
        </div>
      </div>

      <nav className="side-nav" onClick={onNavigate}>
        <NavLink to="/" end className={navClass}>
          <LayoutDashboard />
          <span>Painel</span>
        </NavLink>
        <NavLink to="/agenda" className={navClass}>
          <CalendarDays />
          <span>Agenda</span>
          {todayCount > 0 && <span className="nav-badge">{todayCount}</span>}
        </NavLink>
        <NavLink to="/pacientes" className={navClass}>
          <Users />
          <span>Pacientes</span>
        </NavLink>
        <NavLink to="/sessoes" className={navClass}>
          <ClipboardList />
          <span>Sessões</span>
        </NavLink>
        <NavLink to="/documentos" className={navClass}>
          <FileText />
          <span>Documentos</span>
        </NavLink>

        <div className="nav-section">Configurações</div>
        <NavLink to="/configuracoes/horarios" className={navClass}>
          <Clock />
          <span>Horários de atendimento</span>
        </NavLink>
        <NavLink to="/configuracoes/perfil" className={navClass}>
          <Settings />
          <span>Perfil</span>
        </NavLink>
      </nav>

      <div className="side-user">
        <Avatar name={currentUser?.name ?? "?"} size="sm" colorful={false} />
        <div className="grow" style={{ minWidth: 0 }}>
          <div className="name truncate">{currentUser?.name}</div>
          <div className="role">{currentUser ? roleMeta[currentUser.role] : ""}</div>
        </div>
        <button
          className="icon-btn-light"
          onClick={toggleTheme}
          title="Alternar tema"
          aria-label="Alternar tema"
        >
          {theme === "light" ? <Moon /> : <Sun />}
        </button>
        <button
          className="icon-btn-light"
          onClick={() => {
            if (confirm("Restaurar todos os dados de demonstração? Isso apagará suas alterações.")) {
              resetData();
              notify("info", "Dados de demonstração restaurados.");
            }
          }}
          title="Restaurar dados de demonstração"
          aria-label="Restaurar dados"
        >
          <RotateCcw />
        </button>
        <button className="icon-btn-light" onClick={logout} title="Sair" aria-label="Sair">
          <LogOut />
        </button>
      </div>
    </aside>
  );
}
