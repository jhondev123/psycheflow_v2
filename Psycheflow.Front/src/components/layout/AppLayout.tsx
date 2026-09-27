import { useState } from "react";
import { Outlet } from "react-router-dom";
import { Sidebar } from "@/components/layout/Sidebar";
import { Topbar } from "@/components/layout/Topbar";
import { Toasts } from "@/components/ui/Toasts";
import "@/styles/layout.css";

export function AppLayout() {
  const [open, setOpen] = useState(false);
  return (
    <div className="app-shell">
      {open && <div className="scrim" onClick={() => setOpen(false)} />}
      <Sidebar open={open} onNavigate={() => setOpen(false)} />
      <div className="app-main">
        <Topbar onMenu={() => setOpen(true)} />
        <main className="app-content">
          <div className="content-wrap page-enter">
            <Outlet />
          </div>
        </main>
      </div>
      <Toasts />
    </div>
  );
}
