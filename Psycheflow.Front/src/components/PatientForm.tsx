import { useState } from "react";
import { Modal } from "@/components/ui/Modal";
import { api, ApiError, errorMessage } from "@/lib/api";
import { fmtCpf, fmtPhone } from "@/lib/format";
import { isValidCPF, isValidEmail, isValidPhone, isValidZipCode, maskCPF, maskPhone, maskZipCode } from "@/lib/validation";
import { useStore } from "@/store/AppStore";
import type { Address, Patient, PatientStatus } from "@/types";

interface Form {
  fullName: string;
  cpf: string;
  email: string;
  phone: string;
  birthDate: string;
  status: PatientStatus;
  notes: string;
  hasAddress: boolean;
  zipCode: string;
  street: string;
  number: string;
  complement: string;
  neighborhood: string;
  city: string;
  state: string;
}

function toForm(p?: Patient): Form {
  return {
    fullName: p?.fullName ?? "",
    cpf: p ? fmtCpf(p.cpf) : "",
    email: p?.email ?? "",
    phone: p ? fmtPhone(p.phone) : "",
    birthDate: p?.birthDate ?? "",
    status: p?.status ?? "Active",
    notes: p?.notes ?? "",
    hasAddress: !!p?.address,
    zipCode: p?.address ? maskZipCode(p.address.zipCode) : "",
    street: p?.address?.street ?? "",
    number: p?.address?.number ?? "",
    complement: p?.address?.complement ?? "",
    neighborhood: p?.address?.neighborhood ?? "",
    city: p?.address?.city ?? "",
    state: p?.address?.state ?? "",
  };
}

interface PatientFormProps {
  patient?: Patient;
  onClose: () => void;
  onSaved: (patient: Patient) => void;
}

