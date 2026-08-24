import { describe, expect, test } from "vitest";
import type {
    ChatMessage,
    ChatRunSnapshot,
} from "../../shared/network/contracts";
import { applyChatRunSnapshotToMessages } from "./chat-run-messages";

const messages: ChatMessage[] = [
    { role: "user", content: "question" },
    { role: "assistant", content: "" },
];

const completedSnapshot: ChatRunSnapshot = {
    requestId: "request-1",
    status: "completed",
    content: "answer",
    skillActivities: [],
    usage: {
        inputTokenCount: 10,
        outputTokenCount: 20,
        totalTokenCount: 30,
        cachedInputTokenCount: null,
        reasoningTokenCount: null,
    },
};

describe("applyChatRunSnapshotToMessages", () => {
    test("publishes usage only for a completed snapshot", () => {
        const result = applyChatRunSnapshotToMessages(
            messages,
            completedSnapshot,
        );

        expect(result.at(-1)).toMatchObject({
            role: "assistant",
            content: "answer",
            runStatus: "completed",
            usage: completedSnapshot.usage,
        });
    });

    test.each(["running", "stopped", "failed"] as const)(
        "discards usage for a %s snapshot",
        (status) => {
            const result = applyChatRunSnapshotToMessages(messages, {
                ...completedSnapshot,
                status,
            });

            expect(result.at(-1)?.usage).toBeUndefined();
        },
    );
});
