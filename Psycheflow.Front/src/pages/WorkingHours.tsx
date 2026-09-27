import { useMemo, useState } from "react";
import { Clock, Save } from "lucide-react";
import { useStore } from "@/store/AppStore";
import type { WorkingHour } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { WEEKDAYS_LONG } from "@/lib/domain";
import { timeOptions, toMinutes } from "@/lib/time";
import "@/styles/pages.css";

const ORDER = [1, 2, 3, 4, 5, 6, 0]; // Mon..Sun
const TIMES = timeOptions(6, 22, 30);

interface DayState {
  enabled: boolean;
  start: string;
  end: string;
}

export function WorkingHours() {
  const { activePsychologist, setWorkingHours, notify } = useStore();

  const initial = useMemo<Record<number, DayState>>(() => {
    const map: Record<number, DayState> = {};
    for (const d of ORDER) {
      const wh = activePsychologist?.workingHours.find((w) => w.dayOfWeek === d);
      map[d] = wh
        ? { enabled: true, start: wh.start, end: wh.end }
        : { enabled: false, start: "08:00", end: "18:00" };
    }
    return map;
  }, [activePsychologist]);

  const [days, setDays] = useState<Record<number, DayState>>(initial);
  const [dirty, setDirty] = useState(false);

  function update(d: number, patch: Partial<DayState>) {
    setDays((prev) => ({ ...prev, [d]: { ...prev[d], ...patch } }));
    setDirty(true);
  }

  const invalid = ORDER.some((d) => days[d].enabled && toMinutes(days[d].end) <= toMinutes(days[d].start));

  function save() {
    if (!activePsychologist) return;
    const hours: WorkingHour[] = ORDER.filter((d) => days[d].enabled).map((d) => ({
      dayOfWeek: d,
      start: days[d].start,
      end: days[d].end,
    }));
    setWorkingHours(activePsychologist.id, hours);
    setDirty(false);
    notify("success", "Horários de atendimento atualizados.");
  }

  return (
    <>
      <PageHeader
        title="Horários de atendimento"
        subtitle="Defina os dias e faixas em que você atende. A agenda usa isso para validar as sessões."
        actions={
          <button className="btn btn-primary" onClick={save} disabled={!dirty || invalid}>
            <Save /> Salvar
          </button>
        }
      />

      <div className="card card-pad" style={{ maxWidth: 640 }}>
        {ORDER.map((d) => {
          const state = days[d];
          const bad = state.enabled && toMinutes(state.end) <= toMinutes(state.start);
          return (
            <div className="wh-row" key={d}>
              <div className="wh-day">
                <label className="switch">
                  <input
                    type="checkbox"
                    checked={state.enabled}
                    onChange={(e) => update(d, { enabled: e.target.checked })}
                  />
                  <span className="track" />
                </label>
                {WEEKDAYS_LONG[d]}
              </div>
              {state.enabled ? (
                <div className="wh-times">
                  <Clock size={15} className="muted" />
                  <select
                    className={`select ${bad ? "input-error" : ""}`}
                    value={state.start}
                    onChange={(e) => update(d, { start: e.target.value })}
                  >
                    {TIMES.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                  <span className="muted">até</span>
                  <select
                    className={`select ${bad ? "input-error" : ""}`}
                    value={state.end}
                    onChange={(e) => update(d, { end: e.target.value })}
                  >
                    {TIMES.map((t) => (
                      <option key={t} value={t}>
                        {t}
                      </option>
                    ))}
                  </select>
                </div>
              ) : (
                <span className="wh-off">Não atende</span>
              )}
            </div>
          );
        })}
        {invalid && (
          <div className="field-error" style={{ marginTop: 12 }}>
            O horário final deve ser maior que o inicial nos dias marcados.
          </div>
        )}
      </div>
    </>
  );
}
