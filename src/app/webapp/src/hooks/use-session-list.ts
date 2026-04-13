import { useCallback, useEffect, useRef, useState } from "react";
import { API_BASE } from "../services/api";

export interface SessionInfo {
  id: string;
  agentId: string;
  title: string | null;
  messageCount: number;
  createdAt: string;
  updatedAt: string;
}

export interface UseSessionListReturn {
  sessions: SessionInfo[];
  loading: boolean;
  activeSessionId: string | null;
  setActiveSessionId: (id: string | null) => void;
  refresh: () => Promise<void>;
  deleteSession: (sessionId: string) => Promise<void>;
  renameSession: (sessionId: string, title: string) => Promise<void>;
}

export function useSessionList(
  agentId: string | undefined,
): UseSessionListReturn {
  const [sessions, setSessions] = useState<SessionInfo[]>([]);
  const [loading, setLoading] = useState(false);
  const [activeSessionId, setActiveSessionId] = useState<string | null>(null);
  const agentIdRef = useRef(agentId);

  const fetchSessions = useCallback(async (aid: string) => {
    try {
      setLoading(true);
      const res = await fetch(
        `${API_BASE}/api/sessions?agentId=${encodeURIComponent(aid)}`,
      );
      if (res.ok) {
        const data: SessionInfo[] = await res.json();
        setSessions(data);
      }
    } catch {
      // ignore
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    agentIdRef.current = agentId;
    if (agentId) {
      void fetchSessions(agentId);
    } else {
      setSessions([]);
    }
  }, [agentId, fetchSessions]);

  const refresh = useCallback(async () => {
    if (agentIdRef.current) {
      await fetchSessions(agentIdRef.current);
    }
  }, [fetchSessions]);

  const deleteSession = useCallback(
    async (sessionId: string) => {
      try {
        await fetch(`${API_BASE}/api/sessions/${sessionId}`, {
          method: "DELETE",
        });
        setSessions((prev) => prev.filter((s) => s.id !== sessionId));
        if (activeSessionId === sessionId) {
          setActiveSessionId(null);
        }
      } catch {
        // ignore
      }
    },
    [activeSessionId],
  );

  const renameSession = useCallback(
    async (sessionId: string, title: string) => {
      try {
        await fetch(`${API_BASE}/api/sessions/${sessionId}`, {
          method: "PATCH",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ title }),
        });
        setSessions((prev) =>
          prev.map((s) => (s.id === sessionId ? { ...s, title } : s)),
        );
      } catch {
        // ignore
      }
    },
    [],
  );

  return {
    sessions,
    loading,
    activeSessionId,
    setActiveSessionId,
    refresh,
    deleteSession,
    renameSession,
  };
}
