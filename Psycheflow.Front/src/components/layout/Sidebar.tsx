import { NavLink } from "react-router-dom";
import {
  Building2,
  CalendarDays,
  ClipboardList,
  Clock,
  FileText,
  FolderLock,
  LayoutDashboard,
  LogOut,
  Moon,
  Settings,
  Sparkles,
  Sun,
  UserCog,
  Users,
  Wallet,
} from "lucide-react";
import { useStore } from "@/store/AppStore";
import { Avatar } from "@/components/ui/Avatar";
import { mainRole } from "@/lib/domain";

const navClass = ({ isActive }: { isActive: boolean }) => `nav-link ${isActive ? "active" : ""}`;

export function Sidebar({ open, onNavigate }: { open: boolean; onNavigate: () => void }) {
  const { me, isManagement, isPsychologist, theme, toggleTheme, logout } = useStore();

  return (
    <aside className={`sidebar ${open ? "open" : ""}`}>
      <div className="brand">
        <img src="/favicon.svg" className="brand-logo" alt="" />
        <div>
          <div className="brand-name">Psycheflow</div>
          <div className="brand-sub truncate">{me?.companyName ?? "Gestão clínica"}</div>
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
        </NavLink>
        <NavLink to="/pacientes" className={navClass}>
          <Users />
          <span>Pacientes</span>
        </NavLink>
        <NavLink to="/sessoes" className={navClass}>
          <ClipboardList />
          <span>Sessões</span>
        </NavLink>
        <NavLink to="/financeiro" className={navClass}>
          <Wallet />
          <span>Financeiro</span>
        </NavLink>
        {isPsychologist && (
          <NavLink to="/prontuarios" className={navClass}>
            <FolderLock />
            <span>Prontuários</span>
          </NavLink>
        )}
        <NavLink to="/documentos" className={navClass}>
          <FileText />
          <span>Documentos</span>
        </NavLink>

        <div className="nav-section">Configurações</div>
        {isPsychologist && (
          <NavLink to="/configuracoes/perfil" className={navClass}>
            <Settings />
            <span>Meu perfil</span>
          </NavLink>
        )}
        <NavLink to="/configuracoes/horarios" className={navClass}>
          <Clock />
          <span>Horários de atendimento</span>
        </NavLink>
        <NavLink to="/configuracoes/clinica" className={navClass}>
          <Building2 />
          <span>Clínica</span>
        </NavLink>
        {isManagement && (
          <NavLink to="/configuracoes/usuarios" className={navClass}>
            <UserCog />
            <span>Usuários</span>
          </NavLink>
        )}
        <NavLink to="/configuracoes/ia" className={navClass}>
          <Sparkles />
          <span>Assistente de IA</span>
        </NavLink>
      </nav>

      <div className="side-user">
        <Avatar name={me?.fullName ?? "?"} size="sm" colorful={false} />
        <div className="grow" style={{ minWidth: 0 }}>
          <div className="name truncate">{me?.fullName}</div>
          <div className="role">{me ? mainRole(me.roles) : ""}</div>
        </div>
        <button className="icon-btn-light" onClick={toggleTheme} title="Alternar tema" aria-label="Alternar tema">
          {theme === "light" ? <Moon /> : <Sun />}
        </button>
        <button className="icon-btn-light" onClick={logout} title="Sair" aria-label="Sair">
          <LogOut />
        </button>
      </div>
    </aside>
  );
}
