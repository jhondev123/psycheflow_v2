import { useMemo } from "react";
import { Link, useNavigate } from "react-router-dom";
import { format } from "date-fns";
import { CalendarCheck, CalendarClock, CalendarPlus, Clock3, Users } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { ScheduleStatus, ScheduleType, type Schedule } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { scheduleStatusMeta } from "@/lib/domain";
import { durationLabel } from "@/lib/time";
import { fmtRelativeDay } from "@/lib/format";
import "@/styles/pages.css";

export function Dashboard() {
  const { db, activePsychologist } = useStore();
  const navigate = useNavigate();
  const psyId = activePsychologist?.id ?? "";
  const today = format(new Date(), "yyyy-MM-dd");
  const weekStartISO = format(startOfThisWeek(), "yyyy-MM-dd");
  const weekEndISO = format(endOfThisWeek(), "yyyy-MM-dd");

  const sessions = useMemo(
    () => db.schedules.filter((s) => s.psychologistId === psyId && s.type === ScheduleType.SESSION),
    [db.schedules, psyId],
  );

  const todayConfirmed = sessions.filter(
    (s) => s.date === today && s.status === ScheduleStatus.Confirmed,
  ).length;
  const pending = sessions.filter(
    (s) => s.date >= today && s.status === ScheduleStatus.Pending,
  ).length;
  const weekCount = sessions.filter(
    (s) => s.date >= weekStartISO && s.date <= weekEndISO && s.status !== ScheduleStatus.Cancelled,
  ).length;

  const upcoming = useMemo(
    () =>
      sessions
        .filter((s) => s.date >= today && s.status !== ScheduleStatus.Cancelled)
        .sort((a, b) => a.date.localeCompare(b.date) || a.start.localeCompare(b.start))
        .slice(0, 6),
    [sessions, today],
  );

  const todays = useMemo(
    () =>
      db.schedules
        .filter((s) => s.psychologistId === psyId && s.date === today)
        .sort((a, b) => a.start.localeCompare(b.start)),
    [db.schedules, psyId, today],
  );

  function patientName(s: Schedule): string {
    const session = db.sessions.find((ss) => ss.id === s.sessionId);
    return db.patients.find((p) => p.id === session?.patientId)?.name ?? "Sessão";
  }

  const stats = [
    { label: "Pacientes", value: db.patients.length, icon: <Users />, color: "var(--brand-500)", bg: "var(--brand-50)" },
    { label: "Sessões nesta semana", value: weekCount, icon: <CalendarClock />, color: "var(--info)", bg: "var(--info-bg)" },
    { label: "Confirmadas hoje", value: todayConfirmed, icon: <CalendarCheck />, color: "var(--success)", bg: "var(--success-bg)" },
    { label: "Pendentes", value: pending, icon: <Clock3 />, color: "var(--warning)", bg: "var(--warning-bg)" },
  ];

  return (
    <>
      <PageHeader
        title="Painel"
        subtitle="Visão geral da sua clínica."
        actions={
          <button className="btn btn-primary" onClick={() => navigate("/agenda")}>
            <CalendarPlus /> Ir para a agenda
          </button>
        }
      />

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
          {upcoming.length === 0 ? (
            <EmptyState
              icon={<CalendarClock />}
              title="Nenhum atendimento agendado"
              description="Que tal agendar a próxima sessão?"
            />
          ) : (
            <div className="rows">
              {upcoming.map((s) => {
                const meta = scheduleStatusMeta[s.status];
                return (
                  <div className="list-row up-next" key={s.id} onClick={() => navigate("/agenda")}>
                    <span className="bar" style={{ background: meta.color }} />
                    <div className="time-chip">
                      <span className="t">{s.start}</span>
                      <span className="d">{durationLabel(s.start, s.end)}</span>
                    </div>
                    <Avatar name={patientName(s)} size="sm" />
                    <div className="grow truncate">
                      <div className="pname truncate">{patientName(s)}</div>
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
            <span className="count-pill">{todays.length}</span>
          </div>
          {todays.length === 0 ? (
            <EmptyState icon={<CalendarCheck />} title="Dia livre" description="Sem compromissos hoje." />
          ) : (
            <div className="rows">
              {todays.map((s) => {
                const isBlock = s.type === ScheduleType.BLOCK;
                const meta = scheduleStatusMeta[s.status];
                return (
                  <div className="list-row" key={s.id} onClick={() => navigate("/agenda")}>
                    <div className="time-chip">
                      <span className="t">{s.start}</span>
                      <span className="d">{s.end}</span>
                    </div>
                    <div className="grow truncate">
                      <div className="pname truncate">{isBlock ? s.title ?? "Bloqueio" : patientName(s)}</div>
                      <div className="psub">{isBlock ? "Bloqueio" : meta.label}</div>
                    </div>
                    {!isBlock && <span className="badge" style={{ color: meta.color }}><span className="dot" /></span>}
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </>
  );
}

function startOfThisWeek(): Date {
  const d = new Date();
  d.setDate(d.getDate() - d.getDay());
  d.setHours(0, 0, 0, 0);
  return d;
}
function endOfThisWeek(): Date {
  const d = startOfThisWeek();
  d.setDate(d.getDate() + 6);
  return d;
}
