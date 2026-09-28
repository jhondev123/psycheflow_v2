import { useState } from "react";
import { Modal } from "@/components/ui/Modal";

interface ReasonDialogProps {
  title: string;
  message?: string;
  label?: string;
  confirmLabel: string;
  danger?: boolean;
  required?: boolean;
  onConfirm: (reason: string) => Promise<void>;
  onClose: () => void;
}

/** Pede um motivo (cancelamento, estorno, encerramento…) antes de confirmar a ação. */
export function ReasonDialog({
  title,
  message,
  label = "Motivo",
  confirmLabel,
  danger,
  required = true,
  onConfirm,
  onClose,
}: ReasonDialogProps) {
  const [reason, setReason] = useState("");
  const [busy, setBusy] = useState(false);
  const invalid = required && reason.trim().length === 0;

  async function confirm() {
    setBusy(true);
    try {
      await onConfirm(reason.trim());
      onClose();
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title={title}
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Voltar
          </button>
          <button className={`btn ${danger ? "btn-danger" : "btn-primary"}`} disabled={invalid || busy} onClick={confirm}>
            {busy ? "Salvando…" : confirmLabel}
          </button>
        </>
      }
    >
      <div className="col gap-3">
        {message && <div className="muted">{message}</div>}
        <div className="field">
          <label className="label" htmlFor="reason">
            {label} {required && <span className="req">*</span>}
          </label>
          <textarea id="reason" className="textarea" value={reason} maxLength={500} onChange={(e) => setReason(e.target.value)} autoFocus />
        </div>
      </div>
    </Modal>
  );
}
