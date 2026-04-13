import { useCallback, useEffect, useRef, useState } from "react";
import { API_BASE } from "../services/api";

export interface UseApiListOptions<TItem> {
  endpoint: string;
  initialData?: TItem[];
  onSuccess?: (items: TItem[]) => void;
  onError?: (error: Error) => void;
}

export interface UseApiListReturn<TItem> {
  items: TItem[];
  loading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
}

/**
 * 通用列表加载 Hook：统一处理加载态、错误态和刷新逻辑
 */
export function useApiList<TItem>({
  endpoint,
  initialData = [],
  onSuccess,
  onError,
}: UseApiListOptions<TItem>): UseApiListReturn<TItem> {
  const [items, setItems] = useState<TItem[]>(initialData);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onSuccessRef = useRef(onSuccess);
  const onErrorRef = useRef(onError);

  useEffect(() => {
    onSuccessRef.current = onSuccess;
  }, [onSuccess]);

  useEffect(() => {
    onErrorRef.current = onError;
  }, [onError]);

  const loadList = useCallback(
    async (signal?: AbortSignal) => {
      let aborted = false;

      if (signal) {
        signal.addEventListener("abort", () => {
          aborted = true;
        });
      }

      try {
        setLoading(true);
        setError(null);

        const response = await fetch(`${API_BASE}${endpoint}`, {
          signal,
        });

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }

        const data = (await response.json()) as TItem[];
        if (!aborted) {
          setItems(data);
          onSuccessRef.current?.(data);
        }
      } catch (err) {
        if ((err as Error).name !== "AbortError" && !aborted) {
          const errorMessage = (err as Error).message;
          setError(errorMessage);
          onErrorRef.current?.(err as Error);
        }
      } finally {
        if (!aborted) {
          setLoading(false);
        }
      }
    },
    [endpoint],
  );

  const refresh = useCallback(async () => {
    try {
      await loadList();
    } catch (err) {
      // loadList 已统一处理错误
    }
  }, [loadList]);

  useEffect(() => {
    const controller = new AbortController();

    void loadList(controller.signal);

    return () => {
      controller.abort();
    };
  }, [loadList]);

  return { items, loading, error, refresh };
}
