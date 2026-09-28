import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router-dom";
import { Download, FileText, FolderLock, History, Paperclip, Pencil, Plus, Search, Trash2, Upload } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { AccessEntry, Attachment, MedicalRecord, MedicalRecordListItem, PagedResponse } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { ErrorState, Loading, Pager } from "@/components/ui/Feedback";
import { ConfirmDialog, Modal } from "@/components/ui/Modal";
import { Markdown } from "@/components/ui/Markdown";
import { PatientSelect } from "@/components/PatientSelect";
import { accessActionLabel } from "@/lib/domain";
import { fmtBytes, fmtDateShort, fmtDateTime } from "@/lib/format";
import "@/styles/pages.css";

const MAX_BYTES = 10 * 1024 * 1024;
const ACCEPT = "application/pdf,image/png,image/jpeg";

/** RF019/RF020: prontuário por paciente, visível só ao psicólogo autor (sigilo). */
export function MedicalRecords() {
  const [params] = useSearchParams();
  const [patientId, setPatientId] = useState(params.get("patientId") ?? "");
  const [query, setQuery] = useState("");
  const [search, setSearch] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);
  const [creating, setCreating] = useState(false);
  const [openId, setOpenId] = useState<string | null>(null);

  useEffect(() => {
    const id = setTimeout(() => {
      setSearch(query.trim());
      setPage(1);
    }, 300);
    return () => clearTimeout(id);
  }, [query]);

  const { data, loading, error, reload } = useAsync(
    () => api.get<PagedResponse<MedicalRecordListItem>>("/medical-records", { patientId, search, from, to, page, pageSize: 20 }),
    [patientId, search, from, to, page],
  );

  return (
    <>
      <PageHeader
        title="Prontuários"
        subtitle="Registros sigilosos: só você, como autor(a), tem acesso — e cada acesso fica registrado."
        actions={
          <button className="btn btn-primary" onClick={() => setCreating(true)}>
            <Plus /> Novo registro
          </button>
        }
      />

      <div className="filters">
        <div className="field" style={{ minWidth: 220 }}>
          <label className="label" htmlFor="mPatient">
            Paciente
          </label>
          <PatientSelect
            id="mPatient"
            allowAll
            value={patientId}
            onChange={(v) => {
              setPatientId(v);
              setPage(1);
            }}
          />
        </div>
        <div className="field">
          <label className="label" htmlFor="mFrom">
            De
          </label>
          <input id="mFrom" className="input" type="date" value={from} onChange={(e) => setFrom(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="mTo">
            Até
          </label>
          <input id="mTo" className="input" type="date" value={to} onChange={(e) => setTo(e.target.value)} />
        </div>
        <div className="search grow">
          <Search />
          <input className="input" placeholder="Palavra-chave no título ou no conteúdo…" value={query} onChange={(e) => setQuery(e.target.value)} />
        </div>
      </div>

      <div className="card">
        {loading && !data ? (
          <Loading />
        ) : error ? (
          <ErrorState message={error} onRetry={reload} />
        ) : !data || data.items.length === 0 ? (
          <EmptyState icon={<FolderLock />} title="Nenhum registro encontrado" description="Crie o primeiro registro de prontuário de um paciente." />
        ) : (
          <>
            <div className="rows">
              {data.items.map((r) => (
                <div className="list-row" key={r.id} onClick={() => setOpenId(r.id)}>
                  <FileText size={18} className="muted" />
                  <div className="grow truncate">
                    <div className="pname truncate">{r.title}</div>
                    <div className="psub truncate">
                      {r.patientName} · {fmtDateShort(r.createdAt)} · {r.excerpt}
                    </div>
                  </div>
                  {r.attachmentsCount > 0 && (
                    <span className="badge">
                      <Paperclip size={12} /> {r.attachmentsCount}
                    </span>
                  )}
                </div>
              ))}
            </div>
            <Pager page={data.page} totalPages={data.totalPages} totalCount={data.totalCount} onPage={setPage} />
          </>
        )}
      </div>

      {creating && (
        <RecordForm
          patientId={patientId}
          onClose={() => setCreating(false)}
          onSaved={(record) => {
            setCreating(false);
            void reload();
            setOpenId(record.id);
          }}
        />
      )}
      {openId && <RecordModal id={openId} onClose={() => setOpenId(null)} onChanged={reload} />}
    </>
  );
}

function RecordForm({
  record,
  patientId: initialPatient = "",
  onClose,
  onSaved,
}: {
  record?: MedicalRecord;
  patientId?: string;
  onClose: () => void;
  onSaved: (record: MedicalRecord) => void;
}) {
  const { notify } = useStore();
  const [patientId, setPatientId] = useState(record?.patientId ?? initialPatient);
  const [title, setTitle] = useState(record?.title ?? "");
  const [content, setContent] = useState(record?.content ?? "");
  const [busy, setBusy] = useState(false);
  const invalid = !patientId || !title.trim() || !content.trim();

  async function save() {
    setBusy(true);
    try {
      const saved = record
        ? await api.put<MedicalRecord>(`/medical-records/${record.id}`, { title: title.trim(), content: content.trim() })
        : await api.post<MedicalRecord>("/medical-records", { patientId, title: title.trim(), content: content.trim() });
      notify("success", record ? "Registro atualizado." : "Registro criado.");
      onSaved(saved);
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setBusy(false);
    }
  }

  return (
    <Modal
      title={record ? "Editar registro" : "Novo registro de prontuário"}
      size="lg"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" disabled={invalid || busy} onClick={save}>
            {busy ? "Salvando…" : "Salvar"}
          </button>
        </>
      }
    >
      <div className="col gap-4">
        {!record && (
          <div className="field">
            <label className="label" htmlFor="rPatient">
              Paciente <span className="req">*</span>
            </label>
            <PatientSelect id="rPatient" value={patientId} onChange={setPatientId} />
          </div>
        )}
        <div className="field">
          <label className="label" htmlFor="rTitle">
            Título <span className="req">*</span>
          </label>
          <input id="rTitle" className="input" maxLength={200} placeholder="Anamnese, evolução, avaliação…" value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div className="field">
          <label className="label" htmlFor="rContent">
            Conteúdo (Markdown) <span className="req">*</span>
          </label>
          <textarea id="rContent" className="textarea" style={{ minHeight: 220 }} maxLength={50000} value={content} onChange={(e) => setContent(e.target.value)} />
        </div>
      </div>
    </Modal>
  );
}

