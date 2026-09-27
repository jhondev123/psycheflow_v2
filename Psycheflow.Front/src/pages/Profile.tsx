import { useState } from "react";
import { Link } from "react-router-dom";
import { BadgeCheck, Brain, Clock, Mail, Phone, Save } from "lucide-react";
import { useStore, type ProfileInput } from "@/store/AppStore";
import type { ApproachType } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { approachMeta, roleMeta, WEEKDAYS_SHORT } from "@/lib/domain";
import { maskPhone } from "@/lib/validation";
import "@/styles/pages.css";

export function Profile() {
  const { currentUser, activePsychologist, updateProfile, notify } = useStore();

  const [form, setForm] = useState<ProfileInput>({
    name: activePsychologist?.name ?? currentUser?.name ?? "",
    phone: activePsychologist?.phone ?? "",
    approach: activePsychologist?.approach ?? "PSYCHOANALYSIS",
    licenseNumber: activePsychologist?.licenseNumber ?? "",
  });
  const [dirty, setDirty] = useState(false);

  function set<K extends keyof ProfileInput>(key: K, value: ProfileInput[K]) {
    setForm((f) => ({ ...f, [key]: value }));
    setDirty(true);
  }

  function save(e: React.FormEvent) {
    e.preventDefault();
    if (form.name.trim().length < 3) return notify("error", "Informe seu nome completo.");
    updateProfile(form);
    setDirty(false);
    notify("success", "Perfil atualizado.");
  }

  const workDays = activePsychologist?.workingHours
    .map((w) => w.dayOfWeek)
    .sort((a, b) => a - b) ?? [];

  return (
    <>
      <PageHeader title="Perfil" subtitle="Seus dados profissionais." />

      <div className="split">
        <form className="card card-pad stack" onSubmit={save}>
          <div className="profile-head">
            <Avatar name={form.name || "?"} size="lg" />
            <div>
              <div className="strong" style={{ fontSize: 17 }}>
                {form.name || "Sem nome"}
              </div>
              <div className="muted small">{currentUser ? roleMeta[currentUser.role] : ""}</div>
            </div>
          </div>

          <hr className="divider" />

          <div className="field">
            <label className="label">Nome completo</label>
            <input
              className="input"
              value={form.name}
              onChange={(e) => set("name", e.target.value)}
            />
          </div>

          <div className="grid-form">
            <div className="field">
              <label className="label">CRP</label>
              <input
                className="input"
                inputMode="numeric"
                value={form.licenseNumber}
                onChange={(e) => set("licenseNumber", e.target.value.replace(/\D/g, ""))}
              />
            </div>
            <div className="field">
              <label className="label">Abordagem</label>
              <select
                className="select"
                value={form.approach}
                onChange={(e) => set("approach", e.target.value as ApproachType)}
              >
                <option value="PSYCHOANALYSIS">{approachMeta.PSYCHOANALYSIS}</option>
                <option value="BEHAVIORAL">{approachMeta.BEHAVIORAL}</option>
                <option value="None">{approachMeta.None}</option>
              </select>
            </div>
          </div>

          <div className="field">
            <label className="label">Telefone</label>
            <input
              className="input"
              inputMode="numeric"
              placeholder="(00) 00000-0000"
              value={form.phone}
              onChange={(e) => set("phone", maskPhone(e.target.value))}
            />
          </div>

          <div>
            <button className="btn btn-primary" type="submit" disabled={!dirty}>
              <Save /> Salvar alterações
            </button>
          </div>
        </form>

        <div className="stack">
          <div className="card card-pad stack">
            <div className="section-title">Conta</div>
            <div className="row gap-3">
              <Mail size={17} className="muted" />
              <div className="grow truncate">
                <div className="small muted">E-mail</div>
                <div className="truncate">{currentUser?.email}</div>
              </div>
            </div>
            <div className="row gap-3">
              <Brain size={17} className="muted" />
              <div className="grow">
                <div className="small muted">Abordagem</div>
                <div>{approachMeta[form.approach]}</div>
              </div>
            </div>
            <div className="row gap-3">
              <BadgeCheck size={17} className="muted" />
              <div className="grow">
                <div className="small muted">CRP</div>
                <div className="mono">{form.licenseNumber || "—"}</div>
              </div>
            </div>
            {form.phone && (
              <div className="row gap-3">
                <Phone size={17} className="muted" />
                <div className="grow">
                  <div className="small muted">Telefone</div>
                  <div>{form.phone}</div>
                </div>
              </div>
            )}
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
                {workDays.length === 0 ? (
                  <span className="muted">Nenhum dia configurado</span>
                ) : (
                  [1, 2, 3, 4, 5, 6, 0].map((d) => (
                    <span
                      key={d}
                      className={`day-chip ${workDays.includes(d) ? "ok" : ""}`}
                      title={workDays.includes(d) ? "Atende" : "Não atende"}
                    >
                      {WEEKDAYS_SHORT[d]}
                    </span>
                  ))
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
