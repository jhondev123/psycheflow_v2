import { useEffect, useMemo, useState } from "react";
import {
  addDays,
  addMonths,
  addWeeks,
  endOfMonth,
  endOfWeek,
  format,
  isSameMonth,
  isToday,
  startOfMonth,
  startOfWeek,
} from "date-fns";
import { ptBR } from "date-fns/locale";
import { CalendarPlus, ChevronLeft, ChevronRight, Clock, Trash2, User } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { Agenda as AgendaData, AgendaItem, Psychologist, RecurrenceGeneration, RecurrenceType, WorkingHour } from "@/types";
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
import { dayIndex, recurrenceTypeLabel } from "@/lib/domain";
import { durationLabel } from "@/lib/time";
import { capitalize, fmtDateLong, fmtDateShort, hm, isoDate, parseMoney } from "@/lib/format";
import { Modal } from "@/components/ui/Modal";
import { ErrorState } from "@/components/ui/Feedback";
import { PatientSelect } from "@/components/PatientSelect";
import { SessionModal } from "@/components/SessionModal";
import "@/styles/calendar.css";

type View = "month" | "week" | "day";

const HOURS = Array.from({ length: DAY_END_HOUR - DAY_START_HOUR + 1 }, (_, i) => DAY_START_HOUR + i);

function useNow() {
  const [now, setNow] = useState(() => new Date());
  useEffect(() => {
    const id = window.setInterval(() => setNow(new Date()), 60_000);
    return () => clearInterval(id);
  }, []);
  return now;
}

function range(view: View, cursor: Date): { from: Date; to: Date } {
  if (view === "day") return { from: cursor, to: cursor };
  if (view === "week") return { from: startOfWeek(cursor, { weekStartsOn: 0 }), to: endOfWeek(cursor, { weekStartsOn: 0 }) };
  return {
    from: startOfWeek(startOfMonth(cursor), { weekStartsOn: 0 }),
    to: endOfWeek(endOfMonth(cursor), { weekStartsOn: 0 }),
  };
}

interface CreateCtx {
  date: string;
  start?: string;
}