function RecordModal({ id, onClose, onChanged }: { id: string; onClose: () => void; onChanged: () => void }) {
  const { notify } = useStore();
  const record = useAsync(() => api.get<MedicalRecord>(`/medical-records/${id}`), [id]);
  const [tab, setTab] = useState<"content" | "log">("content");
  const log = useAsync(() => api.get<AccessEntry[]>(`/medical-records/${id}/access-log`), [id, tab], tab === "log");
  const [editing, setEditing] = useState(false);
  const [removing, setRemoving] = useState(false);
  const [uploading, setUploading] = useState(false);
  const fileInput = useRef<HTMLInputElement>(null);

  async function upload(file: File) {
    if (file.size > MAX_BYTES) return notify("error", "O arquivo passa de 10 MB.");
    setUploading(true);
    try {
      const attachment = await api.upload<Attachment>(`/medical-records/${id}/attachments`, file);
      notify("success", "Anexo enviado.");
      // Atualiza localmente: reler o registro contaria como um novo acesso na trilha.
      if (record.data) record.setData({ ...record.data, attachments: [...record.data.attachments, attachment] });
      onChanged();
    } catch (e) {
      notify("error", errorMessage(e));
    } finally {
      setUploading(false);
      if (fileInput.current) fileInput.current.value = "";
    }
  }

  async function removeAttachment(attachmentId: string) {
    try {
      await api.del(`/medical-records/${id}/attachments/${attachmentId}`);
      notify("success", "Anexo removido.");
      if (record.data) record.setData({ ...record.data, attachments: record.data.attachments.filter((a) => a.id !== attachmentId) });
      onChanged();
    } catch (e) {
      notify("error", errorMessage(e));
    }
  }

  const r = record.data;

  return (
    <>
      <Modal
        title={r ? r.title : "Prontuário"}
        size="lg"
        onClose={onClose}
        footer={
          r && (
            <>
              <button className="btn btn-ghost btn-icon" title="Excluir registro" aria-label="Excluir registro" onClick={() => setRemoving(true)}>
                <Trash2 />
              </button>
              <div className="grow" />
              <button className="btn btn-subtle" disabled={uploading} onClick={() => fileInput.current?.click()}>
                <Upload size={15} /> {uploading ? "Enviando…" : "Anexar arquivo"}
              </button>
              <button className="btn btn-primary" onClick={() => setEditing(true)}>
                <Pencil size={15} /> Editar
              </button>
            </>
          )
        }
      >
        {record.loading && !r ? (
          <Loading />
        ) : record.error ? (
          <ErrorState message={record.error} />
        ) : (
          r && (
            <div className="col gap-4">
              <div className="row between wrap gap-2">
                <div className="small muted">
                  {r.patientName} · criado em {fmtDateTime(r.createdAt)}
                  {r.updatedAt && ` · editado em ${fmtDateTime(r.updatedAt)}`}
                </div>
                <div className="cal-views">
                  <button className={`cal-view-btn ${tab === "content" ? "active" : ""}`} onClick={() => setTab("content")}>
                    Registro
                  </button>
                  <button className={`cal-view-btn ${tab === "log" ? "active" : ""}`} onClick={() => setTab("log")}>
                    <History size={13} /> Acessos
                  </button>
                </div>
              </div>

              {tab === "content" ? (
                <>
                  <Markdown text={r.content} />
                  <div>
                    <div className="section-title">Anexos</div>
                    {r.attachments.length === 0 ? (
                      <div className="muted small">Nenhum anexo (PDF, JPG ou PNG até 10 MB).</div>
                    ) : (
                      r.attachments.map((a) => (
                        <div className="attach-row" key={a.id}>
                          <Paperclip size={16} className="muted" />
                          <div className="grow truncate">
                            <div className="truncate small strong">{a.fileName}</div>
                            <div className="tiny muted">
                              {fmtBytes(a.sizeBytes)} · {fmtDateShort(a.createdAt)}
                            </div>
                          </div>
                          <button
                            className="btn btn-ghost btn-icon btn-sm"
                            aria-label="Baixar anexo"
                            title="Baixar"
                            onClick={() => api.download(`/medical-records/${r.id}/attachments/${a.id}`, undefined, a.fileName).catch((e) => notify("error", errorMessage(e)))}
                          >
                            <Download />
                          </button>
                          <button
                            className="btn btn-ghost btn-icon btn-sm"
                            aria-label="Remover anexo"
                            title="Remover"
                            onClick={() => removeAttachment(a.id)}
                          >
                            <Trash2 />
                          </button>
                        </div>
                      ))
                    )}
                  </div>
                </>
              ) : log.loading && !log.data ? (
                <Loading />
              ) : (
                <div className="table-scroll">
                  <table className="table">
                    <thead>
                      <tr>
                        <th>Quando</th>
                        <th>Quem</th>
                        <th>Ação</th>
                      </tr>
                    </thead>
                    <tbody>
                      {(log.data ?? []).map((e, i) => (
                        <tr key={i}>
                          <td className="small">{fmtDateTime(e.at)}</td>
                          <td className="small">{e.userName}</td>
                          <td>
                            <span className={`badge ${e.action === "Denied" ? "badge-danger" : ""}`}>{accessActionLabel[e.action]}</span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
              <input ref={fileInput} type="file" accept={ACCEPT} hidden onChange={(e) => e.target.files?.[0] && upload(e.target.files[0])} />
            </div>
          )
        )}
      </Modal>

      {editing && r && (
        <RecordForm
          record={r}
          onClose={() => setEditing(false)}
          onSaved={(saved) => {
            record.setData(saved);
            setEditing(false);
            onChanged();
          }}
        />
      )}
      {removing && r && (
        <ConfirmDialog
          title="Excluir registro"
          danger
          confirmLabel="Excluir"
          message="O registro sai da lista, mas é mantido no banco pela guarda obrigatória do prontuário."
          onClose={() => setRemoving(false)}
          onConfirm={() =>
            void api
              .del(`/medical-records/${r.id}`)
              .then(() => {
                notify("success", "Registro excluído.");
                onChanged();
                onClose();
              })
              .catch((e) => notify("error", errorMessage(e)))
          }
        />
      )}
    </>
  );
}
