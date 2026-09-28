import { useState } from "react";
import { useSearchParams } from "react-router-dom";
import { endOfMonth, startOfMonth } from "date-fns";
import { CheckCircle2, FileDown, FileText, Pencil, Plus, ScrollText, Trash2 } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { PaymentStatus, PsychologicalReport, Psychologist, ReportTemplate, SessionStatus } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { ConfirmDialog, Modal } from "@/components/ui/Modal";
import { PatientSelect } from "@/components/PatientSelect";
import { reportStatusMeta, reportTemplateLabel } from "@/lib/domain";
import { fmtDateShort, isoDate } from "@/lib/format";
import "@/styles/pages.css";

/** RF014–RF016: relatórios em PDF e laudos/relatórios psicológicos (CFP 06/2019). */
export function Documents() {
  const { isPsychologist } = useStore();
  return (
    <>
      <PageHeader title="Documentos" subtitle="Relatórios em PDF e documentos psicológicos." />
      <div className="split" style={{ marginBottom: 20 }}>
        <SessionsReportCard />
        {isPsychologist && <FeedbackReportCard />}
      </div>
      {isPsychologist && <Reports />}
    </>
  );
}

function useDownload() {
  const { notify } = useStore();
  const [busy, setBusy] = useState(false);
  async function download(path: string, query?: Record<string, string>) {
    setBusy(true);
    try {
      await api.download(path, query);
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }
  return { busy, download };
}

function SessionsReportCard() {
  const { isManagement } = useStore();
  const [from, setFrom] = useState(isoDate(startOfMonth(new Date())));
  const [to, setTo] = useState(isoDate(endOfMonth(new Date())));
  const [sessionStatus, setSessionStatus] = useState<"" | SessionStatus>("");
  const [paymentStatus, setPaymentStatus] = useState<"" | PaymentStatus>("");
  const [psychologistId, setPsychologistId] = useState("");
  const psychologists = useAsync(() => api.get<Psychologist[]>("/psychologists"), [], isManagement);
  const { busy, download } = useDownload();

  return (
    <div className="card card-pad stack">
      <div className="row gap-2">
        <FileText size={18} className="muted" />
        <div className="strong">Relatório de sessões</div>
      </div>
      <div className="small muted">Sessões do período com situação, valores e totais (RF015).</div>
      <div className="grid-form">
        <div className="field">
          <label className="label" htmlFor="srFrom">
            De
          </label>
          <input id="srFrom" className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="srTo">
            Até
          </label>
          <input id="srTo" className="input" type="date" value={to} min={from} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="srSession">
            Situação da sessão
          </label>
          <select id="srSession" className="select" value={sessionStatus} onChange={(e) => setSessionStatus(e.target.value as "" | SessionStatus)}>
            <option value="">Todas</option>
            <option value="Scheduled">Agendadas</option>
            <option value="Completed">Concluídas</option>
            <option value="NoShow">Faltas</option>
            <option value="Cancelled">Canceladas</option>
          </select>
        </div>
        <div className="field">
          <label className="label" htmlFor="srPayment">
            Pagamento
          </label>
          <select id="srPayment" className="select" value={paymentStatus} onChange={(e) => setPaymentStatus(e.target.value as "" | PaymentStatus)}>
            <option value="">Todos</option>
            <option value="Pending">Pendentes</option>
            <option value="Paid">Pagos</option>
            <option value="Cancelled">Cancelados</option>
          </select>
        </div>
        {isManagement && (
          <div className="field span-2">
            <label className="label" htmlFor="srPsy">
              Psicólogo
            </label>
            <select id="srPsy" className="select" value={psychologistId} onChange={(e) => setPsychologistId(e.target.value)}>
              <option value="">Todos</option>
              {(psychologists.data ?? []).map((p) => (
                <option key={p.id} value={p.id}>
                  {p.fullName}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>
      <div>
        <button className="btn btn-primary" disabled={busy} onClick={() => download("/documents/sessions-report", { from, to, sessionStatus, paymentStatus, psychologistId })}>
          <FileDown /> Gerar PDF
        </button>
      </div>
    </div>
  );
}

function FeedbackReportCard() {
  const [patientId, setPatientId] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const { busy, download } = useDownload();

  return (
    <div className="card card-pad stack">
      <div className="row gap-2">
        <FileText size={18} className="muted" />
        <div className="strong">Relatório de feedback</div>
      </div>
      <div className="small muted">Notas de 0 a 10 do paciente nas sessões com você, com a média (RF015-B).</div>
      <div className="field">
        <label className="label" htmlFor="frPatient">
          Paciente <span className="req">*</span>
        </label>
        <PatientSelect id="frPatient" value={patientId} onChange={setPatientId} />
      </div>
      <div className="grid-form">
        <div className="field">
          <label className="label" htmlFor="frFrom">
            De
          </label>
          <input id="frFrom" className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="frTo">
            Até
          </label>
          <input id="frTo" className="input" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
      </div>
      <div>
        <button className="btn btn-primary" disabled={!patientId || busy} onClick={() => download("/documents/feedback-report", { patientId, from, to })}>
          <FileDown /> Gerar PDF
        </button>
      </div>
    </div>
  );
}

function Reports() {
  const { notify } = useStore();
  const [params] = useSearchParams();
  const [patientId, setPatientId] = useState(params.get("patientId") ?? "");
  const [editing, setEditing] = useState<PsychologicalReport | "new" | null>(null);
  const [removing, setRemoving] = useState<PsychologicalReport | null>(null);
  const { data, loading, error, reload } = useAsync(() => api.get<PsychologicalReport[]>("/psychological-reports", { patientId }), [patientId]);
  const { download } = useDownload();

  async function finalize(r: PsychologicalReport) {
    try {
      await api.post(`/psychological-reports/${r.id}/finalize`);
      notify("success", "Documento finalizado.");
      await reload();
    } catch (e) {
      notify("error", errorMessage(e));
    }
  }

  return (
    <div className="card">
      <div className="card-head">
        <div className="card-title">
          <ScrollText size={16} /> Laudos e relatórios psicológicos
        </div>
        <div className="row gap-2">
          <div style={{ minWidth: 220 }}>
            <PatientSelect allowAll value={patientId} onChange={setPatientId} />
          </div>
          <button className="btn btn-primary btn-sm" onClick={() => setEditing("new")}>
            <Plus /> Novo
          </button>
        </div>
      </div>
      {loading && !data ? (
        <Loading />
      ) : error ? (
        <ErrorState message={error} onRetry={reload} />
      ) : !data || data.length === 0 ? (
        <EmptyState icon={<ScrollText />} title="Nenhum documento" description="Crie um laudo ou relatório; ele fica como rascunho até ser finalizado." />
      ) : (
        <div className="rows">
          {data.map((r) => (
            <div className="list-row" key={r.id}>
              <FileText size={18} className="muted" />
              <div className="grow truncate">
                <div className="pname truncate">
                  {reportTemplateLabel[r.template]} — {r.patientName}
                </div>
                <div className="psub truncate">
                  Criado em {fmtDateShort(r.createdAt)}
                  {r.finalizedAt && ` · finalizado em ${fmtDateShort(r.finalizedAt)}`}
                  {r.purpose && ` · ${r.purpose}`}
                </div>
              </div>
              <span className={`badge ${reportStatusMeta[r.status].badge}`}>{reportStatusMeta[r.status].label}</span>
              <div className="cell-actions">
                <button className="btn btn-ghost btn-icon btn-sm" title="PDF" aria-label="Baixar PDF" onClick={() => download(`/psychological-reports/${r.id}/pdf`)}>
                  <FileDown />
                </button>
                {r.status === "Draft" && (
                  <>
                    <button className="btn btn-ghost btn-icon btn-sm" title="Editar" aria-label="Editar" onClick={() => setEditing(r)}>
                      <Pencil />
                    </button>
                    <button className="btn btn-ghost btn-icon btn-sm" title="Finalizar" aria-label="Finalizar" onClick={() => finalize(r)}>
                      <CheckCircle2 />
                    </button>
                    <button className="btn btn-ghost btn-icon btn-sm" title="Excluir" aria-label="Excluir" onClick={() => setRemoving(r)}>
                      <Trash2 />
                    </button>
                  </>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {editing && (
        <ReportForm
          report={editing === "new" ? undefined : editing}
          patientId={patientId}
          onClose={() => setEditing(null)}
          onSaved={() => {
            setEditing(null);
            void reload();
          }}
        />
      )}
      {removing && (
        <ConfirmDialog
          title="Excluir rascunho"
          danger
          confirmLabel="Excluir"
          message="O rascunho será excluído."
          onClose={() => setRemoving(null)}
          onConfirm={() =>
            void api
              .del(`/psychological-reports/${removing.id}`)
              .then(() => {
                notify("success", "Rascunho excluído.");
                return reload();
              })
              .catch((e) => notify("error", errorMessage(e)))
          }
        />
      )}
    </div>
  );
}

const SECTIONS: Array<{ key: "purpose" | "demand" | "procedure" | "analysis" | "conclusion"; label: string; hint: string }> = [
  { key: "purpose", label: "Finalidade", hint: "Para que e para quem o documento é emitido." },
  { key: "demand", label: "Descrição da demanda", hint: "Motivo do encaminhamento e queixa." },
  { key: "procedure", label: "Procedimento", hint: "Recursos e instrumentos usados, número de sessões." },
  { key: "analysis", label: "Análise", hint: "Exposição descritiva e fundamentada dos dados." },
  { key: "conclusion", label: "Conclusão", hint: "Síntese e, se for o caso, encaminhamentos." },
];

function ReportForm({
  report,
  patientId: initialPatient,
  onClose,
  onSaved,
}: {
  report?: PsychologicalReport;
  patientId: string;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { notify } = useStore();
  const [patientId, setPatientId] = useState(report?.patientId ?? initialPatient);
  const [template, setTemplate] = useState<ReportTemplate>(report?.template ?? "PsychologicalReport");
  const [values, setValues] = useState(() => ({
    purpose: report?.purpose ?? "",
    demand: report?.demand ?? "",
    procedure: report?.procedure ?? "",
    analysis: report?.analysis ?? "",
    conclusion: report?.conclusion ?? "",
  }));
  const [includeSessionSummary, setInclude] = useState(report?.includeSessionSummary ?? true);
  const [busy, setBusy] = useState(false);

  async function save() {
    const body = {
      template,
      purpose: values.purpose.trim() || null,
      demand: values.demand.trim() || null,
      procedure: values.procedure.trim() || null,
      analysis: values.analysis.trim() || null,
      conclusion: values.conclusion.trim() || null,
      includeSessionSummary,
    };
    setBusy(true);
    try {
      if (report) await api.put(`/psychological-reports/${report.id}`, body);
      else await api.post("/psychological-reports", { ...body, patientId });
      notify("success", "Rascunho salvo.");
      onSaved();
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title={report ? "Editar documento" : "Novo documento psicológico"}
      size="lg"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" disabled={!patientId || !values.purpose.trim() || busy} onClick={save}>
            {busy ? "Salvando…" : "Salvar rascunho"}
          </button>
        </>
      }
    >
      <div className="col gap-4">
        <div className="grid-form">
          {!report && (
            <div className="field">
              <label className="label" htmlFor="rpPatient">
                Paciente <span className="req">*</span>
              </label>
              <PatientSelect id="rpPatient" value={patientId} onChange={setPatientId} />
            </div>
          )}
          <div className="field">
            <label className="label" htmlFor="rpTemplate">
              Modelo
            </label>
            <select id="rpTemplate" className="select" value={template} onChange={(e) => setTemplate(e.target.value as ReportTemplate)}>
              {(Object.keys(reportTemplateLabel) as ReportTemplate[]).map((t) => (
                <option key={t} value={t}>
                  {reportTemplateLabel[t]}
                </option>
              ))}
            </select>
          </div>
        </div>
        {SECTIONS.map((s) => (
          <div className="field" key={s.key}>
            <label className="label" htmlFor={`rp-${s.key}`}>
              {s.label} {s.key === "purpose" && <span className="req">*</span>}
            </label>
            <textarea
              id={`rp-${s.key}`}
              className="textarea"
              placeholder={s.hint}
              maxLength={10000}
              value={values[s.key]}
              onChange={(e) => setValues((v) => ({ ...v, [s.key]: e.target.value }))}
            />
          </div>
        ))}
        <label className="row gap-2 small">
          <input type="checkbox" checked={includeSessionSummary} onChange={(e) => setInclude(e.target.checked)} /> Incluir no PDF o resumo das sessões
        </label>
        <div className="small muted">Para finalizar, todas as seções precisam estar preenchidas. Depois de finalizado, o documento não pode ser editado.</div>
      </div>
    </Modal>
  );
}