export function Agenda() {
  const { me, isManagement } = useStore();
  const [view, setView] = useState<View>("week");
  const [cursor, setCursor] = useState(() => new Date());
  const [createCtx, setCreateCtx] = useState<CreateCtx | null>(null);
  const [selected, setSelected] = useState<AgendaItem | null>(null);

  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), []);
  const [psychologistId, setPsychologistId] = useState(me?.psychologistId ?? "");
  useEffect(() => {
    if (!psychologistId && psychologists.data?.length) setPsychologistId(psychologists.data[0].id);
  }, [psychologistId, psychologists.data]);

  const { from, to } = range(view, cursor);
  const agenda = useAsync(
    () => api.get<AgendaData>("/agenda", { from: isoDate(from), to: isoDate(to), psychologistId }),
    [isoDate(from), isoDate(to), psychologistId],
    !!psychologistId,
  );

  const items = agenda.data?.items ?? [];
  const workingHours = agenda.data?.workingHours.find((w) => w.psychologistId === psychologistId)?.hours ?? [];
  const psychologistName = psychologists.data?.find((p) => p.id === psychologistId)?.fullName;

  const title = useMemo(() => {
    if (view === "month") return capitalize(format(cursor, "MMMM 'de' yyyy", { locale: ptBR }));
    if (view === "day") return capitalize(format(cursor, "EEEE, d 'de' MMM", { locale: ptBR }));
    const ws = startOfWeek(cursor, { weekStartsOn: 0 });
    const we = addDays(ws, 6);
    return isSameMonth(ws, we)
      ? `${format(ws, "d")}–${format(we, "d 'de' MMM", { locale: ptBR })}`
      : `${format(ws, "d MMM", { locale: ptBR })} – ${format(we, "d MMM", { locale: ptBR })}`;
  }, [view, cursor]);

  function shift(dir: 1 | -1) {
    setCursor((c) => (view === "month" ? addMonths(c, dir) : view === "week" ? addWeeks(c, dir) : addDays(c, dir)));
  }

  if (!me?.psychologistId && !isManagement) return null;

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Agenda</h1>
          <div className="sub">{psychologistName ? `Atendimentos de ${psychologistName}` : "Agenda de atendimentos"}</div>
        </div>
        <div className="row gap-2 wrap">
          {isManagement && (psychologists.data?.length ?? 0) > 1 && (
            <select className="select" aria-label="Psicólogo" value={psychologistId} onChange={(e) => setPsychologistId(e.target.value)}>
              {psychologists.data!.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}
                </option>
              ))}
            </select>
          )}
          <button className="btn btn-primary" disabled={!psychologistId} onClick={() => setCreateCtx({ date: isoDate(view === "month" ? new Date() : cursor) })}>
            <CalendarPlus /> Novo agendamento
          </button>
        </div>
      </div>

      <div className="cal-toolbar">
        <div className="cal-nav">
          <button className="btn btn-ghost btn-icon btn-sm" onClick={() => shift(-1)} aria-label="Anterior">
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
        {agenda.loading && <span className="spinner" aria-label="Carregando" />}
        <div className="grow" />
        <div className="cal-views">
          {(["month", "week", "day"] as View[]).map((v) => (
            <button key={v} className={`cal-view-btn ${view === v ? "active" : ""}`} onClick={() => setView(v)}>
              {v === "month" ? "Mês" : v === "week" ? "Semana" : "Dia"}
            </button>
          ))}
        </div>
      </div>

      {agenda.error && <ErrorState message={agenda.error} onRetry={agenda.reload} />}

      {view === "month" && (
        <MonthView
          cursor={cursor}
          items={items}
          onDay={(d) => {
            setCursor(d);
            setView("day");
          }}
          onEvent={setSelected}
          onCreate={(date) => setCreateCtx({ date })}
        />
      )}
      {view !== "month" && (
        <TimeGrid
          days={view === "week" ? Array.from({ length: 7 }, (_, i) => addDays(from, i)) : [cursor]}
          items={items}
          workingHours={workingHours}
          onSlot={(date, start) => setCreateCtx({ date, start })}
          onEvent={setSelected}
        />
      )}

      <div className="cal-legend">
        <span className="item">
          <span className="swatch" style={{ background: "var(--success)" }} /> Confirmada
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--warning)" }} /> A confirmar
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--info)" }} /> Realizada / falta
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--danger)" }} /> Cancelada
        </span>
        <span className="item">
          <span className="swatch" style={{ background: "var(--text-soft)" }} /> Bloqueio
        </span>
      </div>

      {createCtx && psychologistId && (
        <ScheduleModal ctx={createCtx} psychologistId={psychologistId} onClose={() => setCreateCtx(null)} onCreated={agenda.reload} />
      )}
      {selected?.type === "Session" && selected.sessionId && (
        <SessionModal sessionId={selected.sessionId} onClose={() => setSelected(null)} onChanged={agenda.reload} />
      )}
      {selected?.type === "Block" && <BlockDetails event={toEvent(selected)} onClose={() => setSelected(null)} onDeleted={agenda.reload} />}
    </>
  );
}

