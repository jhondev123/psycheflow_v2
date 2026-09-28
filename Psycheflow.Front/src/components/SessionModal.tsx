import { useState } from "react";
import { Link } from "react-router-dom";
import { CalendarClock, Clock, FileDown, Lock, Repeat, Sparkles, Trash2, User, Wallet } from "lucide-react";
import { Modal, ConfirmDialog } from "@/components/ui/Modal";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { Markdown } from "@/components/ui/Markdown";
import { ReasonDialog } from "@/components/ui/ReasonDialog";
import { AiSuggestionModal } from "@/components/AiSuggestionModal";
import { PayDialog } from "@/components/PayDialog";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import { paymentStatusMeta, scheduleStatusMeta, sessionStatusMeta } from "@/lib/domain";
import { capitalize, fmtDateLong, fmtMoney, hm } from "@/lib/format";
import { useStore } from "@/store/AppStore";
import type { AiSettings, AiSuggestion, Session } from "@/types";

type Mode = "view" | "complete" | "notes" | "reschedule";

interface SessionModalProps {
  sessionId: string;
  onClose: () => void;
  /** Avisado depois de qualquer alteração (para a tela recarregar a lista/agenda). */
  onChanged?: () => void;
}

/** Detalhe da sessão com o ciclo de vida completo (RF004–RF009) e as ações financeiras/documentos ligadas a ela. */
export function SessionModal({ sessionId, onClose, onChanged }: SessionModalProps) {
  const { me, notify } = useStore();
  const { data: session, loading, error, reload } = useAsync(() => api.get<Session>(`/sessions/${sessionId}`), [sessionId]);
  const ai = useAsync(() => api.get<AiSettings>("/ai/settings"), []);

  const [mode, setMode] = useState<Mode>("view");
  const [busy, setBusy] = useState(false);
  const [dialog, setDialog] = useState<"cancel" | "delete" | "pay" | null>(null);

  async function act(action: () => Promise<unknown>, success: string, close = false) {
    setBusy(true);
    try {
      await action();
      notify("success", success);
      onChanged?.();
      if (close) onClose();
      else {
        setMode("view");
        await reload();
      }
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  const isOwn = !!session && session.psychologistId === me?.psychologistId;
  const aiForNotes = isOwn && !!ai.data?.isEnabled && ai.data.shareSessionNotes;

  let body: React.ReactNode = null;
  let footer: React.ReactNode = null;

  if (loading && !session) body = <Loading />;
  else if (error) body = <ErrorState message={error} onRetry={reload} />;
  else if (session) {
    const status = sessionStatusMeta[session.status];
    const scheduled = session.status === "Scheduled";

    body = (
      <div className="col gap-4">
        <div className="ev-meta">
          <div className="ev-line">
            <User />
            <Link className="strong" to={`/pacientes/${session.patientId}`} onClick={onClose}>
              {session.patientName}
            </Link>
            <span className="muted small">com {session.psychologistName}</span>
          </div>
          <div className="ev-line">
            <Clock />
            <span>
              {capitalize(fmtDateLong(session.date))} · {hm(session.startTime)}–{hm(session.endTime)}{" "}
              <span className="muted">({session.durationMinutes} min)</span>
            </span>
          </div>
          <div className="ev-line">
            <span style={{ width: 18 }} />
            <span className="row gap-2 wrap">
              <span className={`badge ${status.badge}`}>
                <span className="dot" /> {status.label}
              </span>
              {scheduled && <span className={`badge ${scheduleStatusMeta[session.scheduleStatus].badge}`}>{scheduleStatusMeta[session.scheduleStatus].label}</span>}
              {session.recurrenceId && (
                <span className="badge">
                  <Repeat size={12} /> Recorrente
                </span>
              )}
              {session.payment && (
                <span className={`badge ${paymentStatusMeta[session.payment.status].badge}`}>
                  <Wallet size={12} /> {fmtMoney(session.payment.amount)} · {paymentStatusMeta[session.payment.status].label}
                </span>
              )}
            </span>
          </div>
        </div>

        {session.cancellationReason && <div className="notice warning">Motivo do cancelamento: {session.cancellationReason}</div>}
        {session.rescheduleReason && scheduled && <div className="notice">Reagendada: {session.rescheduleReason}</div>}

        {mode === "view" && <ClinicalNotes session={session} />}
        {mode === "complete" && (
          <NotesForm
            session={session}
            withFeedback
            aiEnabled={aiForNotes}
            busy={busy}
            onCancel={() => setMode("view")}
            onSubmit={(notes, score, comment) =>
              act(() => api.post(`/sessions/${session.id}/complete`, { notes, feedbackScore: score, feedbackComment: comment }), "Sessão concluída.")
            }
          />
        )}
        {mode === "notes" && (
          <NotesForm
            session={session}
            aiEnabled={aiForNotes}
            busy={busy}
            onCancel={() => setMode("view")}
            onSubmit={(notes) => act(() => api.put(`/sessions/${session.id}`, { patientId: session.patientId, notes }), "Anotações salvas.")}
          />
        )}
        {mode === "reschedule" && (
          <RescheduleForm
            session={session}
            busy={busy}
            onCancel={() => setMode("view")}
            onSubmit={(body) => act(() => api.post(`/sessions/${session.id}/reschedule`, body), "Sessão reagendada.")}
          />
        )}
      </div>
    );

    if (mode === "view") {
      footer = (
        <>
          {session.status !== "Completed" && (
            <button className="btn btn-ghost btn-icon" title="Excluir" aria-label="Excluir sessão" onClick={() => setDialog("delete")}>
              <Trash2 />
            </button>
          )}
          <div className="grow" />
          {scheduled && (
            <>
              <button className="btn btn-ghost" disabled={busy} onClick={() => act(() => api.post(`/sessions/${session.id}/no-show`), "Falta registrada.")}>
                Falta
              </button>
              <button className="btn btn-ghost" disabled={busy} onClick={() => setDialog("cancel")}>
                Cancelar
              </button>
              <button className="btn btn-subtle" disabled={busy} onClick={() => setMode("reschedule")}>
                <CalendarClock size={15} /> Reagendar
              </button>
              {session.scheduleStatus === "Pending" && (
                <button className="btn btn-subtle" disabled={busy} onClick={() => act(() => api.post(`/sessions/${session.id}/confirm`), "Sessão confirmada.")}>
                  Confirmar
                </button>
              )}
              {isOwn && (
                <button className="btn btn-primary" disabled={busy} onClick={() => setMode("complete")}>
                  Concluir
                </button>
              )}
            </>
          )}
          {session.status === "Completed" && (
            <>
              <button
                className="btn btn-ghost"
                onClick={() => api.download(`/documents/attendance/${session.id}`).catch((e) => notify("error", errorMessage(e)))}
              >
                <FileDown size={15} /> Declaração
              </button>
              {session.payment?.status === "Paid" && (
                <button
                  className="btn btn-ghost"
                  onClick={() => api.download(`/documents/receipts/${session.payment!.id}`).catch((e) => notify("error", errorMessage(e)))}
                >
                  <FileDown size={15} /> Recibo
                </button>
              )}
              {session.payment?.status === "Pending" && (
                <button className="btn btn-subtle" onClick={() => setDialog("pay")}>
                  <Wallet size={15} /> Receber
                </button>
              )}
              {isOwn && (
                <button className="btn btn-primary" onClick={() => setMode("notes")}>
                  Editar anotações
                </button>
              )}
            </>
          )}
        </>
      );
    }
  }

  return (
    <>
      <Modal title="Sessão" size="lg" onClose={onClose} footer={footer}>
        {body}
      </Modal>

      {session && dialog === "cancel" && (
        <ReasonDialog
          title="Cancelar sessão"
          message="O horário é liberado na agenda e o pagamento pendente é cancelado."
          confirmLabel="Cancelar sessão"
          danger
          onClose={() => setDialog(null)}
          onConfirm={(reason) => act(() => api.post(`/sessions/${session.id}/cancel`, { reason }), "Sessão cancelada.")}
        />
      )}
      {session && dialog === "delete" && (
        <ConfirmDialog
          title="Excluir sessão"
          danger
          confirmLabel="Excluir"
          message="A sessão sai da agenda e dos relatórios. Para manter o histórico, prefira cancelar."
          onClose={() => setDialog(null)}
          onConfirm={() => void act(() => api.del(`/sessions/${session.id}`), "Sessão excluída.", true)}
        />
      )}
      {session?.payment && dialog === "pay" && (
        <PayDialog
          paymentId={session.payment.id}
          amount={session.payment.amount}
          patientName={session.patientName}
          onClose={() => setDialog(null)}
          onPaid={() => {
            onChanged?.();
            void reload();
          }}
        />
      )}
    </>
  );
}

function ClinicalNotes({ session }: { session: Session }) {
  if (!session.clinicalNotesVisible) {
    return (
      <div className="notice">
        <Lock />
        <span>Anotações e feedback são sigilosos: só o psicólogo da sessão pode vê-los.</span>
      </div>
    );
  }
  if (!session.notes && session.feedbackScore === null) {
    return <div className="muted small">Sem anotações registradas.</div>;
  }
  return (
    <div className="col gap-3">
      {session.notes && (
        <div>
          <div className="section-title">Anotações</div>
          <Markdown text={session.notes} />
        </div>
      )}
      {session.feedbackScore !== null && (
        <div>
          <div className="section-title">Feedback do paciente</div>
          <div>
            <span className="strong">{session.feedbackScore}/10</span>
            {session.feedbackComment && <span className="muted"> — “{session.feedbackComment}”</span>}
          </div>
        </div>
      )}
    </div>
  );
}

interface NotesFormProps {
  session: Session;
  withFeedback?: boolean;
  aiEnabled: boolean;
  busy: boolean;
  onCancel: () => void;
  onSubmit: (notes: string, score: number | null, comment: string | null) => Promise<void>;
}

function NotesForm({ session, withFeedback, aiEnabled, busy, onCancel, onSubmit }: NotesFormProps) {
  const [notes, setNotes] = useState(session.notes ?? "");
  const [score, setScore] = useState<number | null>(session.feedbackScore);
  const [comment, setComment] = useState(session.feedbackComment ?? "");
  const [askAi, setAskAi] = useState(false);
  const invalid = !notes.trim() || (withFeedback && score === null);

  return (
    <div className="col gap-4">
      <div className="field">
        <div className="row between">
          <label className="label" htmlFor="notes">
            Anotações da sessão (Markdown) <span className="req">*</span>
          </label>
          {aiEnabled && (
            <button type="button" className="btn btn-ghost btn-sm" disabled={!notes.trim()} onClick={() => setAskAi(true)} title="Organiza o rascunho com IA (dados pseudonimizados)">
              <Sparkles size={15} /> Organizar com IA
            </button>
          )}
        </div>
        <textarea
          id="notes"
          className="textarea"
          style={{ minHeight: 160 }}
          placeholder="Temas abordados, intervenções, observações clínicas, combinados…"
          value={notes}
          maxLength={20000}
          onChange={(e) => setNotes(e.target.value)}
        />
      </div>

      {withFeedback && (
        <>
          <div className="field">
            <span className="label">
              Feedback do paciente (0 a 10) <span className="req">*</span>
            </span>
            <div className="score-picker" role="radiogroup" aria-label="Nota de 0 a 10">
              {Array.from({ length: 11 }, (_, n) => (
                <button key={n} type="button" role="radio" aria-checked={score === n} className={score === n ? "active" : ""} onClick={() => setScore(n)}>
                  {n}
                </button>
              ))}
            </div>
          </div>
          <div className="field">
            <label className="label" htmlFor="comment">
              Comentário do paciente
            </label>
            <input id="comment" className="input" maxLength={1000} value={comment} onChange={(e) => setComment(e.target.value)} />
          </div>
        </>
      )}

      <div className="row gap-2" style={{ justifyContent: "flex-end" }}>
        <button className="btn btn-ghost" onClick={onCancel}>
          Voltar
        </button>
        <button className="btn btn-primary" disabled={invalid || busy} onClick={() => onSubmit(notes.trim(), score, comment.trim() || null)}>
          {busy ? "Salvando…" : withFeedback ? "Concluir sessão" : "Salvar anotações"}
        </button>
      </div>

      {askAi && (
        <AiSuggestionModal
          title="Anotações organizadas pela IA"
          request={() => api.post<AiSuggestion>("/ai/suggestions/session-notes", { sessionId: session.id, draft: notes })}
          onClose={() => setAskAi(false)}
          onUse={setNotes}
        />
      )}
    </div>
  );
}

interface RescheduleFormProps {
  session: Session;
  busy: boolean;
  onCancel: () => void;
  onSubmit: (body: { date: string; startTime: string; durationMinutes: number; reason: string }) => Promise<void>;
}

function RescheduleForm({ session, busy, onCancel, onSubmit }: RescheduleFormProps) {
  const [date, setDate] = useState(session.date);
  const [startTime, setStartTime] = useState(hm(session.startTime));
  const [duration, setDuration] = useState(session.durationMinutes);
  const [reason, setReason] = useState("");

  return (
    <div className="grid-form">
      <div className="field">
        <label className="label" htmlFor="rDate">
          Nova data
        </label>
        <input id="rDate" className="input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
      </div>
      <div className="field">
        <label className="label" htmlFor="rTime">
          Início
        </label>
        <input id="rTime" className="input" type="time" step={300} value={startTime} onChange={(e) => setStartTime(e.target.value)} />
      </div>
      <div className="field">
        <label className="label" htmlFor="rDuration">
          Duração (min)
        </label>
        <input id="rDuration" className="input" type="number" min={15} max={240} value={duration} onChange={(e) => setDuration(Number(e.target.value))} />
      </div>
      <div className="field span-2">
        <label className="label" htmlFor="rReason">
          Motivo <span className="req">*</span>
        </label>
        <input id="rReason" className="input" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} placeholder="Pedido do paciente, imprevisto…" />
      </div>
      <div className="row gap-2 span-2" style={{ justifyContent: "flex-end" }}>
        <button className="btn btn-ghost" onClick={onCancel}>
          Voltar
        </button>
        <button className="btn btn-primary" disabled={!reason.trim() || !date || !startTime || busy} onClick={() => onSubmit({ date, startTime, durationMinutes: duration, reason: reason.trim() })}>
          {busy ? "Salvando…" : "Reagendar"}
        </button>
      </div>
    </div>
  );
}
