import { useEffect, useState } from "react";
import { Plus, Save, Trash2 } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { DayOfWeek, Psychologist, WorkingHour } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { DAYS, WEEKDAYS_LONG } from "@/lib/domain";
import { hm } from "@/lib/format";
import { toMinutes } from "@/lib/time";
import "@/styles/pages.css";

const ORDER = [1, 2, 3, 4, 5, 6, 0]; // segunda a domingo

interface Range {
  start: string;
  end: string;
}

type Week = Record<DayOfWeek, Range[]>;

function toWeek(hours: WorkingHour[]): Week {
  const week = Object.fromEntries(DAYS.map((d) => [d, [] as Range[]])) as Week;
  for (const h of hours) week[h.dayOfWeek].push({ start: hm(h.startTime), end: hm(h.endTime) });
  return week;
}

function dayProblem(ranges: Range[]): string | null {
  if (ranges.some((r) => toMinutes(r.end) <= toMinutes(r.start))) return "O fim de cada faixa deve ser depois do início.";
  const sorted = [...ranges].sort((a, b) => toMinutes(a.start) - toMinutes(b.start));
  for (let i = 1; i < sorted.length; i++) {
    if (toMinutes(sorted[i].start) < toMinutes(sorted[i - 1].end)) return "As faixas do mesmo dia não podem se sobrepor.";
  }
  return null;
}

/** RC-03: expediente do psicólogo, usado pela agenda para validar as sessões (RN-33). */
export function WorkingHours() {
  const { me, isManagement, notify, refreshProfile } = useStore();
  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), []);
  const [psychologistId, setPsychologistId] = useState(me?.psychologistId ?? "");
  const [week, setWeek] = useState<Week | null>(null);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!psychologistId && psychologists.data?.length) setPsychologistId(psychologists.data[0].id);
  }, [psychologistId, psychologists.data]);

  const hours = useAsync(() => api.get<WorkingHour[]>(`/psychologists/${psychologistId}/working-hours`), [psychologistId], !!psychologistId);
  useEffect(() => {
    if (hours.data) {
      setWeek(toWeek(hours.data));
      setDirty(false);
    }
  }, [hours.data]);

  const canEdit = isManagement || psychologistId === me?.psychologistId;
  const problems = week ? DAYS.map((d) => dayProblem(week[d])) : [];
  const invalid = problems.some(Boolean);

  function update(day: DayOfWeek, ranges: Range[]) {
    setWeek((w) => (w ? { ...w, [day]: ranges } : w));
    setDirty(true);
  }

  async function save() {
    if (!week) return;
    setBusy(true);
    try {
      const payload = DAYS.flatMap((d) => week[d].map((r) => ({ dayOfWeek: d, startTime: r.start, endTime: r.end })));
      await api.put(`/psychologists/${psychologistId}/working-hours`, { hours: payload });
      notify("success", "Horários de atendimento atualizados.");
      setDirty(false);
      if (psychologistId === me?.psychologistId) await refreshProfile();
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader
        title="Horários de atendimento"
        subtitle="Dias e faixas de atendimento. Sessões fora do expediente não podem ser agendadas."
        actions={
          <>
            {isManagement && (psychologists.data?.length ?? 0) > 1 && (
              <select className="select" aria-label="Psicólogo" value={psychologistId} onChange={(e) => setPsychologistId(e.target.value)}>
                {psychologists.data!.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.fullName}
                  </option>
                ))}
              </select>
            )}
            {canEdit && (
              <button className="btn btn-primary" onClick={save} disabled={!dirty || invalid || busy}>
                <Save /> {busy ? "Salvando…" : "Salvar"}
              </button>
            )}
          </>
        }
      />

      {hours.error && <ErrorState message={hours.error} onRetry={hours.reload} />}
      {!week ? (
        !hours.error && <Loading />
      ) : (
        <div className="card card-pad" style={{ maxWidth: 720 }}>
          {ORDER.map((index) => {
            const day = DAYS[index];
            const ranges = week[day];
            const problem = problems[index];
            return (
              <div className="wh-row" key={day} style={{ alignItems: "flex-start" }}>
                <div className="wh-day">
                  <label className="switch">
                    <input
                      type="checkbox"
                      disabled={!canEdit}
                      checked={ranges.length > 0}
                      onChange={(e) => update(day, e.target.checked ? [{ start: "08:00", end: "12:00" }, { start: "13:00", end: "18:00" }] : [])}
                    />
                    <span className="track" />
                  </label>
                  {WEEKDAYS_LONG[index]}
                </div>
                {ranges.length === 0 ? (
                  <span className="wh-off">Não atende</span>
                ) : (
                  <div className="col gap-2">
                    {ranges.map((r, i) => (
                      <div className="wh-times" key={i}>
                        <input
                          className={`input ${problem ? "input-error" : ""}`}
                          type="time"
                          step={900}
                          disabled={!canEdit}
                          aria-label="Início"
                          value={r.start}
                          onChange={(e) => update(day, ranges.map((x, j) => (j === i ? { ...x, start: e.target.value } : x)))}
                        />
                        <span className="muted">até</span>
                        <input
                          className={`input ${problem ? "input-error" : ""}`}
                          type="time"
                          step={900}
                          disabled={!canEdit}
                          aria-label="Fim"
                          value={r.end}
                          onChange={(e) => update(day, ranges.map((x, j) => (j === i ? { ...x, end: e.target.value } : x)))}
                        />
                        {canEdit && (
                          <button className="btn btn-ghost btn-icon btn-sm" aria-label="Remover faixa" onClick={() => update(day, ranges.filter((_, j) => j !== i))}>
                            <Trash2 />
                          </button>
                        )}
                      </div>
                    ))}
                    {canEdit && (
                      <button className="btn btn-ghost btn-sm" style={{ alignSelf: "flex-start" }} onClick={() => update(day, [...ranges, { start: "18:00", end: "20:00" }])}>
                        <Plus size={14} /> Faixa
                      </button>
                    )}
                    {problem && <span className="field-error">{problem}</span>}
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}
    </>
  );
}
