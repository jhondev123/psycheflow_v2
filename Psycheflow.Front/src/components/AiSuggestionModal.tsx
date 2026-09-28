import { useEffect, useState } from "react";
import { Copy, ShieldAlert, Sparkles } from "lucide-react";
import { Modal } from "@/components/ui/Modal";
import { Markdown } from "@/components/ui/Markdown";
import { ErrorState, Loading } from "@/components/ui/Feedback";
import { errorMessage } from "@/lib/api";
import { aiProviderLabel } from "@/lib/domain";
import { useStore } from "@/store/AppStore";
import type { AiSuggestion } from "@/types";

interface AiSuggestionModalProps {
  title: string;
  request: () => Promise<AiSuggestion>;
  onClose: () => void;
  /** Quando informado, mostra "Usar sugestão" (ex.: preencher as anotações da sessão). */
  onUse?: (text: string) => void;
}

/** Pede a sugestão ao abrir, mostra o resultado em Markdown com o aviso de revisão e permite copiar/usar. */
export function AiSuggestionModal({ title, request, onClose, onUse }: AiSuggestionModalProps) {
  const { notify } = useStore();
  const [result, setResult] = useState<AiSuggestion | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    let active = true;
    setResult(null);
    setError(null);
    request()
      .then((r) => active && setResult(r))
      .catch((e) => active && setError(errorMessage(e)));
    return () => {
      active = false;
    };
    // A requisição é feita uma vez por tentativa ("Tentar de novo" incrementa attempt).
  }, [attempt]);

  async function copy() {
    if (!result) return;
    await navigator.clipboard.writeText(result.suggestion);
    notify("success", "Sugestão copiada.");
  }

  return (
    <Modal
      title={title}
      size="lg"
      onClose={onClose}
      footer={
        <>
          <button className="btn btn-ghost" onClick={onClose}>
            Fechar
          </button>
          {result && (
            <button className="btn btn-subtle" onClick={copy}>
              <Copy size={15} /> Copiar
            </button>
          )}
          {result && onUse && (
            <button
              className="btn btn-primary"
              onClick={() => {
                onUse(result.suggestion);
                onClose();
              }}
            >
              <Sparkles size={15} /> Usar sugestão
            </button>
          )}
        </>
      }
    >
      {!result && !error && <Loading label="Gerando sugestão com IA… pode levar alguns segundos." />}
      {error && <ErrorState message={error} onRetry={() => setAttempt((a) => a + 1)} />}
      {result && (
        <div className="col gap-3">
          <div className="notice warning">
            <ShieldAlert />
            <span>{result.disclaimer}</span>
          </div>
          <div className="ai-box">
            <Markdown text={result.suggestion} />
          </div>
          <div className="tiny muted">
            {aiProviderLabel[result.provider]} · {result.model}
          </div>
        </div>
      )}
    </Modal>
  );
}
