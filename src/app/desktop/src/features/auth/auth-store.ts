import { create } from "zustand";
import type { AuthSnapshot } from "../../shared/network/contracts";

interface AuthState extends AuthSnapshot {
    setSnapshot: (snapshot: AuthSnapshot) => void;
}

export const useAuthStore = create<AuthState>((set) => ({
    status: "restoring",
    identity: null,
    setSnapshot: (snapshot) => set(snapshot),
}));
