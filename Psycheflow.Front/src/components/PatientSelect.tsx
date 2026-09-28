import { useMemo, useState } from "react";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { PagedResponse, PatientListItem } from "@/types";

interface PatientSelectProps {
  value: string;
  onChange: (patientId: string, patient?: PatientListItem) => void;
  id?: string;
  /** Mostra "Todos os pacientes" como primeira opção (filtros). */
  allowAll?: boolean;
  invalid?: boolean;
}

/** Seleção de paciente ativo com busca local (carrega até 100 pacientes da clínica). */
export function PatientSelect({ value, onChange, id, allowAll, invalid }: PatientSelectProps) {
  const [filter, setFilter] = useState("");
  const { data, loading } = useAsync(
    () => api.get<PagedResponse<PatientListItem>>("/patients", { status: "Active", pageSize: 100 }),
    [],
  );

  const patients = useMemo(() => {
    const q = filter.trim().toLowerCase();
    return (data?.items ?? [])
      .filter((p) => !q || p.fullName.toLowerCase().includes(q) || p.id === value)
      .sort((a, b) => a.fullName.localeCompare(b.fullName, "pt-BR"));
  }, [data, filter, value]);

  return (
    <div className="col gap-2">
      {(data?.items.length ?? 0) > 12 && (
        <input className="input" placeholder="Filtrar pacientes…" value={filter} onChange={(e) => setFilter(e.target.value)} />
      )}
      <select
        id={id}
        className={`select ${invalid ? "input-error" : ""}`}
        value={value}
        disabled={loading}
        onChange={(e) => onChange(e.target.value, data?.items.find((p) => p.id === e.target.value))}
      >
        <option value="">{loading ? "Carregando…" : allowAll ? "Todos os pacientes" : "Selecione um paciente…"}</option>
        {patients.map((p) => (
          <option key={p.id} value={p.id}>
            {p.fullName}
          </option>
        ))}
      </select>
    </div>
  );
}
