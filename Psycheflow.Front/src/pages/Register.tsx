import { useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { AlertCircle, BadgeCheck, Brain, Eye, EyeOff, Lock, Mail, User } from "lucide-react";
import { useStore } from "@/store/AppStore";
import type { ApproachType } from "@/types";
import { approachMeta } from "@/lib/domain";
import { isValidEmail, passwordIssues } from "@/lib/validation";
import "@/styles/auth.css";

const PW_RULES: Array<{ key: string; label: string }> = [
  { key: "ao menos 6 caracteres", label: "6+ caracteres" },
  { key: "uma letra maiúscula", label: "Maiúscula" },
  { key: "uma letra minúscula", label: "Minúscula" },
  { key: "um número", label: "Número" },
  { key: "um símbolo", label: "Símbolo" },
];

export function Register() {
  const { register, notify } = useStore();
  const navigate = useNavigate();

  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [approach, setApproach] = useState<ApproachType>("PSYCHOANALYSIS");
  const [password, setPassword] = useState("");
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);

  const pwIssues = useMemo(() => passwordIssues(password), [password]);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (name.trim().length < 3) return setError("Informe seu nome completo.");
    if (!isValidEmail(email)) return setError("Informe um e-mail válido.");
    if (!/^\d{3,}$/.test(licenseNumber.trim()))
      return setError("Informe o número do CRP (apenas dígitos).");
    if (pwIssues.length > 0) return setError("A senha precisa de: " + pwIssues.join(", ") + ".");

    const res = register({ name, email, password, licenseNumber, approach });
    if (!res.ok) return setError(res.error ?? "Não foi possível concluir o cadastro.");
    notify("success", "Cadastro concluído. Boas-vindas ao Psycheflow!");
    navigate("/", { replace: true });
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
          <p>
            Crie sua conta de psicólogo(a) e tenha agenda, pacientes e sessões prontos para usar —
            sem instalar nada.
          </p>
        </div>
        <div className="auth-points">
          <div className="auth-point">
            <span className="ic">
              <BadgeCheck />
            </span>
            Configuração de horários de atendimento
          </div>
          <div className="auth-point">
            <span className="ic">
              <Brain />
            </span>
            Abordagem e CRP no seu perfil
          </div>
        </div>
      </aside>

      <section className="auth-panel">
        <div className="auth-card">
          <h1>Criar conta</h1>
          <p className="lead">Leva menos de um minuto.</p>

          <form className="auth-form" onSubmit={submit} noValidate>
            {error && (
              <div className="auth-error">
                <AlertCircle />
                {error}
              </div>
            )}

            <div className="field">
              <label className="label" htmlFor="name">
                Nome completo
              </label>
              <div className="input-icon">
                <User />
                <input
                  id="name"
                  className={`input ${touched && name.trim().length < 3 ? "input-error" : ""}`}
                  placeholder="Dra. Ana Beatriz"
                  value={name}
                  onChange={(e) => {
                    setName(e.target.value);
                    setError(null);
                  }}
                />
              </div>
            </div>

            <div className="field">
              <label className="label" htmlFor="remail">
                E-mail
              </label>
              <div className="input-icon">
                <Mail />
                <input
                  id="remail"
                  className={`input ${touched && !isValidEmail(email) ? "input-error" : ""}`}
                  type="email"
                  placeholder="voce@clinica.com"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    setError(null);
                  }}
                />
              </div>
            </div>

            <div className="grid-form">
              <div className="field">
                <label className="label" htmlFor="crp">
                  CRP
                </label>
                <input
                  id="crp"
                  className="input"
                  inputMode="numeric"
                  placeholder="06123"
                  value={licenseNumber}
                  onChange={(e) => {
                    setLicenseNumber(e.target.value.replace(/\D/g, ""));
                    setError(null);
                  }}
                />
              </div>
              <div className="field">
                <label className="label" htmlFor="approach">
                  Abordagem
                </label>
                <select
                  id="approach"
                  className="select"
                  value={approach}
                  onChange={(e) => setApproach(e.target.value as ApproachType)}
                >
                  <option value="PSYCHOANALYSIS">{approachMeta.PSYCHOANALYSIS}</option>
                  <option value="BEHAVIORAL">{approachMeta.BEHAVIORAL}</option>
                  <option value="None">{approachMeta.None}</option>
                </select>
              </div>
            </div>

            <div className="field">
              <label className="label" htmlFor="rpassword">
                Senha
              </label>
              <div className="pw-field input-icon">
                <Lock />
                <input
                  id="rpassword"
                  className="input"
                  type={showPw ? "text" : "password"}
                  placeholder="Crie uma senha forte"
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    setError(null);
                  }}
                />
                <button
                  type="button"
                  className="pw-toggle"
                  onClick={() => setShowPw((s) => !s)}
                  aria-label={showPw ? "Ocultar senha" : "Mostrar senha"}
                >
                  {showPw ? <EyeOff /> : <Eye />}
                </button>
              </div>
              <div className="pw-hint">
                {PW_RULES.map((r) => (
                  <span key={r.key} className={`pw-chip ${!pwIssues.includes(r.key) ? "ok" : ""}`}>
                    {r.label}
                  </span>
                ))}
              </div>
            </div>

            <button className="btn btn-primary btn-block" type="submit">
              Criar conta
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
