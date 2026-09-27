import { AlertCircle, CheckCircle2, Info, X } from "lucide-react";
import { useStore } from "@/store/AppStore";

const icons = { success: CheckCircle2, error: AlertCircle, info: Info } as const;

export function Toasts() {
  const { toasts, dismissToast } = useStore();
  if (toasts.length === 0) return null;
  return (
    <div className="toasts">
      {toasts.map((t) => {
        const Icon = icons[t.type];
        return (
          <div key={t.id} className={`toast toast-${t.type}`}>
            <Icon />
            <span className="grow small strong">{t.message}</span>
            <button
              className="btn btn-ghost btn-icon btn-sm"
              onClick={() => dismissToast(t.id)}
              aria-label="Fechar"
            >
              <X />
            </button>
          </div>
        );
      })}
    </div>
  );
}
