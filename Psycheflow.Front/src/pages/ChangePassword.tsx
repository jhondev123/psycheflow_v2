import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { AlertCircle, KeyRound, Lock } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { errorMessage } from "@/lib/api";
import { passwordIssues, PASSWORD_RULES } from "@/lib/validation";
import "@/styles/auth.css";

/** Troca de senha — obrigatória no primeiro acesso de quem recebeu senha temporária (RC-02). */
export function ChangePassword() {
  const { me, changePassword, logout, notify } = useStore();
  const navigate = useNavigate();
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const issues = useMemo(() => passwordIssues(next), [next]);
  const mismatch = confirm.length > 0 && confirm !== next;
  const invalid = !current || issues.length > 0 || next !== confirm;

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (invalid) return;
    setBusy(true);
    try {
      await changePassword(current, next);
      notify("success", "Senha alterada. Tudo pronto!");
      navigate("/", { replace: true });
    } catch (err) {
      setError(errorMessage(err, "Não foi possível trocar a senha."));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-wrap">
      <section className="auth-panel" style={{ gridColumn: "1 / -1" }}>
        <div className="auth-card">
          <h1>
            <KeyRound size={22} /> Defina sua senha
          </h1>
          <p className="lead">
            {me?.mustChangePassword
              ? "Você entrou com uma senha temporária. Crie uma senha pessoal para continuar."
              : "Informe a senha atual e a nova senha."}
          </p>

          <form className="auth-form" onSubmit={submit} noValidate>
            {error && (
              <div className="auth-error" role="alert">
                <AlertCircle />
                {error}
              </div>
            )}
            <div className="field">
              <label className="label" htmlFor="current">
                {me?.mustChangePassword ? "Senha temporária" : "Senha atual"}
              </label>
              <div className="input-icon">
                <Lock />
                <input id="current" className="input" type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
              </div>
            </div>
            <div className="field">
              <label className="label" htmlFor="next">
                Nova senha
              </label>
              <input id="next" className="input" type="password" autoComplete="new-password" value={next} onChange={(e) => setNext(e.target.value)} />
              <div className="pw-hint">
                {PASSWORD_RULES.map((r) => (
                  <span key={r.key} className={`pw-chip ${!issues.includes(r.key) ? "ok" : ""}`}>
                    {r.label}
                  </span>
                ))}
              </div>
            </div>
            <div className="field">
              <label className="label" htmlFor="confirm">
                Confirme a nova senha
              </label>
              <input
                id="confirm"
                className={`input ${mismatch ? "input-error" : ""}`}
                type="password"
                autoComplete="new-password"
                value={confirm}
                onChange={(e) => setConfirm(e.target.value)}
              />
              {mismatch && <span className="field-error">As senhas não conferem.</span>}
            </div>
            <button className="btn btn-primary btn-block" type="submit" disabled={invalid || busy}>
              {busy ? "Salvando…" : "Salvar nova senha"}
            </button>
          </form>
          <div className="auth-alt">
            <button className="btn btn-ghost btn-sm" onClick={logout}>
              Sair
            </button>
          </div>
        </div>
      </section>
    </div>
  );
}
