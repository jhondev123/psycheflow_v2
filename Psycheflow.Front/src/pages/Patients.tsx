import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Mail, Phone, Plus, Search, UserPlus, Users } from "lucide-react";
import type { PagedResponse, PatientListItem, PatientStatus } from "@/types";
import { api } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import { PageHeader } from "@/components/layout/PageHeader";
import { Avatar } from "@/components/ui/Avatar";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading, Pager } from "@/components/ui/Feedback";
import { PatientForm } from "@/components/PatientForm";
import { patientStatusMeta } from "@/lib/domain";
import { fmtCpf, fmtPhone, fmtRelativeDay } from "@/lib/format";
import "@/styles/pages.css";

/** RF001–RF003: lista com busca (nome, e-mail ou CPF), filtro por situação e última sessão. */
export function Patients() {
  const navigate = useNavigate();
  const [query, setQuery] = useState("");
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<"" | PatientStatus>("Active");
  const [lastSessionFrom, setLastSessionFrom] = useState("");
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);

  // Busca com um pequeno atraso enquanto a pessoa digita.
  useEffect(() => {
    const id = setTimeout(() => {
      const q = query.trim();
      // CPF digitado com máscara vira só dígitos.
      setSearch(/^[\d.\-\s]+$/.test(q) ? q.replace(/\D/g, "") : q);
      setPage(1);
    }, 300);
    return () => clearTimeout(id);
  }, [query]);

  const { data, loading, error, reload } = useAsync(
    () => api.get<PagedResponse<PatientListItem>>("/patients", { search, status, lastSessionFrom, page, pageSize: 20 }),
    [search, status, lastSessionFrom, page],
  );

  return (
    <>
      <PageHeader
        title="Pacientes"
        subtitle="Cadastro e acompanhamento dos pacientes da clínica."
        actions={
          <button className="btn btn-primary" onClick={() => setCreating(true)}>
            <Plus /> Novo paciente
          </button>
        }
      />

      <div className="filters">
        <div className="search grow">
          <Search />
          <input className="input" placeholder="Buscar por nome, e-mail ou CPF completo…" value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="fStatus">
            Situação
          </label>
          <select
            id="fStatus"
            className="select"
            value={status}
            onChange={(e) => {
              setStatus(e.target.value as "" | PatientStatus);
              setPage(1);
            }}
          >
            <option value="">Todos</option>
            <option value="Active">Ativos</option>
            <option value="Inactive">Inativos</option>
          </select>
        </div>
        <div className="field">
          <label className="label" htmlFor="fLast">
            Última sessão a partir de
          </label>
          <input
            id="fLast"
            className="input"
            type="date"
            value={lastSessionFrom}
            onChange={(e) => {
              setLastSessionFrom(e.target.value);
              setPage(1);
            }}
          />
        </div>
      </div>

      <div className="card">
        {loading && !data ? (
          <Loading />
        ) : error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState
            icon={<Users />}
            title={search || lastSessionFrom ? "Nenhum paciente encontrado" : "Nenhum paciente cadastrado"}
            description={search || lastSessionFrom ? "Tente outro termo ou filtro." : "Cadastre o primeiro paciente para começar a agendar sessões."}
            action={
              !search && (
                <button className="btn btn-primary btn-sm" onClick={() => setCreating(true)}>
                  <UserPlus /> Cadastrar paciente
                </button>
              )
            }
          />
        ) : (
          <>
            <div className="table-scroll">
              <table className="table">
                <thead>
                  <tr>
                    <th>Paciente</th>
                    <th>Contato</th>
                    <th>CPF</th>
                    <th>Última sessão</th>
                    <th>Situação</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map((p) => (
                    <tr key={p.id} style={{ cursor: "pointer" }} onClick={() => navigate(`/pacientes/${p.id}`)}>
                      <td>
                        <div className="person">
                          <Avatar name={p.fullName} size="sm" />
                          <div className="pname truncate">{p.fullName}</div>
                        </div>
                      </td>
                      <td>
                        <div className="col gap-1">
                          <span className="row gap-1 small muted">
                            <Mail size={13} /> {p.email}
                          </span>
                          <span className="row gap-1 small muted">
                            <Phone size={13} /> {fmtPhone(p.phone)}
                          </span>
                        </div>
                      </td>
                      <td className="mono muted">{fmtCpf(p.cpf)}</td>
                      <td className="muted">{p.lastSessionDate ? fmtRelativeDay(p.lastSessionDate) : "—"}</td>
                      <td>
                        <span className={`badge ${patientStatusMeta[p.status].badge}`}>{patientStatusMeta[p.status].label}</span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <Pager page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPage={setPage} />
          </>
        )}
      </div>

      {creating && (
        <PatientForm
          onClose={() => setCreating(false)}
          onSaved={(p) => {
            setCreating(false);
            navigate(`/pacientes/${p.id}`);
          }}
        />
      )}
    </>
  );
}
