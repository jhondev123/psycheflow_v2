import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { BadgeCheck, Brain, Clock, KeyRound, Mail, Save } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, ApiError, errorMessage } from "@/lib/api";
import type { ApproachType } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { Loading } from "@/components/ui/Feedback";
import { APPROACHES, approachLabel, dayIndex, mainRole, WEEKDAYS_SHORT } from "@/lib/domain";
import { fmtPhone } from "@/lib/format";
import { isValidLicense, isValidPhone, maskLicense, maskPhone } from "@/lib/validation";
import "@/styles/pages.css";

interface Form {
  fullName: string;
  licenseNumber: string;
  approach: ApproachType;
  phone: string;
}

/** RC-04: dados profissionais do psicólogo (aparecem nos documentos). */
export function Profile() {
  const { me, psychologist, refreshProfile, notify } = useStore();
  const [form, setForm] = useState<Form | null>(null);
  const [dirty, setDirty] = useState(false);
  const [busy, setBusy] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (psychologist && !dirty) {
      setForm({
        fullName: psychologist.fullName,
        licenseNumber: psychologist.licenseNumber,
        approach: psychologist.approach,
        phone: psychologist.phone ? fmtPhone(psychologist.phone) : "",
      });
    }
  }, [psychologist, dirty]);

  if (!psychologist || !form) return <Loading />;

  const errors = {
    fullName: form.fullName.trim().length < 3 ? "Informe seu nome completo." : fieldErrors.fullName,
    licenseNumber: !isValidLicense(form.licenseNumber) ? "CRP no formato 06/12345." : fieldErrors.licenseNumber,
    phone: form.phone && !isValidPhone(form.phone) ? "Telefone inválido." : fieldErrors.phone,
  };
  const invalid = Object.values(errors).some(Boolean);

  function set<K extends keyof Form>(key: K, value: Form[K]) {
    setForm((f) => (f ? { ...f, [key]: value } : f));
    setFieldErrors((e) => ({ ...e, [key]: "" }));
    setDirty(true);
  }

  async function save(e: React.FormEvent) {
    e.preventDefault();
    if (!form || invalid) return;
    setBusy(true);
    try {
      await api.put(`/psychologists/${psychologist!.id}`, {
        fullName: form.fullName.trim(),
        licenseNumber: form.licenseNumber.trim(),
        approach: form.approach,
        phone: form.phone || null,
      });
      await refreshProfile();
      setDirty(false);
      notify("success", "Perfil atualizado.");
    } catch (err) {
      if (err instanceof ApiError) setFieldErrors(err.fields);
      notify("error", errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const workDays = [...new Set(psychologist.workingHours.map((w) => dayIndex(w.dayOfWeek)))];

  return (
    <>
      <PageHeader title="Meu perfil" subtitle="Seus dados profissionais — aparecem nos recibos, declarações e laudos." />

      <div className="split">
        <form className="card card-pad stack" onSubmit={save} noValidate>
          <div className="profile-head">
            <Avatar name={form.fullName || "?"} size="lg" />
            <div>
              <div className="strong" style={{ fontSize: 17 }}>
                {form.fullName || "Sem nome"}
              </div>
              <div className="muted small">{me ? mainRole(me.roles) : ""}</div>
            </div>
          </div>

          <hr className="divider" />

          <div className="field">
            <label className="label" htmlFor="pfName">
              Nome completo
            </label>
            <input id="pfName" className={`input ${errors.fullName ? "input-error" : ""}`} value={form.fullName} onChange={(e) => set("fullName", e.target.value)} />
            {errors.fullName && <span className="field-error">{errors.fullName}</span>}
          </div>

          <div className="grid-form">
            <div className="field">
              <label className="label" htmlFor="pfCrp">
                CRP
              </label>
              <input
                id="pfCrp"
                className={`input ${errors.licenseNumber ? "input-error" : ""}`}
                inputMode="numeric"
                value={form.licenseNumber}
                onChange={(e) => set("licenseNumber", maskLicense(e.target.value))}
              />
              {errors.licenseNumber && <span className="field-error">{errors.licenseNumber}</span>}
            </div>
            <div className="field">
              <label className="label" htmlFor="pfApproach">
                Abordagem
              </label>
              <select id="pfApproach" className="select" value={form.approach} onChange={(e) => set("approach", e.target.value as ApproachType)}>
                {APPROACHES.map((a) => (
                  <option key={a} value={a}>
                    {approachLabel[a]}
                  </option>
                ))}
              </select>
            </div>
          </div>

          <div className="field">
            <label className="label" htmlFor="pfPhone">
              Telefone
            </label>
            <input
              id="pfPhone"
              className={`input ${errors.phone ? "input-error" : ""}`}
              inputMode="numeric"
              placeholder="(00) 00000-0000"
              value={form.phone}
              onChange={(e) => set("phone", maskPhone(e.target.value))}
            />
            {errors.phone && <span className="field-error">{errors.phone}</span>}
          </div>

          <div>
            <button className="btn btn-primary" type="submit" disabled={!dirty || invalid || busy}>
              <Save /> {busy ? "Salvando…" : "Salvar alterações"}
            </button>
          </div>
        </form>

        <div className="stack">
          <div className="card card-pad stack">
            <div className="section-title">Conta</div>
            <div className="row gap-3">
              <Mail size={17} className="muted" />
              <div className="grow truncate">
                <div className="small muted">E-mail de acesso</div>
                <div className="truncate">{me?.email}</div>
              </div>
            </div>
            <div className="row gap-3">
              <Brain size={17} className="muted" />
              <div className="grow">
                <div className="small muted">Abordagem</div>
                <div>{approachLabel[form.approach]}</div>
              </div>
            </div>
            <div className="row gap-3">
              <BadgeCheck size={17} className="muted" />
              <div className="grow">
                <div className="small muted">CRP</div>
                <div className="mono">{form.licenseNumber || "—"}</div>
              </div>
            </div>
            <Link to="/trocar-senha" className="btn btn-ghost btn-sm" style={{ alignSelf: "flex-start" }}>
              <KeyRound size={15} /> Trocar senha
            </Link>
          </div>

          <div className="card card-pad stack">
            <div className="between row">
              <div className="section-title" style={{ margin: 0 }}>
                Atendimento
              </div>
              <Link to="/configuracoes/horarios" className="btn btn-ghost btn-sm">
                Editar
              </Link>
            </div>
            <div className="row gap-3">
              <Clock size={17} className="muted" />
              <div className="row gap-1 wrap">
                {[1, 2, 3, 4, 5, 6, 0].map((d) => (
                  <span key={d} className={`day-chip ${workDays.includes(d) ? "ok" : ""}`} title={workDays.includes(d) ? "Atende" : "Não atende"}>
                    {WEEKDAYS_SHORT[d]}
                  </span>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
