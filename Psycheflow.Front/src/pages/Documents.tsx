import { useState } from "react";
import { Download, FileText } from "lucide-react";
import { useStore } from "@/store/AppStore";
import type { DocumentTemplate } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { EmptyState } from "@/components/ui/EmptyState";
import { Modal } from "@/components/ui/Modal";
import { capitalize } from "@/lib/format";
import "@/styles/pages.css";

export function Documents() {
  const { db } = useStore();
  const [active, setActive] = useState<DocumentTemplate | null>(null);

  return (
    <>
      <PageHeader title="Documentos" subtitle="Modelos para gerar atestados e declarações." />

      {db.documents.length === 0 ? (
        <div className="card">
          <EmptyState icon={<FileText />} title="Nenhum modelo disponível" />
        </div>
      ) : (
        <div className="doc-grid">
          {db.documents.map((doc) => (
            <div className="card doc-card" key={doc.id}>
              <div className="doc-ic">
                <FileText />
              </div>
              <div>
                <div className="strong">{doc.name}</div>
                <div className="small muted" style={{ marginTop: 2 }}>
                  {doc.description}
                </div>
              </div>
              <div className="doc-fields">
                {doc.fields.map((f) => (
                  <span className="badge" key={f.id}>
                    {capitalize(f.name)}
                  </span>
                ))}
              </div>
              <button className="btn btn-subtle btn-sm" onClick={() => setActive(doc)}>
                <FileText size={15} /> Gerar documento
              </button>
            </div>
          ))}
        </div>
      )}

      {active && <GenerateModal template={active} onClose={() => setActive(null)} />}
    </>
  );
}

function GenerateModal({ template, onClose }: { template: DocumentTemplate; onClose: () => void }) {
  const { db, notify } = useStore();
  const today = new Date().toLocaleDateString("pt-BR");

  const [values, setValues] = useState<Record<string, string>>(() => {
    const init: Record<string, string> = {};
    for (const f of template.fields) {
      init[f.name] = f.defaultValue ?? (f.name === "data" ? today : "");
    }
    return init;
  });
  const [patientId, setPatientId] = useState("");

  const required = template.fields.filter((f) => f.isRequired);
  const missing = required.some((f) => !values[f.name]?.trim());

  function applyPatient(id: string) {
    setPatientId(id);
    const p = db.patients.find((x) => x.id === id);
    if (p && "paciente" in values) {
      setValues((v) => ({ ...v, paciente: p.name }));
    }
  }

  function generate() {
    const lines = [
      template.name.toUpperCase(),
      "",
      ...template.fields
        .slice()
        .sort((a, b) => a.order - b.order)
        .map((f) => `${capitalize(f.name)}: ${values[f.name] || "—"}`),
      "",
      `Emitido em ${today} — Psycheflow`,
    ];
    const blob = new Blob([lines.join("\n")], { type: "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `${template.templateName.replace(/\.\w+$/, "")}.txt`;
    a.click();
    URL.revokeObjectURL(url);
    notify("success", "Documento gerado.");
    onClose();
  }

  return (
    <Modal
      title={template.name}
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Cancelar
          </button>
          <button className="btn btn-primary" onClick={generate} disabled={missing}>
            <Download /> Gerar e baixar
          </button>
        </>
      }
    >
      <div className="col gap-4">
        {"paciente" in values && (
          <div className="field">
            <label className="label">Selecionar paciente</label>
            <select className="select" value={patientId} onChange={(e) => applyPatient(e.target.value)}>
              <option value="">Preencher manualmente…</option>
              {[...db.patients]
                .sort((a, b) => a.name.localeCompare(b.name, "pt-BR"))
                .map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
            </select>
          </div>
        )}

        {template.fields
          .slice()
          .sort((a, b) => a.order - b.order)
          .map((f) => (
            <div className="field" key={f.id}>
              <label className="label">
                {capitalize(f.name)}
                {f.isRequired && <span className="req">*</span>}
              </label>
              <input
                className="input"
                value={values[f.name] ?? ""}
                onChange={(e) => setValues((v) => ({ ...v, [f.name]: e.target.value }))}
              />
            </div>
          ))}
      </div>
    </Modal>
  );
}
