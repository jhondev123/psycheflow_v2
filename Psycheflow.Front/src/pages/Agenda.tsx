import { useEffect, useMemo, useState } from "react";
import {
  addDays,
  addMonths,
  addWeeks,
  format,
  isSameMonth,
  isToday,
  startOfMonth,
  startOfWeek,
} from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  CalendarPlus,
  ChevronLeft,
  ChevronRight,
  Clock,
  Trash2,
  User,
} from "lucide-react";
import { useStore } from "@/store/AppStore";
import {
  type Schedule,
  type WorkingHour,
  ScheduleStatus,
  ScheduleType,
} from "@/types";
import {
  type CalEvent,
  DAY_END_HOUR,
  DAY_START_HOUR,
  GRID_HEIGHT,
  HOUR_PX,
  eventsForDate,
  packDay,
  toEvent,
  yForMinutes,
  yForTime,
} from "@/lib/calendar";
import { scheduleStatusMeta, scheduleTypeLabel } from "@/lib/domain";
import { durationLabel, timeOptions } from "@/lib/time";
import { capitalize, fmtDateLong } from "@/lib/format";
import { Modal } from "@/components/ui/Modal";
import "@/styles/calendar.css";

type View = "month" | "week" | "day";

const iso = (d: Date) => format(d, "yyyy-MM-dd");
const HOURS = Array.from({ length: DAY_END_HOUR - DAY_START_HOUR + 1 }, (_, i) => DAY_START_HOUR + i);

/** Ticking "now" updated every minute (for the time indicator). */
function useNow() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(id);
  }, []);
  return now;
}

interface CreateCtx {
  date: string;
  start?: string;
}

export function Agenda() {
  const { db, activePsychologist } = useStore();
  const [view, setView] = useState<View>("week");
  const [cursor, setCursor] = useState(() => new Date());
  const [createCtx, setCreateCtx] = useState<CreateCtx | null>(null);
  const [selected, setSelected] = useState<Schedule | null>(null);

  const psyId = activePsychologist?.id ?? "";

  const title = useMemo(() => {
    if (view === "month") return capitalize(format(cursor, "MMMM 'de' yyyy", { locale: ptBR }));
    if (view === "day") return capitalize(format(cursor, "EEEE, d 'de' MMM", { locale: ptBR }));
    const ws = startOfWeek(cursor, { weekStartsOn: 0 });
    const we = addDays(ws, 6);
    const sameMonth = isSameMonth(ws, we);
    return sameMonth
      ? capitalize(format(ws, "d", { locale: ptBR })) +
          "–" +
          format(we, "d 'de' MMM", { locale: ptBR })
      : `${format(ws, "d MMM", { locale: ptBR })} – ${format(we, "d MMM", { locale: ptBR })}`;
  }, [view, cursor]);

  function shift(dir: 1 | -1) {
    setCursor((c) =>
      view === "month" ? addMonths(c, dir) : view === "week" ? addWeeks(c, dir) : addDays(c, dir),
    );
  }

  const selectedEvent = selected ? toEvent(db, selected) : null;

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Agenda</h1>
          <div className="sub">
            {activePsychologist
              ? `Atendimentos de ${activePsychologist.name}`
              : "Sua agenda de atendimentos"}
          </div>
        </div>
        <button
          className="btn btn-primary"
          onClick={() => setCreateCtx({ date: iso(view === "month" ? new Date() : cursor) })}
        >
          <CalendarPlus /> Novo agendamento
        </button>
      </div>

      <div className="cal-toolbar">
        <div className="cal-nav">
          <button
            className="btn btn-ghost btn-icon btn-sm"
            onClick={() => shift(-1)}
            aria-label="Anterior"
          >
            <ChevronLeft />
          </button>
          <button className="btn btn-ghost btn-icon btn-sm" onClick={() => shift(1)} aria-label="Próximo">
            <ChevronRight />
          </button>
        </div>
        <button className="btn btn-subtle btn-sm" onClick={() => setCursor(new Date())}>
          Hoje
        </button>
        <div className="cal-title">{title}</div>
        <div className="grow" />
        <div className="cal-views">
          {(["month", "week", "day"] as View[]).map((v) => (
            <button
              key={v}
              className={`cal-view-btn ${view === v ? "active" : ""}`}
              onClick={() => setView(v)}
            >
              {v === "month" ? "Mês" : v === "week" ? "Semana" : "Dia"}
            </button>
          ))}
        </div>
      </div>

      {view === "month" && (
        <MonthView
          cursor={cursor}
          db={db}
          psyId={psyId}
          onDay={(d) => {
            setCursor(d);
            setView("day");
          }}
          onEvent={setSelected}
          onCreate={(date) => setCreateCtx({ date })}
        />
      )}
      {view === "week" && (
        <TimeGrid
          days={weekDays(cursor)}
          db={db}
          psyId={psyId}
          workingHours={activePsychologist?.workingHours ?? []}
          onSlot={(date, start) => setCreateCtx({ date, start })}
          onEvent={setSelected}
        />
      )}
      {view === "day" && (
        <TimeGrid
          days={[cursor]}
          db={db}
          psyId={psyId}
          workingHours={activePsychologist?.workingHours ?? []}
          onSlot={(date, start) => setCreateCtx({ date, start })}
          onEvent={setSelected}
        />
      )}

      <div className="cal-legend">
        <span className="item">
          <span className="swatch" style={{ background: "var(--success)" }} /> Confirmado
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--warning)" }} /> Pendente
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--danger)" }} /> Cancelado
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--text-soft)" }} /> Bloqueio
        </span>
      </div>

      {createCtx && (
        <ScheduleModal ctx={createCtx} onClose={() => setCreateCtx(null)} />
      )}
      {selectedEvent && (
        <EventDetails event={selectedEvent} onClose={() => setSelected(null)} />
      )}
    </>
  );
}