/* ---------------- Semana / dia ---------------- */
function TimeGrid({
  days,
  items,
  workingHours,
  onSlot,
  onEvent,
}: {
  days: Date[];
  items: AgendaItem[];
  workingHours: WorkingHour[];
  onSlot: (date: string, start: string) => void;
  onEvent: (item: AgendaItem) => void;
}) {
  const now = useNow();
  const template = `54px repeat(${days.length}, minmax(0, 1fr))`;

  return (
    <div className="cal card">
      <div className="cal-week-head" style={{ gridTemplateColumns: template }}>
        <div className="cal-corner" />
        {days.map((d) => (
          <div key={isoDate(d)} className={`cal-dayhead ${isToday(d) ? "today" : ""}`}>
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
            const dateISO = isoDate(d);
            const weekday = d.getDay();
            const ranges = workingHours.filter((w) => dayIndex(w.dayOfWeek) === weekday);
            const events = packDay(eventsForDate(items, dateISO));
            const nowMin = now.getHours() * 60 + now.getMinutes();
            const nowVisible = isToday(d) && nowMin >= DAY_START_HOUR * 60 && nowMin <= DAY_END_HOUR * 60;

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
                    style={{ top: yForTime(hm(r.startTime)), height: yForTime(hm(r.endTime)) - yForTime(hm(r.startTime)) }}
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

                {nowVisible && <div className="cal-now" style={{ top: yForMinutes(nowMin) }} />}

                {events.map((ev) => (
                  <EventBlock key={ev.item.scheduleId} ev={ev} onClick={() => onEvent(ev.item)} />
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
  const height = Math.max(22, yForMinutes(ev.endMin) - top - 2);
  const widthPct = 100 / ev.lanes;
  return (
    <div
      className={`cal-event ${ev.isBlock ? "block" : ""} ${ev.isCancelled ? "cancelled" : ""}`}
      style={{
        top,
        height,
        left: `calc(${ev.lane * widthPct}% + 2px)`,
        width: `calc(${widthPct}% - 4px)`,
        // @ts-expect-error variáveis CSS
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
        {ev.start}
        {height > 34 ? `–${ev.end}` : ""}
      </div>
      <div className="en">{ev.title}</div>
    </div>
  );
}

/* ---------------- Mês ---------------- */
function MonthView({
  cursor,
  items,
  onDay,
  onEvent,
  onCreate,
}: {
  cursor: Date;
  items: AgendaItem[];
  onDay: (d: Date) => void;
  onEvent: (item: AgendaItem) => void;
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
          const dateISO = isoDate(d);
          const events = eventsForDate(items, dateISO);
          return (
            <div
              key={dateISO}
              className={`cal-cell ${!isSameMonth(d, cursor) ? "out" : ""} ${isToday(d) ? "today" : ""}`}
              onDoubleClick={() => onCreate(dateISO)}
              onClick={() => onDay(d)}
            >
              <span className="cal-cell-num">{format(d, "d")}</span>
              {events.slice(0, 3).map((ev) => (
                <div
                  key={ev.item.scheduleId}
                  className={`cal-chip ${ev.isCancelled ? "cancelled" : ""}`}
                  style={{
                    // @ts-expect-error variáveis CSS
                    "--ev-color": ev.colors.color,
                    "--ev-bg": ev.colors.bg,
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    onEvent(ev.item);
                  }}
                  title={`${ev.start} ${ev.title}`}
                >
                  <span className="ct">{ev.start}</span>
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

/* ---------------- Novo agendamento ---------------- */
function ScheduleModal({
  ctx,
  psychologistId,
  onClose,
  onCreated,
}: {
  ctx: CreateCtx;
  psychologistId: string;
  onClose: () => void;
  onCreated: () => void;
}) {
  const { settings, notify } = useStore();
  const [kind, setKind] = useState<"session" | "block">("session");
  const [date, setDate] = useState(ctx.date);
  const [start, setStart] = useState(ctx.start ?? "09:00");
  const [duration, setDuration] = useState(settings?.sessionDurationMinutes ?? 50);
  const [price, setPrice] = useState(settings?.sessionDefaultPrice?.toFixed(2).replace(".", ",") ?? "");
  const [patientId, setPatientId] = useState("");
  const [confirmNow, setConfirmNow] = useState(true);
  const [repeat, setRepeat] = useState(false);
  const [recurrence, setRecurrence] = useState<RecurrenceType>("Weekly");
  const [until, setUntil] = useState("");
  // bloqueio
  const [endDate, setEndDate] = useState(ctx.date);
  const [wholeDay, setWholeDay] = useState(!ctx.start);
  const [blockEnd, setBlockEnd] = useState(ctx.start ? `${String(Number(ctx.start.slice(0, 2)) + 1).padStart(2, "0")}:00` : "18:00");
  const [reason, setReason] = useState("");

  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [generation, setGeneration] = useState<RecurrenceGeneration | null>(null);

  async function submit() {
    setError(null);
    if (kind === "session" && !patientId) return setError("Selecione o paciente.");
    const parsedPrice = parseMoney(price);
    setBusy(true);
    try {
      if (kind === "block") {
        await api.post("/schedule-blocks", {
          startDate: date,
          endDate: endDate || date,
          startTime: wholeDay ? null : start,
          endTime: wholeDay ? null : blockEnd,
          reason: reason.trim() || null,
          psychologistId,
        });
        notify("success", "Bloqueio criado.");
        onCreated();
        onClose();
      } else if (repeat) {
        const result = await api.post<RecurrenceGeneration>("/recurrences", {
          patientId,
          psychologistId,
          type: recurrence,
          startDate: date,
          startTime: start,
          endDate: until || null,
          durationMinutes: duration,
          price: parsedPrice,
        });
        notify("success", `${result.scheduled.length} sessões agendadas.`);
        onCreated();
        if (result.skipped.length > 0) setGeneration(result);
        else onClose();
      } else {
        const session = await api.post<{ id: string }>("/sessions", {
          patientId,
          psychologistId,
          date,
          startTime: start,
          durationMinutes: duration,
          price: parsedPrice,
        });
        if (confirmNow) await api.post(`/sessions/${session.id}/confirm`);
        notify("success", "Sessão agendada.");
        onCreated();
        onClose();
      }
    } catch (e) {
      setError(errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  if (generation) {
    return (
      <Modal
        title="Recorrência criada"
        onClose={onClose}
        footer={
          <button className="btn btn-primary" onClick={onClose}>
            Entendi
          </button>
        }
      >
        <div className="col gap-3">
          <div className="notice">
            {generation.scheduled.length} sessões agendadas até {fmtDateShort(generation.recurrence.generatedUntil)}. Algumas datas foram puladas:
          </div>
          <ul className="small">
            {generation.skipped.map((s) => (
              <li key={s.date}>
                <b>{fmtDateShort(s.date)}</b> — {s.reason}
              </li>
            ))}
          </ul>
        </div>
      </Modal>
    );
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
          <button className="btn btn-primary" disabled={busy} onClick={submit}>
            {busy ? "Salvando…" : kind === "session" ? "Agendar" : "Bloquear"}
          </button>
        </>
      }
    >
      <div className="col gap-4">
        {error && (
          <div className="auth-error" role="alert">
            <Clock size={16} /> {error}
          </div>
        )}

        <div className="cal-views" style={{ alignSelf: "flex-start" }}>
          <button type="button" className={`cal-view-btn ${kind === "session" ? "active" : ""}`} onClick={() => setKind("session")}>
            Sessão
          </button>
          <button type="button" className={`cal-view-btn ${kind === "block" ? "active" : ""}`} onClick={() => setKind("block")}>
            Bloqueio
          </button>
        </div>

        {kind === "session" ? (
          <>
            <div className="field">
              <label className="label" htmlFor="patient">
                Paciente <span className="req">*</span>
              </label>
              <PatientSelect id="patient" value={patientId} onChange={setPatientId} />
            </div>
            <div className="grid-form">
              <div className="field">
                <label className="label" htmlFor="date">
                  {repeat ? "Primeira sessão" : "Data"}
                </label>
                <input id="date" className="input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
              </div>
              <div className="field">
                <label className="label" htmlFor="start">
                  Início
                </label>
                <input id="start" className="input" type="time" step={300} value={start} onChange={(e) => setStart(e.target.value)} />
              </div>
              <div className="field">
                <label className="label" htmlFor="duration">
                  Duração (min)
                </label>
                <input id="duration" className="input" type="number" min={15} max={240} value={duration} onChange={(e) => setDuration(Number(e.target.value))} />
              </div>
              <div className="field">
                <label className="label" htmlFor="price">
                  Valor (R$)
                </label>
                <input id="price" className="input" inputMode="decimal" placeholder="Padrão da clínica" value={price} onChange={(e) => setPrice(e.target.value)} />
              </div>
            </div>

            <label className="row gap-2 small">
              <input type="checkbox" checked={repeat} onChange={(e) => setRepeat(e.target.checked)} /> Repetir (recorrência)
            </label>
            {repeat ? (
              <div className="grid-form">
                <div className="field">
                  <label className="label" htmlFor="recType">
                    Frequência
                  </label>
                  <select id="recType" className="select" value={recurrence} onChange={(e) => setRecurrence(e.target.value as RecurrenceType)}>
                    {(Object.keys(recurrenceTypeLabel) as RecurrenceType[]).map((t) => (
                      <option key={t} value={t}>
                        {recurrenceTypeLabel[t]}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <label className="label" htmlFor="until">
                    Até (opcional)
                  </label>
                  <input id="until" className="input" type="date" value={until} min={date} onChange={(e) => setUntil(e.target.value)} />
                </div>
                <div className="small muted span-2">As sessões são criadas para os próximos 3 meses; depois é possível estender na ficha do paciente.</div>
              </div>
            ) : (
              <label className="row gap-2 small">
                <input type="checkbox" checked={confirmNow} onChange={(e) => setConfirmNow(e.target.checked)} /> Já confirmar com o paciente
              </label>
            )}
          </>
        ) : (
          <>
            <div className="grid-form">
              <div className="field">
                <label className="label" htmlFor="bStart">
                  De
                </label>
                <input id="bStart" className="input" type="date" value={date} onChange={(e) => setDate(e.target.value)} />
              </div>
              <div className="field">
                <label className="label" htmlFor="bEnd">
                  Até
                </label>
                <input id="bEnd" className="input" type="date" value={endDate} min={date} onChange={(e) => setEndDate(e.target.value)} />
              </div>
            </div>
            <label className="row gap-2 small">
              <input type="checkbox" checked={wholeDay} onChange={(e) => setWholeDay(e.target.checked)} /> Dia inteiro
            </label>
            {!wholeDay && (
              <div className="grid-form">
                <div className="field">
                  <label className="label" htmlFor="bFrom">
                    Início
                  </label>
                  <input id="bFrom" className="input" type="time" step={300} value={start} onChange={(e) => setStart(e.target.value)} />
                </div>
                <div className="field">
                  <label className="label" htmlFor="bTo">
                    Término
                  </label>
                  <input id="bTo" className="input" type="time" step={300} value={blockEnd} onChange={(e) => setBlockEnd(e.target.value)} />
                </div>
              </div>
            )}
            <div className="field">
              <label className="label" htmlFor="bReason">
                Motivo
              </label>
              <input id="bReason" className="input" placeholder="Almoço, férias, congresso…" maxLength={500} value={reason} onChange={(e) => setReason(e.target.value)} />
            </div>
          </>
        )}
      </div>
    </Modal>
  );
}

/* ---------------- Bloqueio ---------------- */
function BlockDetails({ event, onClose, onDeleted }: { event: CalEvent; onClose: () => void; onDeleted: () => void }) {
  const { notify } = useStore();
  const [busy, setBusy] = useState(false);

  async function remove() {
    setBusy(true);
    try {
      await api.del(`/schedule-blocks/${event.item.scheduleId}`);
      notify("info", "Bloqueio removido.");
      onDeleted();
      onClose();
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title="Bloqueio"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-danger" disabled={busy} onClick={remove}>
            <Trash2 /> Remover bloqueio
          </button>
          <div className="grow" />
          <button className="btn btn-ghost" onClick={onClose}>
            Fechar
          </button>
        </>
      }
    >
      <div className="ev-meta">
        <div className="ev-line">
          <User />
          <span className="strong">{event.title}</span>
        </div>
        <div className="ev-line">
          <Clock />
          <span>
            {capitalize(fmtDateLong(event.item.date))} · {event.start}–{event.end} <span className="muted">({durationLabel(event.start, event.end)})</span>
          </span>
        </div>
      </div>
    </Modal>
  );
}
