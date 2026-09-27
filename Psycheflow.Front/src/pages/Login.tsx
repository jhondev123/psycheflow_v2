import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { AlertCircle, CalendarHeart, Eye, EyeOff, Lock, Mail, ShieldCheck, Users } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { isValidEmail } from "@/lib/validation";
import "@/styles/auth.css";

export function Login() {
  const { login, notify } = useStore();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? "/";

  const [email, setEmail] = useState("ana@psycheflow.com");
  const [password, setPassword] = useState("Test123$");
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (!isValidEmail(email) || password.length === 0) {
      setError("Informe um e-mail válido e a senha.");
      return;
    }
    const res = login(email, password);
    if (!res.ok) {
      setError(res.error ?? "Não foi possível entrar.");
      return;
    }
    notify("success", "Bem-vindo(a) de volta!");
    navigate(from, { replace: true });
  }

  return (
    <div className="auth-wrap">
      <aside className="auth-aside">
        <div className="auth-brand">
          <img src="/favicon.svg" alt="" />
          Psycheflow
        </div>
        <div className="auth-pitch">
          <h2>A gestão da sua clínica, simples e no seu tempo.</h2>
          <p>
            Agenda inteligente, prontuário de sessões e cadastro de pacientes em um só lugar —
            pensado para psicólogos.
          </p>
        </div>
        <div className="auth-points">
          <div className="auth-point">
            <span className="ic">
              <CalendarHeart />
            </span>
            Agenda visual com confirmação e bloqueios
          </div>
          <div className="auth-point">
            <span className="ic">
              <Users />
            </span>
            Pacientes e histórico de atendimentos
          </div>
          <div className="auth-point">
            <span className="ic">
              <ShieldCheck />
            </span>
            Seus dados ficam no seu navegador
          </div>
        </div>
      </aside>

      <section className="auth-panel">
        <div className="auth-card">
          <h1>Entrar</h1>
          <p className="lead">Acesse o painel da sua clínica.</p>

          <form className="auth-form" onSubmit={submit} noValidate>
            {error && (
              <div className="auth-error">
                <AlertCircle />
                {error}
              </div>
            )}

            <div className="field">
              <label className="label" htmlFor="email">
                E-mail
              </label>
              <div className="input-icon">
                <Mail />
                <input
                  id="email"
                  className={`input ${touched && !isValidEmail(email) ? "input-error" : ""}`}
                  type="email"
                  autoComplete="email"
                  placeholder="voce@clinica.com"
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    setError(null);
                  }}
                />
              </div>
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
                  autoComplete="current-password"
                  placeholder="••••••••"
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
            </div>

            <button className="btn btn-primary btn-block" type="submit">
              Entrar
            </button>
          </form>

          <div className="auth-demo">
            <ShieldCheck size={16} />
            <div>
              Conta demo: <b>ana@psycheflow.com</b> · senha <span className="mono">Test123$</span>
            </div>
          </div>

          <div className="auth-alt">
            Não tem uma conta? <Link to="/register">Cadastre-se</Link>
          </div>
        </div>
      </section>
    </div>
  );
}