function weekDays(cursor: Date): Date[] {
  const ws = startOfWeek(cursor, { weekStartsOn: 0 });
  return Array.from({ length: 7 }, (_, i) => addDays(ws, i));
}

/* ---------------- Week / Day time grid ---------------- */
function TimeGrid({
  days,
  db,
  psyId,
  workingHours,
  onSlot,
  onEvent,
}: {
  days: Date[];
  db: ReturnType<typeof useStore>["db"];
  psyId: string;
  workingHours: WorkingHour[];
  onSlot: (date: string, start: string) => void;
  onEvent: (s: Schedule) => void;
}) {
  const now = useNow();
  const cols = days.length;
  const template = `54px repeat(${cols}, minmax(0, 1fr))`;

  return (
    <div className="cal card">
      <div className="cal-week-head" style={{ gridTemplateColumns: template }}>
        <div className="cal-corner" />
        {days.map((d) => (
          <div key={iso(d)} className={`cal-dayhead ${isToday(d) ? "today" : ""}`}>
            <div className="dow">{format(d, "EEE", { locale: ptBR })}</div>
            <div className="dnum">{format(d, "d")}</div>
          </div>
        ))}
      </div>

      <div className="cal-scroll">
        <div className="cal-canvas" style={{ gridTemplateColumns: template, height: GRID_HEIGHT }}>
          <div className="cal-gutter">
            {HOURS.map((h) => (
              <span key={h} className="cal-hour-label" style={{ top: yForTime(`${h}:00`) }}>
                {String(h).padStart(2, "0")}:00
              </span>
            ))}
          </div>

          {days.map((d) => {
            const dateISO = iso(d);
            const weekday = d.getDay();
            const ranges = workingHours.filter((w) => w.dayOfWeek === weekday);
            const events = packDay(eventsForDate(db, psyId, dateISO));
            const showNow = isToday(d);
            const nowMin = now.getHours() * 60 + now.getMinutes();
            const nowVisible = nowMin >= DAY_START_HOUR * 60 && nowMin <= DAY_END_HOUR * 60;

            return (
              <div
                key={dateISO}
                className={`cal-daycol ${weekday === 0 || weekday === 6 ? "is-weekend" : ""}`}
                style={{
                  backgroundImage: `repeating-linear-gradient(to bottom, var(--border) 0, var(--border) 1px, transparent 1px, transparent ${HOUR_PX}px)`,
                }}
              >
                {ranges.map((r, i) => (
                  <div
                    key={i}
                    className="cal-work"
                    style={{ top: yForTime(r.start), height: yForTime(r.end) - yForTime(r.start) }}
                  />
                ))}

                {HOURS.slice(0, -1).map((h) => (
                  <div
                    key={h}
                    className="cal-slot"
                    style={{ top: yForTime(`${h}:00`), height: HOUR_PX }}
                    onClick={() => onSlot(dateISO, `${String(h).padStart(2, "0")}:00`)}
                  />
                ))}

                {showNow && nowVisible && (
                  <div className="cal-now" style={{ top: yForMinutes(nowMin) }} />
                )}

                {events.map((ev) => (
                  <EventBlock key={ev.schedule.id} ev={ev} onClick={() => onEvent(ev.schedule)} />
                ))}
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
}

function EventBlock({ ev, onClick }: { ev: CalEvent; onClick: () => void }) {
  const top = yForMinutes(ev.startMin);
  const rawH = yForMinutes(ev.endMin) - top;
  const height = Math.max(22, rawH - 2);
  const widthPct = 100 / ev.lanes;
  return (
    <div
      className={`cal-event ${ev.isBlock ? "block" : ""} ${ev.isCancelled ? "cancelled" : ""}`}
      style={{
        top,
        height,
        left: `calc(${ev.lane * widthPct}% + 2px)`,
        width: `calc(${widthPct}% - 4px)`,
        // @ts-expect-error custom props
        "--ev-color": ev.colors.color,
        "--ev-bg": ev.colors.bg,
      }}
      onClick={(e) => {
        e.stopPropagation();
        onClick();
      }}
      title={ev.title}
    >
      <div className="et">
        {ev.schedule.start}
        {height > 34 ? `–${ev.schedule.end}` : ""}
      </div>
      <div className="en">{ev.title}</div>
    </div>
  );
}

/* ---------------- Month view ---------------- */
function MonthView({
  cursor,
  db,
  psyId,
  onDay,
  onEvent,
  onCreate,
}: {
  cursor: Date;
  db: ReturnType<typeof useStore>["db"];
  psyId: string;
  onDay: (d: Date) => void;
  onEvent: (s: Schedule) => void;
  onCreate: (date: string) => void;
}) {
  const gridStart = startOfWeek(startOfMonth(cursor), { weekStartsOn: 0 });
  const days = Array.from({ length: 42 }, (_, i) => addDays(gridStart, i));

  return (
    <div className="cal card">
      <div className="cal-month-head">
        {["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"].map((d) => (
          <div key={d} className="cal-month-dow">
            {d}
          </div>
        ))}
      </div>
      <div className="cal-month-grid">
        {days.map((d) => {
          const dateISO = iso(d);
          const events = eventsForDate(db, psyId, dateISO);
          const out = !isSameMonth(d, cursor);
          return (
            <div
              key={dateISO}
              className={`cal-cell ${out ? "out" : ""} ${isToday(d) ? "today" : ""}`}
              onDoubleClick={() => onCreate(dateISO)}
              onClick={() => onDay(d)}
            >
              <span className="cal-cell-num">{format(d, "d")}</span>
              {events.slice(0, 3).map((ev) => (
                <div
                  key={ev.schedule.id}
                  className={`cal-chip ${ev.isCancelled ? "cancelled" : ""}`}
                  style={{
                    // @ts-expect-error custom props
                    "--ev-color": ev.colors.color,
                    "--ev-bg": ev.isBlock ? "var(--surface-2)" : ev.colors.bg,
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    onEvent(ev.schedule);
                  }}
                  title={`${ev.schedule.start} ${ev.title}`}
                >
                  <span className="ct">{ev.schedule.start}</span>
                  <span className="truncate">{ev.title}</span>
                </div>
              ))}
              {events.length > 3 && <div className="cal-more">+{events.length - 3} mais</div>}
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ---------------- Create modal ---------------- */
const START_OPTIONS = timeOptions(DAY_START_HOUR, DAY_END_HOUR - 1, 30);
const END_OPTIONS = timeOptions(DAY_START_HOUR + 1, DAY_END_HOUR, 30);

function addHour(hhmm: string): string {
  const [h, m] = hhmm.split(":").map(Number);
  return `${String(Math.min(DAY_END_HOUR, h + 1)).padStart(2, "0")}:${String(m).padStart(2, "0")}`;
}

function ScheduleModal({ ctx, onClose }: { ctx: CreateCtx; onClose: () => void }) {
  const { db, activePsychologist, createSchedule, notify } = useStore();
  const [type, setType] = useState<ScheduleType>(ScheduleType.SESSION);
  const [date, setDate] = useState(ctx.date);
  const [start, setStart] = useState(ctx.start ?? "09:00");
  const [end, setEnd] = useState(addHour(ctx.start ?? "09:00"));
  const [patientId, setPatientId] = useState("");
  const [status, setStatus] = useState<ScheduleStatus>(ScheduleStatus.Confirmed);
  const [title, setTitle] = useState("");
  const [error, setError] = useState<string | null>(null);

  const patients = useMemo(
    () => [...db.patients].sort((a, b) => a.name.localeCompare(b.name, "pt-BR")),
    [db.patients],
  );

  function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!activePsychologist) return setError("Nenhum psicólogo ativo.");
    const res = createSchedule({
      date,
      start,
      end,
      psychologistId: activePsychologist.id,
      type,
      patientId: type === ScheduleType.SESSION ? patientId : null,
      status,
      title: type === ScheduleType.BLOCK ? title.trim() || "Bloqueio" : undefined,
    });
    if (!res.ok) return setError(res.error ?? "Não foi possível agendar.");
    notify("success", type === ScheduleType.SESSION ? "Sessão agendada." : "Bloqueio criado.");
    onClose();
  }

  return (
    <Modal
      title="Novo agendamento"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" type="submit" form="sched-form">
            Agendar
          </button>
        </>
      }
    >
      <form id="sched-form" onSubmit={submit} className="col gap-4" noValidate>
        {error && (
          <div className="auth-error">
            <Clock size={16} /> {error}
          </div>
        )}

        <div className="cal-views" style={{ alignSelf: "flex-start" }}>
          <button
            type="button"
            className={`cal-view-btn ${type === ScheduleType.SESSION ? "active" : ""}`}
            onClick={() => setType(ScheduleType.SESSION)}
          >
            Sessão
          </button>
          <button
            type="button"
            className={`cal-view-btn ${type === ScheduleType.BLOCK ? "active" : ""}`}
            onClick={() => setType(ScheduleType.BLOCK)}
          >
            Bloqueio
          </button>
        </div>

        {type === ScheduleType.SESSION ? (
          <div className="field">
            <label className="label">
              Paciente <span className="req">*</span>
            </label>
            <select
              className="select"
              value={patientId}
              onChange={(e) => {
                setPatientId(e.target.value);
                setError(null);
              }}
            >
              <option value="">Selecione um paciente…</option>
              {patients.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </div>
        ) : (
          <div className="field">
            <label className="label">Descrição do bloqueio</label>
            <input
              className="input"
              placeholder="Almoço, Reunião, Indisponível…"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
            />
          </div>
        )}

        <div className="field">
          <label className="label">Data</label>
          <input
            className="input"
            type="date"
            value={date}
            onChange={(e) => {
              setDate(e.target.value);
              setError(null);
            }}
          />
        </div>

        <div className="grid-form">
          <div className="field">
            <label className="label">Início</label>
            <select
              className="select"
              value={start}
              onChange={(e) => {
                setStart(e.target.value);
                if (e.target.value >= end) setEnd(addHour(e.target.value));
                setError(null);
              }}
            >
              {START_OPTIONS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label className="label">Término</label>
            <select
              className="select"
              value={end}
              onChange={(e) => {
                setEnd(e.target.value);
                setError(null);
              }}
            >
              {END_OPTIONS.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </div>
        </div>

        {type === ScheduleType.SESSION && (
          <div className="field">
            <label className="label">Situação</label>
            <select
              className="select"
              value={status}
              onChange={(e) => setStatus(Number(e.target.value) as ScheduleStatus)}
            >
              <option value={ScheduleStatus.Confirmed}>Confirmado</option>
              <option value={ScheduleStatus.Pending}>Pendente</option>
            </select>
          </div>
        )}
      </form>
    </Modal>
  );
}

/* ---------------- Details modal ---------------- */
function EventDetails({ event, onClose }: { event: CalEvent; onClose: () => void }) {
  const { setScheduleStatus, deleteSchedule, notify } = useStore();
  const s = event.schedule;
  const meta = scheduleStatusMeta[s.status];

  return (
    <Modal
      title={event.isBlock ? "Bloqueio" : "Detalhes da sessão"}
      onClose={onClose}
      footer={
        <>
          <button
            className="btn btn-danger"
            onClick={() => {
              deleteSchedule(s.id);
              notify("info", "Agendamento removido.");
              onClose();
            }}
          >
            <Trash2 /> Excluir
          </button>
          <div className="grow" />
          {s.status !== ScheduleStatus.Cancelled ? (
            <>
              {s.status !== ScheduleStatus.Confirmed && (
                <button
                  className="btn btn-subtle"
                  onClick={() => {
                    setScheduleStatus(s.id, ScheduleStatus.Confirmed);
                    notify("success", "Agendamento confirmado.");
                    onClose();
                  }}
                >
                  Confirmar
                </button>
              )}
              <button
                className="btn btn-ghost"
                onClick={() => {
                  setScheduleStatus(s.id, ScheduleStatus.Cancelled);
                  notify("info", "Agendamento cancelado.");
                  onClose();
                }}
              >
                Cancelar sessão
              </button>
            </>
          ) : (
            <button
              className="btn btn-subtle"
              onClick={() => {
                setScheduleStatus(s.id, ScheduleStatus.Pending);
                notify("info", "Agendamento reativado.");
                onClose();
              }}
            >
              Reativar
            </button>
          )}
        </>
      }
    >
      <div className="ev-meta">
        {!event.isBlock && (
          <div className="ev-line">
            <User />
            <span className="strong">{event.patientName ?? "Sessão"}</span>
          </div>
        )}
        {event.isBlock && (
          <div className="ev-line">
            <User />
            <span className="strong">{event.title}</span>
          </div>
        )}
        <div className="ev-line">
          <Clock />
          <span>
            {capitalize(fmtDateLong(s.date))} · {s.start}–{s.end}{" "}
            <span className="muted">({durationLabel(s.start, s.end)})</span>
          </span>
        </div>
        <div className="ev-line">
          <span style={{ width: 18 }} />
          <span className="row gap-2">
            <span className={`badge ${meta.badge}`}>
              <span className="dot" /> {meta.label}
            </span>
            <span className="badge">{scheduleTypeLabel(s.type)}</span>
          </span>
        </div>
      </div>
    </Modal>
  );
}
