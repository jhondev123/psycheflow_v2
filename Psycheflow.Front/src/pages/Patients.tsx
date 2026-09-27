import { useMemo, useState } from "react";
import { Mail, Pencil, Phone, Plus, Search, Trash2, UserPlus, Users } from "lucide-react";
import { useStore, type PatientInput } from "@/store/AppStore";
import type { Patient } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal, ConfirmDialog } from "@/components/ui/Modal";
import { fmtDateShort } from "@/lib/format";
import { isValidCPF, isValidEmail, isValidPhone, maskCPF, maskPhone } from "@/lib/validation";
import "@/styles/pages.css";

const EMPTY: PatientInput = { name: "", email: "", cpf: "", phone: "", birthDate: "", notes: "" };

export function Patients() {
  const { db, addPatient, updatePatient, deletePatient, notify } = useStore();
  const [query, setQuery] = useState("");
  const [editing, setEditing] = useState<Patient | null>(null);
  const [creating, setCreating] = useState(false);
  const [removing, setRemoving] = useState<Patient | null>(null);

  const patients = useMemo(() => {
    const q = query.trim().toLowerCase();
    return [...db.patients]
      .filter(
        (p) =>
          !q ||
          p.name.toLowerCase().includes(q) ||
          p.email.toLowerCase().includes(q) ||
          (p.cpf ?? "").includes(q),
      )
      .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"));
  }, [db.patients, query]);

  function sessionCount(patientId: string) {
    return db.sessions.filter((s) => s.patientId === patientId).length;
  }

  return (
    <>
      <PageHeader
        title="Pacientes"
        subtitle="Cadastro e histórico de pacientes da clínica."
        actions={
          <button className="btn btn-primary" onClick={() => setCreating(true)}>
            <Plus /> Novo paciente
          </button>
        }
      />

      <div className="toolbar">
        <div className="search">
          <Search />
          <input
            className="input"
            placeholder="Buscar por nome, e-mail ou CPF…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
          />
        </div>
        <span className="count-pill">{patients.length}</span>
      </div>

      <div className="card">
        {patients.length === 0 ? (
          <EmptyState
            icon={<Users />}
            title={query ? "Nenhum paciente encontrado" : "Nenhum paciente cadastrado"}
            description={
              query
                ? "Tente outro termo de busca."
                : "Cadastre seu primeiro paciente para começar a agendar sessões."
            }
            action={
              !query && (
                <button className="btn btn-primary btn-sm" onClick={() => setCreating(true)}>
                  <UserPlus /> Cadastrar paciente
                </button>
              )
            }
          />
        ) : (
          <div className="table-scroll">
            <table className="table">
              <thead>
                <tr>
                  <th>Paciente</th>
                  <th>Contato</th>
                  <th>CPF</th>
                  <th>Nascimento</th>
                  <th>Sessões</th>
                  <th style={{ width: 96 }}></th>
                </tr>
              </thead>
              <tbody>
                {patients.map((p) => (
                  <tr key={p.id}>
                    <td>
                      <div className="person">
                        <Avatar name={p.name} size="sm" />
                        <div className="truncate">
                          <div className="pname truncate">{p.name}</div>
                          {p.notes && <div className="psub truncate">{p.notes}</div>}
                        </div>
                      </div>
                    </td>
                    <td>
                      <div className="col gap-1">
                        <span className="row gap-1 small muted">
                          <Mail size={13} /> {p.email}
                        </span>
                        {p.phone && (
                          <span className="row gap-1 small muted">
                            <Phone size={13} /> {p.phone}
                          </span>
                        )}
                      </div>
                    </td>
                    <td className="mono muted">{p.cpf ?? "—"}</td>
                    <td className="muted">{p.birthDate ? fmtDateShort(p.birthDate) : "—"}</td>
                    <td>
                      <span className="count-pill">{sessionCount(p.id)}</span>
                    </td>
                    <td>
                      <div className="cell-actions">
                        <button
                          className="btn btn-ghost btn-icon btn-sm"
                          onClick={() => setEditing(p)}
                          aria-label="Editar"
                          title="Editar"
                        >
                          <Pencil />
                        </button>
                        <button
                          className="btn btn-ghost btn-icon btn-sm"
                          onClick={() => setRemoving(p)}
                          aria-label="Excluir"
                          title="Excluir"
                        >
                          <Trash2 />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(creating || editing) && (
        <PatientForm
          initial={editing ?? undefined}
          onClose={() => {
            setCreating(false);
            setEditing(null);
          }}
          onSubmit={(input) => {
            if (editing) {
              updatePatient(editing.id, input);
              notify("success", "Paciente atualizado.");
            } else {
              addPatient(input);
              notify("success", "Paciente cadastrado.");
            }
            setCreating(false);
            setEditing(null);
          }}
        />
      )}

      {removing && (
        <ConfirmDialog
          title="Excluir paciente"
          danger
          confirmLabel="Excluir"
          message={
            <>
              Tem certeza que deseja excluir <b>{removing.name}</b>? Esta ação não pode ser desfeita.
            </>
          }
          onConfirm={() => {
            deletePatient(removing.id);
            notify("info", "Paciente excluído.");
          }}
          onClose={() => setRemoving(null)}
        />
      )}
    </>
  );
}

function PatientForm({
  initial,
  onClose,
  onSubmit,
}: {
  initial?: Patient;
  onClose: () => void;
  onSubmit: (input: PatientInput) => void;
}) {
  const [form, setForm] = useState<PatientInput>(
    initial
      ? {
          name: initial.name,
          email: initial.email,
          cpf: initial.cpf ?? "",
          phone: initial.phone ?? "",
          birthDate: initial.birthDate ?? "",
          notes: initial.notes ?? "",
        }
      : { ...EMPTY },
  );
  const [touched, setTouched] = useState(false);

  function set<K extends keyof PatientInput>(key: K, value: PatientInput[K]) {
    setForm((f) => ({ ...f, [key]: value }));
  }

  const errors = {
    name: form.name.trim().length < 3 ? "Informe o nome completo." : null,
    email: !isValidEmail(form.email) ? "E-mail inválido." : null,
    cpf: form.cpf && !isValidCPF(form.cpf) ? "CPF inválido." : null,
    phone: form.phone && !isValidPhone(form.phone) ? "Telefone inválido." : null,
  };
  const hasError = Object.values(errors).some(Boolean);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (hasError) return;
    onSubmit(form);
  }

  return (
    <Modal
      title={initial ? "Editar paciente" : "Novo paciente"}
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" type="submit" form="patient-form">
            {initial ? "Salvar alterações" : "Cadastrar"}
          </button>
        </>
      }
    >
      <form id="patient-form" className="grid-form" onSubmit={submit} noValidate>
        <div className="field span-2">
          <label className="label">
            Nome completo <span className="req">*</span>
          </label>
          <input
            className={`input ${touched && errors.name ? "input-error" : ""}`}
            placeholder="Nome do paciente"
            value={form.name}
            onChange={(e) => set("name", e.target.value)}
          />
          {touched && errors.name && <span className="field-error">{errors.name}</span>}
        </div>

        <div className="field span-2">
          <label className="label">
            E-mail <span className="req">*</span>
          </label>
          <input
            className={`input ${touched && errors.email ? "input-error" : ""}`}
            type="email"
            placeholder="paciente@email.com"
            value={form.email}
            onChange={(e) => set("email", e.target.value)}
          />
          {touched && errors.email && <span className="field-error">{errors.email}</span>}
        </div>

        <div className="field">
          <label className="label">CPF</label>
          <input
            className={`input ${touched && errors.cpf ? "input-error" : ""}`}
            inputMode="numeric"
            placeholder="000.000.000-00"
            value={form.cpf}
            onChange={(e) => set("cpf", maskCPF(e.target.value))}
          />
          {touched && errors.cpf && <span className="field-error">{errors.cpf}</span>}
        </div>

        <div className="field">
          <label className="label">Telefone</label>
          <input
            className={`input ${touched && errors.phone ? "input-error" : ""}`}
            inputMode="numeric"
            placeholder="(00) 00000-0000"
            value={form.phone}
            onChange={(e) => set("phone", maskPhone(e.target.value))}
          />
          {touched && errors.phone && <span className="field-error">{errors.phone}</span>}
        </div>

        <div className="field span-2">
          <label className="label">Data de nascimento</label>
          <input
            className="input"
            type="date"
            value={form.birthDate}
            max="2099-12-31"
            onChange={(e) => set("birthDate", e.target.value)}
          />
        </div>

        <div className="field span-2">
          <label className="label">Observações</label>
          <textarea
            className="textarea"
            placeholder="Notas clínicas, motivo do acompanhamento, etc."
            value={form.notes}
            onChange={(e) => set("notes", e.target.value)}
          />
        </div>
      </form>
    </Modal>
  );
}
