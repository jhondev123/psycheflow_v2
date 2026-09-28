import { useState } from "react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { AlertCircle, CalendarHeart, Eye, EyeOff, Lock, Mail, ShieldCheck, Sparkles } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { errorMessage } from "@/lib/api";
import { isValidEmail } from "@/lib/validation";
import "@/styles/auth.css";

export function Login() {
  const { login, notify } = useStore();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? "/";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPw, setShowPw] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [touched, setTouched] = useState(false);
  const [busy, setBusy] = useState(false);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (!isValidEmail(email) || password.length === 0) {
      setError("Informe um e-mail válido e a senha.");
      return;
    }
    setBusy(true);
    try {
      const me = await login(email, password);
      if (me.mustChangePassword) {
        navigate("/trocar-senha", { replace: true });
        return;
      }
      notify("success", `Bem-vindo(a), ${me.fullName.split(" ")[0]}!`);
      navigate(from, { replace: true });
    } catch (err) {
      setError(errorMessage(err, "Não foi possível entrar."));
    } finally {
      setBusy(false);
    }
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
          <p>Agenda, pacientes, sessões, financeiro, documentos e prontuários em um só lugar — pensado para psicólogos.</p>
        </div>
        <div className="auth-points">
          <div className="auth-point">
            <span className="ic">
              <CalendarHeart />
            </span>
            Agenda com confirmação, recorrência e bloqueios
          </div>
          <div className="auth-point">
            <span className="ic">
              <ShieldCheck />
            </span>
            Prontuários sigilosos com trilha de acesso
          </div>
          <div className="auth-point">
            <span className="ic">
              <Sparkles />
            </span>
            Sugestões com IA a partir de dados pseudonimizados
          </div>
        </div>
      </aside>

      <section className="auth-panel">
        <div className="auth-card">
          <h1>Entrar</h1>
          <p className="lead">Acesse o painel da sua clínica.</p>

          <form className="auth-form" onSubmit={submit} noValidate>
            {error && (
              <div className="auth-error" role="alert">
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
                <button type="button" className="pw-toggle" onClick={() => setShowPw((s) => !s)} aria-label={showPw ? "Ocultar senha" : "Mostrar senha"}>
                  {showPw ? <EyeOff /> : <Eye />}
                </button>
              </div>
            </div>

            <button className="btn btn-primary btn-block" type="submit" disabled={busy}>
              {busy ? "Entrando…" : "Entrar"}
            </button>
          </form>

          {import.meta.env.DEV && (
            <div className="auth-demo">
              <ShieldCheck size={16} />
              <div>
                Dados de demonstração (Docker em desenvolvimento): <b>ana@psycheflow.dev</b> (psicóloga) ou{" "}
                <b>admin@psycheflow.dev</b> · senha <span className="mono">Psycheflow@123</span>
              </div>
            </div>
          )}

          <div className="auth-alt">
            Não tem uma conta? <Link to="/register">Cadastre sua clínica</Link>
          </div>
        </div>
      </section>
    </div>
  );
}
