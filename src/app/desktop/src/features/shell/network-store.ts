import { create } from "zustand";
import type {
    ConnectionSnapshot,
    GlobalApiError,
} from "../../shared/network/contracts";

interface NetworkState extends ConnectionSnapshot {
    globalError: GlobalApiError | null;
    setConnection: (snapshot: ConnectionSnapshot) => void;
    setGlobalError: (error: GlobalApiError | null) => void;
    clearGlobalError: () => void;
}

export const useNetworkStore = create<NetworkState>((set) => ({
    status: "reconnecting",
    globalError: null,
    setConnection: (snapshot) => set(snapshot),
    setGlobalError: (globalError) => set({ globalError }),
    clearGlobalError: () => set({ globalError: null }),
}));
