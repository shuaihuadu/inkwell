import { createContext, useContext, useMemo, useState, ReactNode } from 'react';
import { RoleInfo, RoleKey } from './types';

interface AppContextValue {
  role: RoleInfo;
  setRoleKey: (k: RoleKey) => void;
  /** 是否处于锁定态（NFR-003 / UI-002 全屏遮罩触发位） */
  locked: boolean;
  setLocked: (v: boolean) => void;
  /** 离线徽标（EX-001 演示） */
  offline: boolean;
  setOffline: (v: boolean) => void;
}

const ROLES: Record<RoleKey, RoleInfo> = {
  member: {
    key: 'member',
    username: 'alice',
    isSuper: false,
    label: 'Member（非 Owner，演示只读 / 共享视图）'
  },
  owner: {
    key: 'owner',
    username: 'owner-bob',
    isSuper: false,
    label: 'Member（Agent Owner，演示编辑视图）'
  },
  admin: {
    key: 'admin',
    username: 'sa-carol',
    isSuper: true,
    label: 'Admin（is_super=true，可见 UI-009）'
  }
};

const AppContext = createContext<AppContextValue | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const [roleKey, setRoleKey] = useState<RoleKey>('owner');
  const [locked, setLocked] = useState(false);
  const [offline, setOffline] = useState(false);

  const value = useMemo<AppContextValue>(
    () => ({
      role: ROLES[roleKey],
      setRoleKey,
      locked,
      setLocked,
      offline,
      setOffline
    }),
    [roleKey, locked, offline]
  );

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useAppContext(): AppContextValue {
  const v = useContext(AppContext);
  if (!v) throw new Error('useAppContext must be used inside AppProvider');
  return v;
}

export const ROLE_OPTIONS: RoleKey[] = ['member', 'owner', 'admin'];
export { ROLES };
