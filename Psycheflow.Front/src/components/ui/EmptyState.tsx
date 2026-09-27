import type { ReactNode } from "react";

interface EmptyStateProps {
  icon: ReactNode;
  title: string;
  description?: string;
  action?: ReactNode;
}

export function EmptyState({ icon, title, description, action }: EmptyStateProps) {
  return (
    <div className="empty">
      <div className="empty-icon">{icon}</div>
      <div className="strong" style={{ fontSize: 15, color: "var(--text)" }}>
        {title}
      </div>
      {description && <div className="small" style={{ maxWidth: 360 }}>{description}</div>}
      {action && <div style={{ marginTop: 6 }}>{action}</div>}
    </div>
  );
}
