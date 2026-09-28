import { useState } from "react";
import { Modal } from "@/components/ui/Modal";
import { api, ApiError, errorMessage } from "@/lib/api";
import { paymentMethodLabel } from "@/lib/domain";
import { fmtMoney, isoDate, parseMoney } from "@/lib/format";
import { useStore } from "@/store/AppStore";
import type { PaymentMethod } from "@/types";

interface PayDialogProps {
  paymentId: string;
  amount: number;
  patientName?: string;
  onClose: () => void;
  onPaid: () => void;
}

/** RF011: lança o pagamento de uma sessão concluída (método, data e valor). */
export function PayDialog({ paymentId, amount, patientName, onClose, onPaid }: PayDialogProps) {
  const { notify } = useStore();
  const [method, setMethod] = useState<PaymentMethod>("Pix");
  const [paidAt, setPaidAt] = useState(isoDate(new Date()));
  const [value, setValue] = useState(amount.toFixed(2).replace(".", ","));
  const [notes, setNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const parsed = parseMoney(value);

  async function submit() {
    if (parsed === null || parsed < 0) return setError("Informe um valor válido.");
    setBusy(true);
    try {
      await api.post(`/payments/${paymentId}/pay`, { method, paidAt, amount: parsed, notes: notes.trim() || null });
      notify("success", `Pagamento de ${fmtMoney(parsed)} registrado.`);
      onPaid();
      onClose();
    } catch (e) {
      setError(e instanceof ApiError ? e.message : errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title={patientName ? `Receber pagamento — ${patientName}` : "Receber pagamento"}
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" disabled={busy} onClick={submit}>
            {busy ? "Salvando…" : "Confirmar recebimento"}
          </button>
        </>
      }
    >
      <div className="grid-form">
        {error && <div className="auth-error span-2">{error}</div>}
        <div className="field">
          <label className="label" htmlFor="method">
            Forma de pagamento
          </label>
          <select id="method" className="select" value={method} onChange={(e) => setMethod(e.target.value as PaymentMethod)}>
            {(Object.keys(paymentMethodLabel) as PaymentMethod[]).map((m) => (
              <option key={m} value={m}>
                {paymentMethodLabel[m]}
              </option>
            ))}
          </select>
        </div>
        <div className="field">
          <label className="label" htmlFor="paidAt">
            Data
          </label>
          <input id="paidAt" className="input" type="date" value={paidAt} min={isoDate(new Date())} onChange={(e) => setPaidAt(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="amount">
            Valor (R$)
          </label>
          <input id="amount" className="input" inputMode="decimal" value={value} onChange={(e) => setValue(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="payNotes">
            Observação
          </label>
          <input id="payNotes" className="input" value={notes} maxLength={500} onChange={(e) => setNotes(e.target.value)} />
        </div>
      </div>
    </Modal>
  );
}
