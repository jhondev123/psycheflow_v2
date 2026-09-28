import { useEffect, useState } from "react";
import { Save, ShieldCheck, Sparkles } from "lucide-react";
import { useStore } from "@/store/AppStore";
import { api, ApiError, errorMessage } from "@/lib/api";
import { useAsync } from "@/lib/useAsync";
import type { AiProvider, AiSettings as AiSettingsData } from "@/types";
import { PageHeader } from "@/components/layout/PageHeader";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { aiProviderLabel } from "@/lib/domain";
import { fmtDateTime } from "@/lib/format";
import "@/styles/pages.css";

const ALL_PROVIDERS: AiProvider[] = ["Claude", "OpenAi", "Gemini"];

/** RF021 / RN-67: a clínica habilita a IA, escolhe o provedor e o que pode ser enviado, e aceita os termos. */
export function AiSettings() {
  const { isManagement, notify } = useStore();
  const { data, loading, error, reload, setData } = useAsync(() => api.get<AiSettingsData>("/ai/settings"), []);

  const [isEnabled, setEnabled] = useState(false);
  const [provider, setProvider] = useState<AiProvider>("Claude");
  const [shareSessionNotes, setNotes] = useState(true);
  const [shareFeedbacks, setFeedbacks] = useState(true);
  const [shareMedicalRecords, setRecords] = useState(false);
  const [acceptTerms, setAccept] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!data) return;
    setEnabled(data.isEnabled);
    setProvider(data.availableProviders.includes(data.provider) || data.availableProviders.length === 0 ? data.provider : data.availableProviders[0]);
    if (data.isEnabled || data.consentAcceptedAt === null) {
      setNotes(data.isEnabled ? data.shareSessionNotes : true);
      setFeedbacks(data.isEnabled ? data.shareFeedbacks : true);
      setRecords(data.shareMedicalRecords);
    }
    setAccept(false);
  }, [data]);

  if (loading && !data) return <Loading />;
  if (error || !data) return <ErrorState message={error ?? "Não foi possível carregar."} onRetry={reload} />;

  const noProviders = data.availableProviders.length === 0;
  const nothingShared = !shareSessionNotes && !shareFeedbacks && !shareMedicalRecords;
  const invalid = isEnabled && (noProviders || nothingShared || !acceptTerms);

  async function save(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    try {
      const saved = await api.put<AiSettingsData>("/ai/settings", { isEnabled, provider, shareSessionNotes, shareFeedbacks, shareMedicalRecords, acceptTerms });
      setData(saved);
      notify("success", isEnabled ? "Assistente de IA habilitado." : "Assistente de IA desabilitado.");
    } catch (err) {
      notify("error", err instanceof ApiError ? err.message : errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader title="Assistente de IA" subtitle="Sugestões para anotações de sessão, análise do paciente e próximos passos." />

      <form className="card card-pad stack" style={{ maxWidth: 680 }} onSubmit={save} noValidate>
        <div className="row gap-2 wrap">
          <Sparkles size={18} className="muted" />
          <span className="strong">Situação:</span>
          <span className={`badge ${data.isEnabled ? "badge-success" : ""}`}>{data.isEnabled ? `Habilitado · ${aiProviderLabel[data.provider]}` : "Desabilitado"}</span>
          {data.consentAcceptedAt && <span className="small muted">Termos aceitos em {fmtDateTime(data.consentAcceptedAt)}</span>}
        </div>

        <div className="notice">
          <ShieldCheck />
          <div>
            Antes de sair do sistema, os dados são <b>pseudonimizados</b>: o paciente vira “[paciente]”, outras pessoas citadas viram
            “[pessoa]” e CPF, e-mail, telefone e endereço são removidos. Só vão os tipos de dado marcados abaixo e somente os do
            psicólogo que pediu a sugestão. As sugestões não substituem a avaliação profissional.
          </div>
        </div>

        {!isManagement ? (
          <div className="muted small">Somente administradores e gestores podem alterar esta configuração.</div>
        ) : (
          <>
            <label className="row gap-2">
              <span className="switch">
                <input type="checkbox" checked={isEnabled} onChange={(e) => setEnabled(e.target.checked)} />
                <span className="track" />
              </span>
              <span className="strong">Habilitar o assistente de IA nesta clínica</span>
            </label>

            <div className="field">
              <label className="label" htmlFor="aiProvider">
                Provedor
              </label>
              <select id="aiProvider" className="select" value={provider} onChange={(e) => setProvider(e.target.value as AiProvider)}>
                {ALL_PROVIDERS.map((p) => (
                  <option key={p} value={p} disabled={!data.availableProviders.includes(p)}>
                    {aiProviderLabel[p]}
                    {!data.availableProviders.includes(p) ? " — sem chave no servidor" : ""}
                  </option>
                ))}
              </select>
              {noProviders && (
                <span className="field-error">
                  Nenhum provedor configurado no servidor. Defina ANTHROPIC_API_KEY, OPENAI_API_KEY ou GEMINI_API_KEY no .env da API.
                </span>
              )}
            </div>

            <div className="field">
              <span className="label">Dados que podem ser enviados</span>
              <label className="row gap-2 small">
                <input type="checkbox" checked={shareSessionNotes} onChange={(e) => setNotes(e.target.checked)} /> Anotações das sessões
              </label>
              <label className="row gap-2 small">
                <input type="checkbox" checked={shareFeedbacks} onChange={(e) => setFeedbacks(e.target.checked)} /> Feedbacks dos pacientes (notas e comentários)
              </label>
              <label className="row gap-2 small">
                <input type="checkbox" checked={shareMedicalRecords} onChange={(e) => setRecords(e.target.checked)} /> Registros de prontuário
              </label>
              {isEnabled && nothingShared && <span className="field-error">Marque ao menos um tipo de dado.</span>}
            </div>

            {isEnabled && (
              <label className="row gap-2 small" style={{ alignItems: "flex-start" }}>
                <input type="checkbox" checked={acceptTerms} onChange={(e) => setAccept(e.target.checked)} />
                <span>
                  Declaro que a clínica autoriza o envio desses dados, pseudonimizados, ao provedor escolhido para gerar sugestões, e que os
                  psicólogos revisarão todo conteúdo antes de usá-lo. O aceite fica registrado com data e usuário.
                </span>
              </label>
            )}

            <div>
              <button className="btn btn-primary" type="submit" disabled={invalid || busy}>
                <Save /> {busy ? "Salvando…" : "Salvar"}
              </button>
            </div>
          </>
        )}
      </form>
    </>
  );
}
