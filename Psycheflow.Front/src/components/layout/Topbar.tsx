import { Menu } from "lucide-react";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { useStore } from "@/store/AppStore";
import { capitalize } from "@/lib/format";
function greeting(): string {
  const h = new Date().getHours();
  if (h < 12) return "Bom dia";
  if (h < 18) return "Boa tarde";
  return "Boa noite";
}

export function Topbar({ onMenu }: { onMenu: () => void }) {
  const { currentUser } = useStore();
  const firstName = currentUser?.name.split(" ")[0] ?? "";
  return (
    <header className="topbar">
      <button className="icon-btn-light menu-toggle" onClick={onMenu} aria-label="Abrir menu" style={{ background: "var(--surface-2)", color: "var(--text-muted)" }}>
        <Menu />
      </button>
      <div className="grow">
        <div className="greeting">
          {greeting()}, {firstName} 👋
        </div>
        <div className="date">{capitalize(format(new Date(), "EEEE, d 'de' MMMM 'de' yyyy", { locale: ptBR }))}</div>
      </div>
    </header>
  );
}
