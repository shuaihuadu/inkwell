import { describe, expect, test } from "vitest";
import { toToolRunActivity } from "./chat-tool-activity.js";

describe("toToolRunActivity", () => {
    test("maps a generic function call", () => {
        expect(
            toToolRunActivity({
                id: "call-current-time",
                function: {
                    name: "get_current_datetime",
                    arguments: '{"timeZoneId":"Asia/Shanghai"}',
                },
            }),
        ).toEqual({
            callId: "call-current-time",
            type: "tool-called",
            skillName: "get_current_datetime",
            functionName: "get_current_datetime",
            argumentsJson: '{"timeZoneId":"Asia/Shanghai"}',
            status: "loading",
        });
    });

    test("preserves specialized Skill activity mapping", () => {
        expect(
            toToolRunActivity({
                id: "call-load-skill",
                function: {
                    name: "load_skill",
                    arguments: '{"skillName":"code-review"}',
                },
            }),
        ).toMatchObject({
            type: "skill-loaded",
            skillName: "code-review",
            functionName: "load_skill",
        });
    });

    test("waits for fragmented arguments to form valid JSON", () => {
        expect(
            toToolRunActivity({
                id: "call-current-time",
                function: {
                    name: "get_current_datetime",
                    arguments: '{"timeZoneId":"Asia/',
                },
            }),
        ).toBeNull();
    });
});