/** RF001/RF003: cadastro e edição de paciente. CPF não muda depois de cadastrado (RN-11). */
export function PatientForm({ patient, onClose, onSaved }: PatientFormProps) {
  const { notify } = useStore();
  const [form, setForm] = useState<Form>(() => toForm(patient));
  const [touched, setTouched] = useState(false);
  const [serverErrors, setServerErrors] = useState<Record<string, string>>({});
  const [busy, setBusy] = useState(false);
  const [lookingUp, setLookingUp] = useState(false);

  function set<K extends keyof Form>(key: K, value: Form[K]) {
    setForm((f) => ({ ...f, [key]: value }));
    setServerErrors((e) => ({ ...e, [key]: "", [`address.${key}`]: "" }));
  }

  const errors: Record<string, string | null> = {
    fullName: form.fullName.trim().length < 3 ? "Informe o nome completo." : null,
    cpf: !patient && !isValidCPF(form.cpf) ? "CPF inválido." : null,
    email: !isValidEmail(form.email) ? "E-mail inválido." : null,
    phone: !isValidPhone(form.phone) ? "Telefone com DDD." : null,
    zipCode: form.hasAddress && !isValidZipCode(form.zipCode) ? "CEP com 8 dígitos." : null,
    street: form.hasAddress && !form.street.trim() ? "Informe a rua." : null,
    number: form.hasAddress && !form.number.trim() ? "Informe o número." : null,
    neighborhood: form.hasAddress && !form.neighborhood.trim() ? "Informe o bairro." : null,
    city: form.hasAddress && !form.city.trim() ? "Informe a cidade." : null,
    state: form.hasAddress && !/^[A-Za-z]{2}$/.test(form.state.trim()) ? "UF com 2 letras." : null,
  };
  const hasError = Object.values(errors).some(Boolean);
  const err = (key: string) => (touched ? errors[key] : null) || serverErrors[key] || serverErrors[`address.${key}`] || null;

  /** Preenche o endereço pelo CEP (ViaCEP), como pede o RF001. */
  async function lookupZipCode(value: string) {
    const digits = value.replace(/\D/g, "");
    if (digits.length !== 8) return;
    setLookingUp(true);
    try {
      const response = await fetch(`https://viacep.com.br/ws/${digits}/json/`);
      const data = (await response.json()) as { erro?: boolean; logradouro?: string; bairro?: string; localidade?: string; uf?: string };
      if (data.erro) {
        notify("info", "CEP não encontrado. Preencha o endereço manualmente.");
        return;
      }
      setForm((f) => ({
        ...f,
        street: data.logradouro || f.street,
        neighborhood: data.bairro || f.neighborhood,
        city: data.localidade || f.city,
        state: data.uf || f.state,
      }));
    } catch {
      notify("info", "Não foi possível consultar o CEP. Preencha o endereço manualmente.");
    } finally {
      setLookingUp(false);
    }
  }

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setTouched(true);
    if (hasError) return;

    const address: Address | null = form.hasAddress
      ? {
          zipCode: form.zipCode.replace(/\D/g, ""),
          street: form.street.trim(),
          number: form.number.trim(),
          complement: form.complement.trim() || null,
          neighborhood: form.neighborhood.trim(),
          city: form.city.trim(),
          state: form.state.trim().toUpperCase(),
        }
      : null;
    const body = {
      fullName: form.fullName.trim(),
      email: form.email.trim(),
      phone: form.phone,
      birthDate: form.birthDate || null,
      address,
      notes: form.notes.trim() || null,
    };

    setBusy(true);
    try {
      const saved = patient
        ? await api.put<Patient>(`/patients/${patient.id}`, { ...body, status: form.status })
        : await api.post<Patient>("/patients", { ...body, cpf: form.cpf });
      notify("success", patient ? "Paciente atualizado." : "Paciente cadastrado.");
      onSaved(saved);
    } catch (error) {
      if (error instanceof ApiError) setServerErrors(error.fields);
      notify("error", errorMessage(error));
    } finally {
      setBusy(false);
    }
  }

  const text = (key: keyof Form, label: string, props: React.InputHTMLAttributes<HTMLInputElement> = {}, span2 = false) => (
    <div className={`field ${span2 ? "span-2" : ""}`}>
      <label className="label" htmlFor={`pf-${key}`}>
        {label}
      </label>
      <input
        id={`pf-${key}`}
        className={`input ${err(key) ? "input-error" : ""}`}
        value={form[key] as string}
        onChange={(e) => set(key, e.target.value as never)}
        {...props}
      />
      {err(key) && <span className="field-error">{err(key)}</span>}
    </div>
  );

  return (
    <Modal
      title={patient ? "Editar paciente" : "Novo paciente"}
      size="lg"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" type="submit" form="patient-form" disabled={busy}>
            {busy ? "Salvando…" : patient ? "Salvar alterações" : "Cadastrar"}
          </button>
        </>
      }
    >
      <form id="patient-form" className="grid-form" onSubmit={submit} noValidate>
        {text("fullName", "Nome completo *", { placeholder: "Nome do paciente" }, true)}
        {patient ? (
          <div className="field">
            <span className="label">CPF</span>
            <div className="input mono" style={{ display: "flex", alignItems: "center" }} aria-readonly>
              {form.cpf}
            </div>
          </div>
        ) : (
          text("cpf", "CPF *", { inputMode: "numeric", placeholder: "000.000.000-00", onChange: (e) => set("cpf", maskCPF(e.target.value)) })
        )}
        {text("birthDate", "Data de nascimento", { type: "date", max: new Date().toISOString().slice(0, 10) })}
        {text("email", "E-mail *", { type: "email", placeholder: "paciente@email.com" })}
        {text("phone", "Telefone *", { inputMode: "numeric", placeholder: "(00) 00000-0000", onChange: (e) => set("phone", maskPhone(e.target.value)) })}
        {patient && (
          <div className="field">
            <label className="label" htmlFor="pf-status">
              Situação
            </label>
            <select id="pf-status" className="select" value={form.status} onChange={(e) => set("status", e.target.value as PatientStatus)}>
              <option value="Active">Ativo</option>
              <option value="Inactive">Inativo</option>
            </select>
          </div>
        )}

        <label className="row gap-2 small span-2">
          <input type="checkbox" checked={form.hasAddress} onChange={(e) => set("hasAddress", e.target.checked)} /> Informar endereço
        </label>
        {form.hasAddress && (
          <>
            {text("zipCode", `CEP *${lookingUp ? " (buscando…)" : ""}`, {
              inputMode: "numeric",
              placeholder: "00000-000",
              onChange: (e) => set("zipCode", maskZipCode(e.target.value)),
              onBlur: (e) => void lookupZipCode(e.target.value),
            })}
            {text("state", "UF *", { maxLength: 2, placeholder: "PR", onChange: (e) => set("state", e.target.value.toUpperCase()) })}
            {text("street", "Rua *", {}, true)}
            {text("number", "Número *")}
            {text("complement", "Complemento")}
            {text("neighborhood", "Bairro *")}
            {text("city", "Cidade *")}
          </>
        )}

        <div className="field span-2">
          <label className="label" htmlFor="pf-notes">
            Observações
          </label>
          <textarea
            id="pf-notes"
            className="textarea"
            placeholder="Encaminhamento, contato de emergência, preferências de horário…"
            maxLength={2000}
            value={form.notes}
            onChange={(e) => set("notes", e.target.value)}
          />
        </div>
      </form>
    </Modal>
  );
}
