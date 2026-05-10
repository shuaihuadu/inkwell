import { useSearchParams } from 'react-router-dom';

/**
 * 读 ?state=xxx，便于 PrototypeReviewer 评审时通过深链直接演示每个状态
 * （依 OQ-021 closed：列表加载 = 骨架；空 = 文案 + 主动作；不使用插画）
 */
export function useStateQuery<T extends string>(
  defaultState: T,
  allowed: readonly T[]
): [T, (s: T) => void] {
  const [params, setParams] = useSearchParams();
  const raw = params.get('state') as T | null;
  const current: T = raw && (allowed as readonly string[]).includes(raw) ? raw : defaultState;
  const set = (s: T) => {
    const next = new URLSearchParams(params);
    if (s === defaultState) next.delete('state');
    else next.set('state', s);
    setParams(next, { replace: true });
  };
  return [current, set];
}
