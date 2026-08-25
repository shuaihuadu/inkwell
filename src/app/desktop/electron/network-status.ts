import type {
    ConnectionSnapshot,
    GlobalApiError,
} from "../src/shared/network/contracts.js";

export const offlineFailureThreshold = 2;

export function nextConnectionSnapshot(
    succeeded: boolean,
    consecutiveFailures: number,
): { snapshot: ConnectionSnapshot; consecutiveFailures: number } {
    if (succeeded) {
        return { snapshot: { status: "online" }, consecutiveFailures: 0 };
    }

    const nextFailures = consecutiveFailures + 1;
    return {
        snapshot: {
            status:
                nextFailures >= offlineFailureThreshold
                    ? "offline"
                    : "reconnecting",
        },
        consecutiveFailures: nextFailures,
    };
}

export function toGlobalApiError(
    status: number,
    retryAfter: string | null,
    now = Date.now(),
): GlobalApiError | null {
    if (status === 429) {
        return {
            code: "rate-limited",
            retryAfterSeconds: parseRetryAfterSeconds(retryAfter, now),
        };
    }

    if (status >= 500) {
        return { code: "service-unavailable", retryAfterSeconds: null };
    }

    return null;
}

function parseRetryAfterSeconds(
    retryAfter: string | null,
    now: number,
): number | null {
    if (!retryAfter) return null;

    const seconds = Number(retryAfter);
    if (Number.isFinite(seconds) && seconds >= 0) return Math.ceil(seconds);

    const retryTime = Date.parse(retryAfter);
    if (Number.isNaN(retryTime)) return null;

    return Math.max(0, Math.ceil((retryTime - now) / 1_000));
}
