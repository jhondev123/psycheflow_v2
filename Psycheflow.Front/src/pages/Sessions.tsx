import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { startOfMonth, endOfMonth } from "date-fns";
import { ClipboardList } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { PagedResponse, Psychologist, SessionListItem, SessionStatus } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading, Pager } from "@/components/ui/Feedback";
import { PatientSelect } from "@/components/PatientSelect";
import { SessionModal } from "@/components/SessionModal";
import { scheduleStatusMeta, sessionStatusMeta } from "@/lib/domain";
import { fmtRelativeDay, hm, isoDate } from "@/lib/format";
import "@/styles/pages.css";

const STATUSES: Array<{ key: "" | SessionStatus; label: string }> = [
  { key: "", label: "Todas" },
  { key: "Scheduled", label: "Agendadas" },
  { key: "Completed", label: "Concluídas" },
  { key: "NoShow", label: "Faltas" },
  { key: "Cancelled", label: "Canceladas" },
];

/** RF005: sessões por período, situação, paciente e psicólogo; registro da sessão no modal. */
export function Sessions() {
  const { isManagement } = useStore();
  const [params, setParams] = useSearchParams();
  const [from, setFrom] = useState(isoDate(startOfMonth(new Date())));
  const [to, setTo] = useState(isoDate(endOfMonth(new Date())));
  const [status, setStatus] = useState<"" | SessionStatus>("");
  const [patientId, setPatientId] = useState("");
  const [psychologistId, setPsychologistId] = useState("");
  const [page, setPage] = useState(1);

  const openId = params.get("id");
  const setOpen = (id: string | null) => setParams(id ? { id } : {}, { replace: true });

  const { data, loading, error, reload } = useAsync(
    () => api.get<PagedResponse<SessionListItem>>("/sessions", { from, to, status, patientId, psychologistId, page, pageSize: 20 }),
    [from, to, status, patientId, psychologistId, page],
    !!from,
  );
  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), [], isManagement);

  function filter<T>(setter: (v: T) => void) {
    return (v: T) => {
      setter(v);
      setPage(1);
    };
  }

  return (
    <>
      <PageHeader title="Sessões" subtitle="Registros e evolução dos atendimentos." />

      <div className="filters">
        <div className="field">
          <label className="label" htmlFor="sFrom">
            De
          </label>
          <input id="sFrom" className="input" type="date" value={from} onChange={(e) => filter(setFrom)(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="sTo">
            Até
          </label>
          <input id="sTo" className="input" type="date" value={to} min={from} onChange={(e) => filter(setTo)(e.target.value)} />
        </div>
        <div className="field" style={{ minWidth: 220 }}>
          <label className="label" htmlFor="sPatient">
            Paciente
          </label>
          <PatientSelect id="sPatient" allowAll value={patientId} onChange={filter(setPatientId)} />
        </div>
        {isManagement && (
          <div className="field">
            <label className="label" htmlFor="sPsy">
              Psicólogo
            </label>
            <select id="sPsy" className="select" value={psychologistId} onChange={(e) => filter(setPsychologistId)(e.target.value)}>
              <option value="">Todos</option>
              {(psychologists.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}
                </option>
              ))}
            </select>
          </div>
        )}
        <div className="cal-views">
          {STATUSES.map((s) => (
            <button key={s.key} className={`cal-view-btn ${status === s.key ? "active" : ""}`} onClick={() => filter(setStatus)(s.key)}>
              {s.label}
            </button>
          ))}
        </div>
      </div>

      <div className="card">
        {loading && !data ? (
          <Loading />
        ) : error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState icon={<ClipboardList />} title="Nenhuma sessão no período" description="Ajuste os filtros ou agende pela agenda." />
        ) : (
          <>
            <div className="table-scroll">
              <table className="table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Data</th>
                    <th>Horário</th>
                    {isManagement && <th>Psicólogo</th>}
                    <th>Situação</th>
                    <th style={{ width: 110 }}></th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((s) => {
                    const meta = sessionStatusMeta[s.status];
                    return (
                      <tr key={s.id}>
                        <td>
                          <div className="person">
                            <Avatar name={s.patientName} size="sm" />
                            <div className="pname truncate">{s.patientName}</div>
                          </div>
                        </td>
                        <td className="muted">{fmtRelativeDay(s.date)}</td>
                        <td className="mono muted">
                          {hm(s.startTime)}–{hm(s.endTime)}
                        </td>
                        {isManagement && <td className="muted">{s.psychologistName}</td>}
                        <td>
                          <div className="row gap-1 wrap">
                            <span className={`badge ${meta.badge}`}>
                              <span className="dot" /> {meta.label}
                            </span>
                            {s.status === "Scheduled" && (
                              <span className={`badge ${scheduleStatusMeta[s.scheduleStatus].badge}`}>{scheduleStatusMeta[s.scheduleStatus].label}</span>
                            )}
                          </div>
                        </td>
                        <td>
                          <div className="cell-actions">
                            <button className="btn btn-subtle btn-sm" onClick={() => setOpen(s.id)}>
                              {s.status === "Scheduled" ? "Registrar" : "Ver"}
                            </button>
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <Pager page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPage={setPage} />
          </>
        )}
      </div>

      {openId && <SessionModal sessionId={openId} onClose={() => setOpen(null)} onChanged={reload} />}
    </>
  );
}
