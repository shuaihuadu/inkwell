import { describe, expect, it } from "vitest";
import {
    nextConnectionSnapshot,
    offlineFailureThreshold,
    toGlobalApiError,
} from "./network-status.js";

describe("network status", () => {
    it("moves through reconnecting to offline and recovers online", () => {
        let failures = 0;

        const reconnecting = nextConnectionSnapshot(false, failures);
        failures = reconnecting.consecutiveFailures;
        const offline = nextConnectionSnapshot(false, failures);
        const recovered = nextConnectionSnapshot(
            true,
            offline.consecutiveFailures,
        );

        expect(offlineFailureThreshold).toBe(2);
        expect(reconnecting.snapshot.status).toBe("reconnecting");
        expect(offline.snapshot.status).toBe("offline");
        expect(recovered).toEqual({
            snapshot: { status: "online" },
            consecutiveFailures: 0,
        });
    });

    it("maps rate limits and server failures without exposing response bodies", () => {
        const now = Date.parse("2026-08-25T10:00:00Z");

        expect(toGlobalApiError(429, "12", now)).toEqual({
            code: "rate-limited",
            retryAfterSeconds: 12,
        });
        expect(
            toGlobalApiError(429, "Mon, 25 Aug 2026 10:00:05 GMT", now),
        ).toEqual({ code: "rate-limited", retryAfterSeconds: 5 });
        expect(toGlobalApiError(503, null, now)).toEqual({
            code: "service-unavailable",
            retryAfterSeconds: null,
        });
        expect(toGlobalApiError(404, null, now)).toBeNull();
    });
});
