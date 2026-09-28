import { AlertCircle, RotateCw } from "lucide-react";

export function Loading({ label = "Carregando…" }: { label?: string }) {
  return (
    <div className="loading-block" role="status">
      <span className="spinner" /> {label}
    </div>
  );
}

export function ErrorState({ message, onRetry }: { message: string; onRetry?: () => void }) {
  return (
    <div className="error-block" role="alert">
      <AlertCircle />
      <span>{message}</span>
      {onRetry && (
        <button className="btn btn-subtle btn-sm" onClick={onRetry}>
          <RotateCw size={15} /> Tentar de novo
        </button>
      )}
    </div>
  );
}

interface PagerProps {
  page: number;
  totalPages: number;
  totalCount: number;
  onPage: (page: number) => void;
}

export function Pager({ page, totalPages, totalCount, onPage }: PagerProps) {
  if (totalPages <= 1) return null;
  return (
    <div className="pager">
      <span>
        {totalCount} registros · página {page} de {totalPages}
      </span>
      <button className="btn btn-ghost btn-sm" disabled={page <= 1} onClick={() => onPage(page - 1)}>
        Anterior
      </button>
      <button className="btn btn-ghost btn-sm" disabled={page >= totalPages} onClick={() => onPage(page + 1)}>
        Próxima
      </button>
    </div>
  );
}
