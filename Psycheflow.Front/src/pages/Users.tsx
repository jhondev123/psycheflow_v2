import { useState } from "react";
import { KeyRound, Lock, UserPlus, Users as UsersIcon } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, ApiError, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { ApproachType, CreatedUser, Role, UserItem } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { Modal } from "@/components/ui/Modal";
import { APPROACHES, approachLabel, roleLabel } from "@/lib/domain";
import { isValidEmail, isValidLicense, maskLicense } from "@/lib/validation";
import "@/styles/pages.css";

/** RC-02: usuários da clínica. Quem é criado recebe senha temporária e troca no primeiro acesso. */
export function Users() {
  const { data, loading, error, reload } = useAsync(() => api.get<UserItem[]>("/users"), []);
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState<CreatedUser | null>(null);

  return (
    <>
      <PageHeader
        title="Usuários"
        subtitle="Equipe da clínica: administradores, gestores e psicólogos."
        actions={
          <button className="btn btn-primary" onClick={() => setCreating(true)}>
            <UserPlus /> Novo usuário
          </button>
        }
      />
      <div className="card">
        {loading && !data ? (
          <Loading />
        ) : error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : !data || data.length === 0 ? (
          <EmptyState icon={<UsersIcon />} title="Nenhum usuário" />
        ) : (
          <div className="table-scroll">
            <table className="table">
              <thead>
                <tr>
                  <th>Nome</th>
                  <th>E-mail</th>
                  <th>Perfis</th>
                  <th>Situação</th>
                </tr>
              </thead>
              <tbody>
                {data.map((u) => (
                  <tr key={u.id}>
                    <td>
                      <div className="person">
                        <Avatar name={u.fullName} size="sm" />
                        <div className="pname truncate">{u.fullName}</div>
                      </div>
                    </td>
                    <td className="muted">{u.email}</td>
                    <td>
                      <div className="row gap-1 wrap">
                        {u.roles.map((r) => (
                          <span key={r} className="badge">
                            {roleLabel[r]}
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>
                      {u.isLockedOut ? (
                        <span className="badge badge-danger">
                          <Lock size={12} /> Bloqueado
                        </span>
                      ) : u.mustChangePassword ? (
                        <span className="badge badge-warning">
                          <KeyRound size={12} /> Aguardando 1º acesso
                        </span>
                      ) : (
                        <span className="badge badge-success">Ativo</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {creating && (
        <UserForm
          onClose={() => setCreating(false)}
          onCreated={(user) => {
            setCreating(false);
            setCreated(user);
            void reload();
          }}
        />
      )}
      {created && (
        <Modal
          title="Usuário criado"
          onClose={() => setCreated(null)}
          footer={
            <button className="btn btn-primary" onClick={() => setCreated(null)}>
              Pronto
            </button>
          }
        >
          <div className="col gap-3">
            <div>
              Envie a senha temporária para <b>{created.fullName}</b> ({created.email}). Ela só é mostrada agora; no primeiro acesso
              a pessoa define a própria senha.
            </div>
            <div className="secret">{created.temporaryPassword}</div>
          </div>
        </Modal>
      )}
    </>
  );
}

function UserForm({ onClose, onCreated }: { onClose: () => void; onCreated: (user: CreatedUser) => void }) {
  const { me, notify } = useStore();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<Role>("Psychologist");
  const [licenseNumber, setLicenseNumber] = useState("");
  const [approach, setApproach] = useState<ApproachType>("CognitiveBehavioral");
  const [touched, setTouched] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);

  const errors: Record<string, string | undefined> = {
    fullName: fullName.trim().length < 3 ? "Informe o nome completo." : fieldErrors.fullName,
    email: !isValidEmail(email) ? "E-mail inválido." : fieldErrors.email,
    licenseNumber: role === "Psychologist" && !isValidLicense(licenseNumber) ? "CRP no formato 06/12345." : fieldErrors.licenseNumber,
  };
  const invalid = Object.values(errors).some(Boolean);
  const show = (key: string) => (touched ? errors[key] : fieldErrors[key]);

  async function submit() {
    setTouched(true);
    if (invalid) return;
    setBusy(true);
    try {
      const user = await api.post<CreatedUser>("/users", {
        fullName: fullName.trim(),
        email: email.trim(),
        role,
        licenseNumber: role === "Psychologist" ? licenseNumber : null,
        approach: role === "Psychologist" ? approach : null,
      });
      notify("success", "Usuário criado.");
      onCreated(user);
    } catch (e) {
      if (e instanceof ApiError) setFieldErrors(e.fields);
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  const roles: Role[] = me?.roles.includes("Admin") ? ["Psychologist", "Manager", "Admin"] : ["Psychologist", "Manager"];

  return (
    <Modal
      title="Novo usuário"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" disabled={busy} onClick={submit}>
            {busy ? "Criando…" : "Criar usuário"}
          </button>
        </>
      }
    >
      <div className="grid-form">
        <div className="field span-2">
          <label className="label" htmlFor="uName">
            Nome completo
          </label>
          <input id="uName" className={`input ${show("fullName") ? "input-error" : ""}`} value={fullName} onChange={(e) => setFullName(e.target.value)} />
          {show("fullName") && <span className="field-error">{show("fullName")}</span>}
        </div>
        <div className="field span-2">
          <label className="label" htmlFor="uEmail">
            E-mail
          </label>
          <input id="uEmail" type="email" className={`input ${show("email") ? "input-error" : ""}`} value={email} onChange={(e) => setEmail(e.target.value)} />
          {show("email") && <span className="field-error">{show("email")}</span>}
        </div>
        <div className="field span-2">
          <label className="label" htmlFor="uRole">
            Perfil
          </label>
          <select id="uRole" className="select" value={role} onChange={(e) => setRole(e.target.value as Role)}>
            {roles.map((r) => (
              <option key={r} value={r}>
                {roleLabel[r]}
              </option>
            ))}
          </select>
        </div>
        {role === "Psychologist" && (
          <>
            <div className="field">
              <label className="label" htmlFor="uCrp">
                CRP
              </label>
              <input
                id="uCrp"
                className={`input ${show("licenseNumber") ? "input-error" : ""}`}
                inputMode="numeric"
                placeholder="06/12345"
                value={licenseNumber}
                onChange={(e) => setLicenseNumber(maskLicense(e.target.value))}
              />
              {show("licenseNumber") && <span className="field-error">{show("licenseNumber")}</span>}
            </div>
            <div className="field">
              <label className="label" htmlFor="uApproach">
                Abordagem
              </label>
              <select id="uApproach" className="select" value={approach} onChange={(e) => setApproach(e.target.value as ApproachType)}>
                {APPROACHES.map((a) => (
                  <option key={a} value={a}>
                    {approachLabel[a]}
                  </option>
                ))}
              </select>
            </div>
          </>
        )}
      </div>
    </Modal>
  );
}
