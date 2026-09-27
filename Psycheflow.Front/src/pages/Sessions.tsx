import { useMemo, useState } from "react";
import { ClipboardList, Search } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { type Schedule, type Session, SessionStatus } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { sessionStatusMeta } from "@/lib/domain";
import { durationLabel } from "@/lib/time";
import { fmtDateLong, fmtRelativeDay } from "@/lib/format";
import "@/styles/pages.css";

interface Row {
  session: Session;
  schedule?: Schedule;
  patientName: string;
}

const FILTERS: Array<{ key: "all" | SessionStatus; label: string }> = [
  { key: "all", label: "Todas" },
  { key: SessionStatus.Scheduled, label: "Agendadas" },
  { key: SessionStatus.Completed, label: "Concluídas" },
  { key: SessionStatus.NoShow, label: "Faltas" },
];

export function Sessions() {
  const { db, activePsychologist } = useStore();
  const psyId = activePsychologist?.id ?? "";
  const [query, setQuery] = useState("");
  const [filter, setFilter] = useState<"all" | SessionStatus>("all");
  const [open, setOpen] = useState<Row | null>(null);

  const rows = useMemo<Row[]>(() => {
    const q = query.trim().toLowerCase();
    return db.sessions
      .filter((s) => s.psychologistId === psyId)
      .map((session) => {
        const schedule = db.schedules.find((sc) => sc.id === session.scheduleId);
        const patientName = db.patients.find((p) => p.id === session.patientId)?.name ?? "Paciente";
        return { session, schedule, patientName };
      })
      .filter((r) => (filter === "all" ? true : r.session.status === filter))
      .filter((r) => !q || r.patientName.toLowerCase().includes(q))
      .sort((a, b) => {
        const da = a.schedule?.date ?? "";
        const dbb = b.schedule?.date ?? "";
        return dbb.localeCompare(da) || (b.schedule?.start ?? "").localeCompare(a.schedule?.start ?? "");
      });
  }, [db.sessions, db.schedules, db.patients, psyId, query, filter]);

  return (
    <>
      <PageHeader title="Sessões" subtitle="Registros e evolução dos atendimentos." />

      <div className="toolbar">
        <div className="search">
          <Search />
          <input
            className="input"
            placeholder="Buscar por paciente…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <div className="cal-views">
          {FILTERS.map((f) => (
            <button
              key={String(f.key)}
              className={`cal-view-btn ${filter === f.key ? "active" : ""}`}
              onClick={() => setFilter(f.key)}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      <div className="card">
        {rows.length === 0 ? (
          <EmptyState
            icon={<ClipboardList />}
            title="Nenhuma sessão encontrada"
            description="As sessões agendadas na agenda aparecerão aqui para registro."
          />
        ) : (
          <div className="table-scroll">
            <table className="table">
              <thead>
                <tr>
                  <th>Paciente</th>
                  <th>Data</th>
                  <th>Horário</th>
                  <th>Situação</th>
                  <th style={{ width: 120 }}></th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r) => {
                  const meta = sessionStatusMeta[r.session.status];
                  return (
                    <tr key={r.session.id}>
                      <td>
                        <div className="person">
                          <Avatar name={r.patientName} size="sm" />
                          <div className="truncate">
                            <div className="pname truncate">{r.patientName}</div>
                            {r.session.feedback && (
                              <div className="psub truncate">{r.session.feedback}</div>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="muted">
                        {r.schedule ? fmtRelativeDay(r.schedule.date) : "—"}
                      </td>
                      <td className="mono muted">
                        {r.schedule ? `${r.schedule.start}–${r.schedule.end}` : "—"}
                      </td>
                      <td>
                        <span className={`badge ${meta.badge}`}>
                          <span className="dot" /> {meta.label}
                        </span>
                      </td>
                      <td>
                        <div className="cell-actions">
                          <button className="btn btn-subtle btn-sm" onClick={() => setOpen(r)}>
                            {r.session.status === SessionStatus.Completed ? "Ver" : "Registrar"}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {open && <SessionModal row={open} onClose={() => setOpen(null)} />}
    </>
  );
}

function SessionModal({ row, onClose }: { row: Row; onClose: () => void }) {
  const { completeSession, setSessionStatus, notify } = useStore();
  const { session, schedule, patientName } = row;
  const [feedback, setFeedback] = useState(session.feedback);
  const [description, setDescription] = useState(session.description);
  const meta = sessionStatusMeta[session.status];
  const isCompleted = session.status === SessionStatus.Completed;

  function save() {
    completeSession(session.id, feedback.trim(), description.trim());
    notify("success", "Sessão registrada como concluída.");
    onClose();
  }

  return (
    <Modal
      title="Registro da sessão"
      size="lg"
      onClose={onClose}
      footer={
        <>
          {!isCompleted && (
            <button
              className="btn btn-ghost"
              onClick={() => {
                setSessionStatus(session.id, SessionStatus.NoShow);
                notify("info", "Sessão marcada como falta.");
                onClose();
              }}
            >
              Marcar falta
            </button>
          )}
          <div className="grow" />
          <button className="btn btn-ghost" onClick={onClose}>
            Fechar
          </button>
          <button className="btn btn-primary" onClick={save}>
            {isCompleted ? "Salvar" : "Concluir sessão"}
          </button>
        </>
      }
    >
      <div className="col gap-4">
        <div className="row gap-3 between wrap">
          <div className="person">
            <Avatar name={patientName} />
            <div>
              <div className="pname">{patientName}</div>
              <div className="psub">
                {schedule
                  ? `${fmtDateLong(schedule.date)} · ${schedule.start}–${schedule.end} (${durationLabel(
                      schedule.start,
                      schedule.end,
                    )})`
                  : "Sem horário vinculado"}
              </div>
            </div>
          </div>
          <span className={`badge ${meta.badge}`}>
            <span className="dot" /> {meta.label}
          </span>
        </div>

        <hr className="divider" />

        <div className="field">
          <label className="label">Anotações da sessão</label>
          <textarea
            className="textarea"
            style={{ minHeight: 120 }}
            placeholder="Temas abordados, técnicas utilizadas, observações clínicas…"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <div className="field">
          <label className="label">Evolução / feedback</label>
          <textarea
            className="textarea"
            placeholder="Evolução do paciente, encaminhamentos, próximos passos…"
            value={feedback}
            onChange={(e) => setFeedback(e.target.value)}
          />
        </div>
      </div>
    </Modal>
  );
}
