import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, BadgeCheck, Building2, Brain, Eye, EyeOff, Lock, Mail, User } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { ApiError, errorMessage } from "@/lib/api";
import { APPROACHES, approachLabel } from "@/lib/domain";
import {
  isValidEmail,
  isValidLicense,
  isValidPhone,
  maskLicense,
  maskPhone,
  passwordIssues,
  PASSWORD_RULES,
} from "@/lib/validation";
import type { ApproachType } from "@/types";
import "@/styles/auth.css";

export function Register() {
  const { register, notify } = useStore();
  const navigate = useNavigate();

  const [companyName, setCompanyName] = useState("");
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [approach, setApproach] = useState<ApproachType>("CognitiveBehavioral");
  const [phone, setPhone] = useState("");
  const [password, setPassword] = useState("");
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [touched, setTouched] = useState(false);
  const [busy, setBusy] = useState(false);

  const pwIssues = useMemo(() => passwordIssues(password), [password]);

  const errors: Record<string, string | null> = {
    companyName: companyName.trim().length < 2 ? "Informe o nome da clínica (ou o seu, se atende sozinho)." : null,
    fullName: fullName.trim().length < 3 ? "Informe seu nome completo." : null,
    email: !isValidEmail(email) ? "E-mail inválido." : null,
    licenseNumber: !isValidLicense(licenseNumber) ? "CRP no formato 06/12345." : null,
    phone: phone && !isValidPhone(phone) ? "Telefone inválido." : null,
    password: pwIssues.length > 0 ? `A senha precisa de: ${pwIssues.join(", ")}.` : null,
  };

  const show = (key: string) => (touched ? errors[key] ?? fieldErrors[key] : fieldErrors[key]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (Object.values(errors).some(Boolean)) {
      setError("Revise os campos destacados.");
      return;
    }
    setBusy(true);
    try {
      await register({ companyName, fullName, email, password, licenseNumber, approach, phone: phone || null });
      notify("success", "Cadastro concluído. Boas-vindas ao Psycheflow!");
      navigate("/configuracoes/horarios", { replace: true });
    } catch (err) {
      setError(errorMessage(err, "Não foi possível concluir o cadastro."));
      if (err instanceof ApiError) setFieldErrors(err.fields);
    } finally {
      setBusy(false);
    }
  }

  function input(key: string, props: React.InputHTMLAttributes<HTMLInputElement>, icon?: React.ReactNode) {
    const field = <input id={key} className={`input ${show(key) ? "input-error" : ""}`} {...props} />;
    return icon ? (
      <div className="input-icon">
        {icon}
        {field}
      </div>
    ) : (
      field
    );
  }

  return (
    <div className="auth-wrap">
      <aside className="auth-aside">
        <div className="auth-brand">
          <img src="/favicon.svg" alt="" />
          Psycheflow
        </div>
        <div className="auth-pitch">
          <h2>Comece a organizar seus atendimentos hoje.</h2>
          <p>Cadastre sua clínica (ou seu consultório) — você entra como administrador(a) e psicólogo(a) e pode convidar a equipe depois.</p>
        </div>
        <div className="auth-points">
          <div className="auth-point">
            <span className="ic">
              <BadgeCheck />
            </span>
            Horários de atendimento e agenda prontos
          </div>
          <div className="auth-point">
            <span className="ic">
              <Brain />
            </span>
            Abordagem e CRP no seu perfil e nos documentos
          </div>
        </div>
      </aside>

      <section className="auth-panel">
        <div className="auth-card">
          <h1>Criar conta</h1>
          <p className="lead">Leva menos de um minuto.</p>

          <form className="auth-form" onSubmit={submit} noValidate>
            {error && (
              <div className="auth-error" role="alert">
                <AlertCircle />
                {error}
              </div>
            )}

            <div className="field">
              <label className="label" htmlFor="companyName">
                Clínica ou consultório
              </label>
              {input("companyName", { placeholder: "Clínica Viver", value: companyName, onChange: (e) => setCompanyName(e.target.value) }, <Building2 />)}
              {show("companyName") && <span className="field-error">{show("companyName")}</span>}
            </div>

            <div className="field">
              <label className="label" htmlFor="fullName">
                Seu nome completo
              </label>
              {input("fullName", { placeholder: "Ana Souza", value: fullName, onChange: (e) => setFullName(e.target.value) }, <User />)}
              {show("fullName") && <span className="field-error">{show("fullName")}</span>}
            </div>

            <div className="field">
              <label className="label" htmlFor="email">
                E-mail
              </label>
              {input("email", { type: "email", placeholder: "voce@clinica.com", value: email, onChange: (e) => setEmail(e.target.value) }, <Mail />)}
              {show("email") && <span className="field-error">{show("email")}</span>}
            </div>

            <div className="grid-form">
              <div className="field">
                <label className="label" htmlFor="licenseNumber">
                  CRP
                </label>
                {input("licenseNumber", {
                  inputMode: "numeric",
                  placeholder: "06/12345",
                  value: licenseNumber,
                  onChange: (e) => setLicenseNumber(maskLicense(e.target.value)),
                })}
                {show("licenseNumber") && <span className="field-error">{show("licenseNumber")}</span>}
              </div>
              <div className="field">
                <label className="label" htmlFor="approach">
                  Abordagem
                </label>
                <select id="approach" className="select" value={approach} onChange={(e) => setApproach(e.target.value as ApproachType)}>
                  {APPROACHES.map((a) => (
                    <option key={a} value={a}>
                      {approachLabel[a]}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div className="field">
              <label className="label" htmlFor="phone">
                Telefone (opcional)
              </label>
              {input("phone", { inputMode: "numeric", placeholder: "(45) 99999-1234", value: phone, onChange: (e) => setPhone(maskPhone(e.target.value)) })}
              {show("phone") && <span className="field-error">{show("phone")}</span>}
            </div>

            <div className="field">
              <label className="label" htmlFor="password">
                Senha
              </label>
              <div className="pw-field input-icon">
                <Lock />
                <input
                  id="password"
                  className="input"
                  type={showPw ? "text" : "password"}
                  autoComplete="new-password"
                  placeholder="Crie uma senha forte"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <button type="button" className="pw-toggle" onClick={() => setShowPw((s) => !s)} aria-label={showPw ? "Ocultar senha" : "Mostrar senha"}>
                  {showPw ? <EyeOff /> : <Eye />}
                </button>
              </div>
              <div className="pw-hint">
                {PASSWORD_RULES.map((r) => (
                  <span key={r.key} className={`pw-chip ${!pwIssues.includes(r.key) ? "ok" : ""}`}>
                    {r.label}
                  </span>
                ))}
              </div>
            </div>

            <button className="btn btn-primary btn-block" type="submit" disabled={busy}>
              {busy ? "Criando conta…" : "Criar conta"}
            </button>
          </form>

          <div className="auth-alt">
            Já tem uma conta? <Link to="/login">Entrar</Link>
          </div>
        </div>
      </section>
    </div>
  );
}
