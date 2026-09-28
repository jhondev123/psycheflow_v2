import { useState } from "react";
import { endOfMonth, startOfMonth } from "date-fns";
import { FileDown, Pencil, Undo2, Wallet } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { PagedResponse, Payment, PaymentStatus, Psychologist } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading, Pager } from "@/components/ui/Feedback";
import { Modal } from "@/components/ui/Modal";
import { ReasonDialog } from "@/components/ui/ReasonDialog";
import { PatientSelect } from "@/components/PatientSelect";
import { PayDialog } from "@/components/PayDialog";
import { SessionModal } from "@/components/SessionModal";
import { paymentMethodLabel, paymentStatusMeta, sessionStatusMeta } from "@/lib/domain";
import { fmtDateShort, fmtMoney, hm, isoDate, parseMoney } from "@/lib/format";
import "@/styles/pages.css";

/** RF011–RF013: pagamentos por período (data da sessão), situação, paciente e psicólogo. */
export function Payments() {
  const { isManagement, notify } = useStore();
  const [from, setFrom] = useState(isoDate(startOfMonth(new Date())));
  const [to, setTo] = useState(isoDate(endOfMonth(new Date())));
  const [status, setStatus] = useState<"" | PaymentStatus>("");
  const [patientId, setPatientId] = useState("");
  const [psychologistId, setPsychologistId] = useState("");
  const [page, setPage] = useState(1);

  const [paying, setPaying] = useState<Payment | null>(null);
  const [editing, setEditing] = useState<Payment | null>(null);
  const [cancelling, setCancelling] = useState<Payment | null>(null);
  const [openSession, setOpenSession] = useState<string | null>(null);

  const { data, loading, error, reload } = useAsync(
    () => api.get<PagedResponse<Payment>>("/payments", { from, to, status, patientId, psychologistId, page, pageSize: 20 }),
    [from, to, status, patientId, psychologistId, page],
  );
  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), [], isManagement);

  const totals = (data?.items ?? []).reduce(
    (acc, p) => {
      if (p.status === "Paid") acc.paid += p.amount;
      if (p.status === "Pending") acc.pending += p.amount;
      return acc;
    },
    { paid: 0, pending: 0 },
  );

  async function download(path: string, query?: Record<string, string>) {
    try {
      await api.download(path, query);
    } catch (e) {
      notify("error", errorMessage(e));
    }
  }

  function filter<T>(setter: (v: T) => void) {
    return (v: T) => {
      setter(v);
      setPage(1);
    };
  }

  return (
    <>
      <PageHeader
        title="Financeiro"
        subtitle="Pagamentos das sessões: lançar, editar, estornar e emitir recibos."
        actions={
          <button
            className="btn btn-subtle"
            onClick={() => download("/documents/sessions-report", { from, to, paymentStatus: status, psychologistId })}
          >
            <FileDown /> Relatório do período (PDF)
          </button>
        }
      />

      <div className="filters">
        <div className="field">
          <label className="label" htmlFor="pFrom">
            Sessões de
          </label>
          <input id="pFrom" className="input" type="date" value={from} onChange={(e) => filter(setFrom)(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="pTo">
            Até
          </label>
          <input id="pTo" className="input" type="date" value={to} min={from} onChange={(e) => filter(setTo)(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="pStatus">
            Situação
          </label>
          <select id="pStatus" className="select" value={status} onChange={(e) => filter(setStatus)(e.target.value as "" | PaymentStatus)}>
            <option value="">Todas</option>
            <option value="Pending">Pendentes</option>
            <option value="Paid">Pagos</option>
            <option value="Cancelled">Cancelados</option>
          </select>
        </div>
        <div className="field" style={{ minWidth: 220 }}>
          <label className="label" htmlFor="pPatient">
            Paciente
          </label>
          <PatientSelect id="pPatient" allowAll value={patientId} onChange={filter(setPatientId)} />
        </div>
        {isManagement && (
          <div className="field">
            <label className="label" htmlFor="pPsy">
              Psicólogo
            </label>
            <select id="pPsy" className="select" value={psychologistId} onChange={(e) => filter(setPsychologistId)(e.target.value)}>
              <option value="">Todos</option>
              {(psychologists.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      {data && data.items.length > 0 && (
        <div className="stat-grid" style={{ marginBottom: 16 }}>
          <div className="card stat">
            <div className="stat-ic" style={{ background: "var(--success-bg)", color: "var(--success)" }}>
              <Wallet />
            </div>
            <div>
              <div className="value">{fmtMoney(totals.paid)}</div>
              <div className="label">Recebido (nesta página)</div>
            </div>
          </div>
          <div className="card stat">
            <div className="stat-ic" style={{ background: "var(--warning-bg)", color: "var(--warning)" }}>
              <Wallet />
            </div>
            <div>
              <div className="value">{fmtMoney(totals.pending)}</div>
              <div className="label">Pendente (nesta página)</div>
            </div>
          </div>
        </div>
      )}

      <div className="card">
        {loading && !data ? (
          <Loading />
        ) : error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState icon={<Wallet />} title="Nenhum pagamento no período" description="Os pagamentos nascem pendentes quando a sessão é agendada." />
        ) : (
          <>
            <div className="table-scroll">
              <table className="table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Sessão</th>
                    <th>Valor</th>
                    <th>Situação</th>
                    <th>Recebimento</th>
                    <th style={{ width: 150 }}></th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((p) => (
                    <tr key={p.id}>
                      <td className="pname">{p.patientName}</td>
                      <td>
                        <button className="btn btn-ghost btn-sm" onClick={() => setOpenSession(p.sessionId)}>
                          {fmtDateShort(p.sessionDate)} {hm(p.sessionStartTime)}
                        </button>
                        <span className={`badge ${sessionStatusMeta[p.sessionStatus].badge}`}>{sessionStatusMeta[p.sessionStatus].label}</span>
                      </td>
                      <td className="strong">{fmtMoney(p.amount)}</td>
                      <td>
                        <span className={`badge ${paymentStatusMeta[p.status].badge}`}>{paymentStatusMeta[p.status].label}</span>
                      </td>
                      <td className="small muted">
                        {p.paidAt ? `${fmtDateShort(p.paidAt)}${p.method ? ` · ${paymentMethodLabel[p.method]}` : ""}` : p.cancellationReason ?? "—"}
                      </td>
                      <td>
                        <div className="cell-actions">
                          {p.status === "Pending" && p.sessionStatus === "Completed" && (
                            <button className="btn btn-subtle btn-sm" onClick={() => setPaying(p)}>
                              Receber
                            </button>
                          )}
                          {p.status === "Pending" && (
                            <button className="btn btn-ghost btn-icon btn-sm" title="Alterar valor" aria-label="Alterar valor" onClick={() => setEditing(p)}>
                              <Pencil />
                            </button>
                          )}
                          {p.status === "Paid" && (
                            <button className="btn btn-ghost btn-icon btn-sm" title="Recibo" aria-label="Baixar recibo" onClick={() => download(`/documents/receipts/${p.id}`)}>
                              <FileDown />
                            </button>
                          )}
                          {p.status !== "Cancelled" && (
                            <button
                              className="btn btn-ghost btn-icon btn-sm"
                              title={p.status === "Paid" ? "Estornar" : "Cancelar"}
                              aria-label={p.status === "Paid" ? "Estornar" : "Cancelar"}
                              onClick={() => setCancelling(p)}
                            >
                              <Undo2 />
                            </button>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPage={setPage} />
          </>
        )}
      </div>

      {paying && <PayDialog paymentId={paying.id} amount={paying.amount} patientName={paying.patientName} onClose={() => setPaying(null)} onPaid={reload} />}
      {editing && <AmountDialog payment={editing} onClose={() => setEditing(null)} onSaved={reload} />}
      {cancelling && (
        <ReasonDialog
          title={cancelling.status === "Paid" ? "Estornar pagamento" : "Cancelar pagamento"}
          message={`${cancelling.patientName} · ${fmtMoney(cancelling.amount)}`}
          confirmLabel={cancelling.status === "Paid" ? "Estornar" : "Cancelar pagamento"}
          danger
          onClose={() => setCancelling(null)}
          onConfirm={async (reason) => {
            try {
              await api.post(`/payments/${cancelling.id}/cancel`, { reason });
              notify("success", cancelling.status === "Paid" ? "Pagamento estornado." : "Pagamento cancelado.");
              await reload();
            } catch (e) {
              notify("error", errorMessage(e));
            }
          }}
        />
      )}
      {openSession && <SessionModal sessionId={openSession} onClose={() => setOpenSession(null)} onChanged={reload} />}
    </>
  );
}

function AmountDialog({ payment, onClose, onSaved }: { payment: Payment; onClose: () => void; onSaved: () => void }) {
  const { notify } = useStore();
  const [value, setValue] = useState(payment.amount.toFixed(2).replace(".", ","));
  const [busy, setBusy] = useState(false);
  const amount = parseMoney(value);

  async function save() {
    setBusy(true);
    try {
      await api.put(`/payments/${payment.id}`, { amount });
      notify("success", "Valor atualizado.");
      onSaved();
      onClose();
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title="Alterar valor"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" disabled={amount === null || amount < 0 || busy} onClick={save}>
            Salvar
          </button>
        </>
      }
    >
      <div className="field">
        <label className="label" htmlFor="newAmount">
          Valor da sessão (R$)
        </label>
        <input id="newAmount" className="input" inputMode="decimal" value={value} onChange={(e) => setValue(e.target.value)} autoFocus />
      </div>
    </Modal>
  );
}
