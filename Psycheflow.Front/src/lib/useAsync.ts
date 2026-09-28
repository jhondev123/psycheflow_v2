import { useCallback, useEffect, useRef, useState, type DependencyList } from "react";
import { errorMessage } from "@/lib/api";

export interface AsyncState<T> {
  data: T | undefined;
  loading: boolean;
  error: string | null;
  /** Busca de novo (ex.: depois de salvar). */
  reload: () => Promise<void>;
  /** Troca o dado localmente sem ir ao servidor. */
  setData: (value: T | undefined) => void;
}

/**
 * Carrega dados da API quando as dependências mudam. Respostas de buscas antigas são descartadas,
 * então digitar rápido num filtro nunca mostra um resultado fora de ordem.
 */
export function useAsync<T>(load: () => Promise<T>, deps: DependencyList, enabled = true): AsyncState<T> {
  const [data, setData] = useState<T | undefined>(undefined);
  const [loading, setLoading] = useState(enabled);
  const [error, setError] = useState<string | null>(null);
  const request = useRef(0);

  const run = useCallback(load, deps);

  const reload = useCallback(async () => {
    if (!enabled) return;
    const id = ++request.current;
    setLoading(true);
    setError(null);
    try {
      const result = await run();
      if (id === request.current) setData(result);
    } catch (e) {
      if (id === request.current) setError(errorMessage(e));
    } finally {
      if (id === request.current) setLoading(false);
    }
  }, [run, enabled]);

  useEffect(() => {
    void reload();
  }, [reload]);

  return { data, loading, error, reload, setData };
}
