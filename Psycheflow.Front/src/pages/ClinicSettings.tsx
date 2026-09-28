import { useEffect, useState } from "react";
import { Save } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, ApiError, errorMessage } from "@/lib/api";
import { PageHeader } from "@/components/layout/PageHeader";
import { Loading } from "@/components/ui/Feedback";
import { parseMoney } from "@/lib/format";
import "@/styles/pages.css";

const TIME_ZONES = [
  "America/Sao_Paulo",
  "America/Bahia",
  "America/Fortaleza",
  "America/Recife",
  "America/Belem",
  "America/Manaus",
  "America/Cuiaba",
  "America/Campo_Grande",
  "America/Porto_Velho",
  "America/Boa_Vista",
  "America/Rio_Branco",
  "America/Noronha",
];

/** Configurações da clínica: duração e valor padrão da sessão (RN-32, RN-51) e fuso horário. */
export function ClinicSettings() {
  const { me, settings, isManagement, refreshSettings, notify } = useStore();
  const [duration, setDuration] = useState("");
  const [price, setPrice] = useState("");
  const [timeZone, setTimeZone] = useState("America/Sao_Paulo");
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (settings) {
      setDuration(String(settings.sessionDurationMinutes));
      setPrice(settings.sessionDefaultPrice === null ? "" : settings.sessionDefaultPrice.toFixed(2).replace(".", ","));
      setTimeZone(settings.timeZone);
    }
  }, [settings]);

  if (!settings) return <Loading />;

  const minutes = Number(duration);
  const parsedPrice = parseMoney(price);
  const invalid = !Number.isInteger(minutes) || minutes < 15 || minutes > 240 || (price.trim() !== "" && (parsedPrice === null || parsedPrice < 0));

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErrors({});
    try {
      await api.put("/settings", { sessionDurationMinutes: minutes, sessionDefaultPrice: parsedPrice, timeZone });
      await refreshSettings();
      notify("success", "Configurações salvas.");
    } catch (err) {
      if (err instanceof ApiError) setErrors(err.fields);
      notify("error", errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title="Clínica" subtitle={me?.companyName} />
      <form className="card card-pad stack" style={{ maxWidth: 560 }} onSubmit={save} noValidate>
        {!isManagement && <div className="notice">Somente administradores e gestores podem alterar estas configurações.</div>}
        <div className="grid-form">
          <div className="field">
            <label className="label" htmlFor="csDuration">
              Duração padrão da sessão (min)
            </label>
            <input
              id="csDuration"
              className={`input ${errors.sessionDurationMinutes ? "input-error" : ""}`}
              type="number"
              min={15}
              max={240}
              disabled={!isManagement}
              value={duration}
              onChange={(e) => setDuration(e.target.value)}
            />
            <span className="tiny muted">Entre 15 e 240 minutos.</span>
          </div>
          <div className="field">
            <label className="label" htmlFor="csPrice">
              Valor padrão da sessão (R$)
            </label>
            <input
              id="csPrice"
              className={`input ${errors.sessionDefaultPrice ? "input-error" : ""}`}
              inputMode="decimal"
              placeholder="Opcional"
              disabled={!isManagement}
              value={price}
              onChange={(e) => setPrice(e.target.value)}
            />
            <span className="tiny muted">Usado quando a sessão é agendada sem valor.</span>
          </div>
          <div className="field span-2">
            <label className="label" htmlFor="csTz">
              Fuso horário da agenda
            </label>
            <select id="csTz" className="select" disabled={!isManagement} value={timeZone} onChange={(e) => setTimeZone(e.target.value)}>
              {[...new Set([timeZone, ...TIME_ZONES])].map((tz) => (
                <option key={tz} value={tz}>
                  {tz.replace("America/", "").replace(/_/g, " ")}
                </option>
              ))}
            </select>
          </div>
        </div>
        {isManagement && (
          <div>
            <button className="btn btn-primary" type="submit" disabled={invalid || busy}>
              <Save /> {busy ? "Salvando…" : "Salvar"}
            </button>
          </div>
        )}
      </form>
    </>
  );
}
