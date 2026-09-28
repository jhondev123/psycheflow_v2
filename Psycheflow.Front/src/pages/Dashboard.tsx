import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { CalendarCheck, CalendarClock, CalendarPlus, Clock3, HandCoins, TrendingUp, Users } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { Dashboard as DashboardData, Psychologist } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { scheduleStatusMeta, sessionStatusMeta } from "@/lib/domain";
import { durationLabel } from "@/lib/time";
import { fmtMoney, fmtRelativeDay, hm } from "@/lib/format";
import "@/styles/pages.css";

export function Dashboard() {
  const { isManagement } = useStore();
  const navigate = useNavigate();
  const [psychologistId, setPsychologistId] = useState("");

  const { data, loading, error, reload } = useAsync(
    () => api.get<DashboardData>("/dashboard", { psychologistId }),
    [psychologistId],
  );
  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), [], isManagement);

  const stats = data
    ? [
        { label: "Pacientes ativos", value: data.activePatients, icon: <Users />, color: "var(--brand-500)", bg: "var(--brand-50)" },
        { label: "Sessões nesta semana", value: data.sessionsThisWeek, icon: <CalendarClock />, color: "var(--info)", bg: "var(--info-bg)" },
        { label: `Confirmadas hoje (de ${data.sessionsToday})`, value: data.confirmedToday, icon: <CalendarCheck />, color: "var(--success)", bg: "var(--success-bg)" },
        { label: "A confirmar", value: data.pendingConfirmations, icon: <Clock3 />, color: "var(--warning)", bg: "var(--warning-bg)" },
        { label: `A receber (${data.finance.pendingPayments})`, value: fmtMoney(data.finance.pendingAmount), icon: <HandCoins />, color: "var(--danger)", bg: "var(--danger-bg)" },
        { label: "Recebido no mês", value: fmtMoney(data.finance.receivedThisMonth), icon: <TrendingUp />, color: "var(--success)", bg: "var(--success-bg)" },
      ]
    : [];

  return (
    <>
      <PageHeader
        title="Painel"
        subtitle="Visão geral do dia e da semana."
        actions={
          <>
            {isManagement && (
              <select className="select" value={psychologistId} onChange={(e) => setPsychologistId(e.target.value)} aria-label="Psicólogo">
                <option value="">Toda a clínica</option>
                {(psychologists.data ?? []).map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.fullName}
                  </option>
                ))}
              </select>
            )}
            <button className="btn btn-primary" onClick={() => navigate("/agenda")}>
              <CalendarPlus /> Ir para a agenda
            </button>
          </>
        }
      />

      {loading && !data && <Loading />}
      {error && <ErrorState message={error} onRetry={reload} />}

      {data && (
        <>
          <div className="stat-grid" style={{ marginBottom: 20 }}>
            {stats.map((s) => (
              <div className="card stat" key={s.label}>
                <div className="stat-ic" style={{ background: s.bg, color: s.color }}>
                  {s.icon}
                </div>
                <div>
                  <div className="value">{s.value}</div>
                  <div className="label">{s.label}</div>
                </div>
              </div>
            ))}
          </div>

          <div className="split">
            <div className="card">
              <div className="card-head">
                <div className="card-title">Próximos atendimentos</div>
                <Link className="btn btn-ghost btn-sm" to="/agenda">
                  Ver agenda
                </Link>
              </div>
              {data.upcoming.length === 0 ? (
                <EmptyState icon={<CalendarClock />} title="Nenhum atendimento agendado" description="Que tal agendar a próxima sessão?" />
              ) : (
                <div className="rows">
                  {data.upcoming.map((s) => {
                    const meta = scheduleStatusMeta[s.scheduleStatus];
                    return (
                      <div className="list-row up-next" key={s.sessionId} onClick={() => navigate(`/sessoes?id=${s.sessionId}`)}>
                        <span className="bar" style={{ background: meta.color }} />
                        <div className="time-chip">
                          <span className="t">{hm(s.startTime)}</span>
                          <span className="d">{durationLabel(hm(s.startTime), hm(s.endTime))}</span>
                        </div>
                        <Avatar name={s.patientName} size="sm" />
                        <div className="grow truncate">
                          <div className="pname truncate">{s.patientName}</div>
                          <div className="psub">{fmtRelativeDay(s.date)}</div>
                        </div>
                        <span className={`badge ${meta.badge}`}>{meta.label}</span>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            <div className="card">
              <div className="card-head">
                <div className="card-title">Hoje</div>
                <span className="count-pill">{data.todayItems.length}</span>
              </div>
              {data.todayItems.length === 0 ? (
                <EmptyState icon={<CalendarCheck />} title="Dia livre" description="Sem compromissos hoje." />
              ) : (
                <div className="rows">
                  {data.todayItems.map((item) => {
                    const isBlock = item.type === "Block";
                    const label = isBlock
                      ? "Bloqueio"
                      : item.sessionStatus
                        ? sessionStatusMeta[item.sessionStatus].label
                        : scheduleStatusMeta[item.status].label;
                    return (
                      <div
                        className="list-row"
                        key={item.scheduleId}
                        onClick={() => navigate(isBlock || !item.sessionId ? "/agenda" : `/sessoes?id=${item.sessionId}`)}
                      >
                        <div className="time-chip">
                          <span className="t">{hm(item.startTime)}</span>
                          <span className="d">{hm(item.endTime)}</span>
                        </div>
                        <div className="grow truncate">
                          <div className="pname truncate">{isBlock ? item.blockReason || "Bloqueio" : item.patientName}</div>
                          <div className="psub">{label}</div>
                        </div>
                        {!isBlock && (
                          <span className="badge" style={{ color: scheduleStatusMeta[item.status].color }}>
                            <span className="dot" />
                          </span>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </>
  );
}
