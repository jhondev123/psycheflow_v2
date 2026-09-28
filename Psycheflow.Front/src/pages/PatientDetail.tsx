import { useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { ArrowLeft, Brain, CalendarPlus, FileDown, FolderLock, Lightbulb, Pencil, Repeat, Wallet } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type {
  AiSettings,
  AiSuggestion,
  PagedResponse,
  Patient,
  Payment,
  Recurrence,
  RecurrenceGeneration,
  SessionListItem,
} from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { ReasonDialog } from "@/components/ui/ReasonDialog";
import { PatientForm } from "@/components/PatientForm";
import { SessionModal } from "@/components/SessionModal";
import { PayDialog } from "@/components/PayDialog";
import { AiSuggestionModal } from "@/components/AiSuggestionModal";
import { patientStatusMeta, paymentMethodLabel, paymentStatusMeta, recurrenceTypeLabel, sessionStatusMeta, WEEKDAYS_LONG } from "@/lib/domain";
import { fmtCpf, fmtDateShort, fmtMoney, fmtPhone, hm, parseISODate } from "@/lib/format";
import "@/styles/pages.css";

function ageFrom(birthDate: string): number {
  const birth = parseISODate(birthDate);
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  if (today < new Date(today.getFullYear(), birth.getMonth(), birth.getDate())) age--;
  return age;
}

export function PatientDetail() {
  const { id = "" } = useParams();
  const navigate = useNavigate();
  const { isPsychologist, notify } = useStore();

  const patient = useAsync(() => api.get<Patient>(`/patients/${id}`), [id]);
  const sessions = useAsync(
    () => api.get<PagedResponse<SessionListItem>>("/sessions", { patientId: id, from: "2000-01-01", pageSize: 100 }),
    [id],
  );
  const recurrences = useAsync(() => api.get<Recurrence[]>("/recurrences", { patientId: id }), [id]);
  const payments = useAsync(() => api.get<PagedResponse<Payment>>("/payments", { patientId: id, pageSize: 100 }), [id]);
  const ai = useAsync(() => api.get<AiSettings>("/ai/settings"), []);

  const [editing, setEditing] = useState(false);
  const [openSession, setOpenSession] = useState<string | null>(null);
  const [paying, setPaying] = useState<Payment | null>(null);
  const [ending, setEnding] = useState<Recurrence | null>(null);
  const [suggestion, setSuggestion] = useState<"patient-analysis" | "next-steps" | null>(null);

  if (patient.loading && !patient.data) return <Loading />;
  if (patient.error || !patient.data) return <ErrorState message={patient.error ?? "Paciente não encontrado."} onRetry={patient.reload} />;
  const p = patient.data;

  const reloadAll = () => {
    void sessions.reload();
    void payments.reload();
    void recurrences.reload();
  };

  const aiEnabled = isPsychologist && !!ai.data?.isEnabled;
  const sessionRows = [...(sessions.data?.items ?? [])].reverse();

  async function extend(r: Recurrence) {
    try {
      const result = await api.post<RecurrenceGeneration>(`/recurrences/${r.id}/extend`);
      notify("success", `${result.scheduled.length} sessões agendadas até ${fmtDateShort(result.recurrence.generatedUntil)}.`);
      if (result.skipped.length) notify("info", `${result.skipped.length} datas foram puladas (agenda indisponível).`);
      reloadAll();
    } catch (e) {
      notify("error", errorMessage(e));
    }
  }

  async function download(path: string, query?: Record<string, string>) {
    try {
      await api.download(path, query);
    } catch (e) {
      notify("error", errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader
        title={p.fullName}
        subtitle={`Paciente desde ${fmtDateShort(p.createdAt)}`}
        actions={
          <>
            <button className="btn btn-ghost" onClick={() => navigate("/pacientes")}>
              <ArrowLeft /> Voltar
            </button>
            {aiEnabled && (
              <>
                <button className="btn btn-subtle" onClick={() => setSuggestion("patient-analysis")} title="Análise com IA (dados pseudonimizados)">
                  <Brain /> Análise com IA
                </button>
                <button className="btn btn-subtle" onClick={() => setSuggestion("next-steps")} title="Próximos passos com IA (dados pseudonimizados)">
                  <Lightbulb /> Próximos passos
                </button>
              </>
            )}
            <button className="btn btn-primary" onClick={() => setEditing(true)}>
              <Pencil /> Editar
            </button>
          </>
        }
      />

      <div className="split" style={{ marginBottom: 20 }}>
        <div className="card card-pad stack">
          <div className="profile-head">
            <Avatar name={p.fullName} size="lg" />
            <div>
              <div className="strong" style={{ fontSize: 17 }}>
                {p.fullName}
              </div>
              <span className={`badge ${patientStatusMeta[p.status].badge}`}>{patientStatusMeta[p.status].label}</span>
            </div>
          </div>
          <div className="detail-grid">
            <Info k="CPF" v={<span className="mono">{fmtCpf(p.cpf)}</span>} />
            <Info k="Nascimento" v={p.birthDate ? `${fmtDateShort(p.birthDate)} (${ageFrom(p.birthDate)} anos)` : "—"} />
            <Info k="E-mail" v={p.email} />
            <Info k="Telefone" v={fmtPhone(p.phone)} />
            <Info
              k="Endereço"
              v={
                p.address
                  ? `${p.address.street}, ${p.address.number}${p.address.complement ? ` — ${p.address.complement}` : ""} · ${p.address.neighborhood} · ${p.address.city}/${p.address.state}`
                  : "—"
              }
            />
          </div>
          {p.notes && (
            <div>
              <div className="section-title">Observações</div>
              <div className="small" style={{ whiteSpace: "pre-wrap" }}>
                {p.notes}
              </div>
            </div>
          )}
        </div>

        <div className="stack">
          <div className="card card-pad stack">
            <div className="section-title" style={{ margin: 0 }}>
              Atalhos
            </div>
            <Link className="btn btn-subtle" to="/agenda">
              <CalendarPlus size={16} /> Agendar na agenda
            </Link>
            {isPsychologist && (
              <>
                <Link className="btn btn-subtle" to={`/prontuarios?patientId=${p.id}`}>
                  <FolderLock size={16} /> Prontuário
                </Link>
                <button className="btn btn-subtle" onClick={() => download("/documents/feedback-report", { patientId: p.id })}>
                  <FileDown size={16} /> Relatório de feedback (PDF)
                </button>
                <Link className="btn btn-subtle" to={`/documentos?patientId=${p.id}`}>
                  <FileDown size={16} /> Laudos e relatórios
                </Link>
              </>
            )}
          </div>

          <div className="card card-pad stack">
            <div className="section-title" style={{ margin: 0 }}>
              <Repeat size={13} /> Recorrências
            </div>
            {(recurrences.data ?? []).length === 0 ? (
              <div className="muted small">Nenhuma recorrência. Crie pela agenda marcando “Repetir”.</div>
            ) : (
              recurrences.data!.map((r) => (
                <div key={r.id} className="col gap-1" style={{ borderBottom: "1px solid var(--border)", paddingBottom: 10 }}>
                  <div className="row between wrap gap-2">
                    <span className="strong small">
                      {recurrenceTypeLabel[r.type]} · {WEEKDAYS_LONG[parseISODate(r.startDate).getDay()]} às {hm(r.startTime)}
                    </span>
                    <span className={`badge ${r.isActive ? "badge-success" : ""}`}>{r.isActive ? "Ativa" : "Encerrada"}</span>
                  </div>
                  <div className="tiny muted">
                    Desde {fmtDateShort(r.startDate)} · gerada até {fmtDateShort(r.generatedUntil)} · {fmtMoney(r.price)}
                    {r.endReason && ` · ${r.endReason}`}
                  </div>
                  {r.isActive && (
                    <div className="row gap-2">
                      <button className="btn btn-ghost btn-sm" onClick={() => extend(r)}>
                        Estender 3 meses
                      </button>
                      <button className="btn btn-ghost btn-sm" onClick={() => setEnding(r)}>
                        Encerrar
                      </button>
                    </div>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      <div className="split">
        <div className="card">
          <div className="card-head">
            <div className="card-title">Sessões</div>
            <span className="count-pill">{sessions.data?.totalCount ?? 0}</span>
          </div>
          {sessions.loading && !sessions.data ? (
            <Loading />
          ) : sessionRows.length === 0 ? (
            <EmptyState icon={<CalendarPlus />} title="Nenhuma sessão" description="Agende a primeira sessão pela agenda." />
          ) : (
            <div className="rows">
              {sessionRows.map((s) => (
                <div className="list-row" key={s.id} onClick={() => setOpenSession(s.id)}>
                  <div className="time-chip">
                    <span className="t">{hm(s.startTime)}</span>
                    <span className="d">{fmtDateShort(s.date)}</span>
                  </div>
                  <div className="grow truncate">
                    <div className="pname truncate">{s.psychologistName}</div>
                  </div>
                  <span className={`badge ${sessionStatusMeta[s.status].badge}`}>{sessionStatusMeta[s.status].label}</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="card">
          <div className="card-head">
            <div className="card-title">
              <Wallet size={16} /> Pagamentos
            </div>
          </div>
          {(payments.data?.items ?? []).length === 0 ? (
            <EmptyState icon={<Wallet />} title="Sem pagamentos" />
          ) : (
            <div className="rows">
              {[...payments.data!.items].reverse().map((pay) => (
                <div className="list-row" key={pay.id}>
                  <div className="grow truncate">
                    <div className="pname">{fmtMoney(pay.amount)}</div>
                    <div className="psub">
                      Sessão de {fmtDateShort(pay.sessionDate)}
                      {pay.method && ` · ${paymentMethodLabel[pay.method]}`}
                      {pay.paidAt && ` · pago em ${fmtDateShort(pay.paidAt)}`}
                    </div>
                  </div>
                  <span className={`badge ${paymentStatusMeta[pay.status].badge}`}>{paymentStatusMeta[pay.status].label}</span>
                  {pay.status === "Pending" && pay.sessionStatus === "Completed" && (
                    <button className="btn btn-subtle btn-sm" onClick={() => setPaying(pay)}>
                      Receber
                    </button>
                  )}
                  {pay.status === "Paid" && (
                    <button className="btn btn-ghost btn-sm" onClick={() => download(`/documents/receipts/${pay.id}`)} aria-label="Baixar recibo" title="Recibo">
                      <FileDown size={15} />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {editing && (
        <PatientForm
          patient={p}
          onClose={() => setEditing(false)}
          onSaved={(saved) => {
            patient.setData(saved);
            setEditing(false);
          }}
        />
      )}
      {openSession && <SessionModal sessionId={openSession} onClose={() => setOpenSession(null)} onChanged={reloadAll} />}
      {paying && (
        <PayDialog paymentId={paying.id} amount={paying.amount} patientName={p.fullName} onClose={() => setPaying(null)} onPaid={reloadAll} />
      )}
      {ending && (
        <ReasonDialog
          title="Encerrar recorrência"
          message="As sessões futuras ainda agendadas desta recorrência serão canceladas."
          confirmLabel="Encerrar"
          required={false}
          danger
          onClose={() => setEnding(null)}
          onConfirm={async (reason) => {
            try {
              await api.post(`/recurrences/${ending.id}/end`, { reason: reason || null });
              notify("success", "Recorrência encerrada.");
              reloadAll();
            } catch (e) {
              notify("error", errorMessage(e));
            }
          }}
        />
      )}
      {suggestion && (
        <AiSuggestionModal
          title={suggestion === "patient-analysis" ? `Análise do acompanhamento — ${p.fullName}` : `Próximos passos — ${p.fullName}`}
          request={() => api.post<AiSuggestion>(`/ai/suggestions/${suggestion}`, { patientId: p.id })}
          onClose={() => setSuggestion(null)}
        />
      )}
    </>
  );
}

function Info({ k, v }: { k: string; v: React.ReactNode }) {
  return (
    <div className="info">
      <span className="k">{k}</span>
      <span className="v">{v}</span>
    </div>
  );
}
