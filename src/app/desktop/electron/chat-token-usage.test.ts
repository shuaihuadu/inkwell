import { describe, expect, test } from "vitest";
import { addAguiTokenUsage, addChatTokenUsage } from "./chat-token-usage.js";

describe("addChatTokenUsage", () => {
    test("maps one OpenAI usage object", () => {
        expect(
            addChatTokenUsage(undefined, {
                prompt_tokens: 10,
                completion_tokens: 20,
                total_tokens: 30,
                prompt_tokens_details: { cached_tokens: 4 },
                completion_tokens_details: { reasoning_tokens: 5 },
            }),
        ).toEqual({
            inputTokenCount: 10,
            outputTokenCount: 20,
            totalTokenCount: 30,
            cachedInputTokenCount: 4,
            reasoningTokenCount: 5,
        });
    });

    test("aggregates multiple tool-loop usage objects by category", () => {
        const first = addChatTokenUsage(undefined, {
            prompt_tokens: 2,
            completion_tokens: 3,
            total_tokens: 5,
        });

        expect(
            addChatTokenUsage(first, {
                prompt_tokens: 7,
                completion_tokens: 11,
                total_tokens: 18,
            }),
        ).toMatchObject({
            inputTokenCount: 9,
            outputTokenCount: 14,
            totalTokenCount: 23,
        });
    });

    test("preserves zero and missing category semantics", () => {
        expect(
            addChatTokenUsage(undefined, {
                prompt_tokens: 0,
            }),
        ).toMatchObject({
            inputTokenCount: 0,
            outputTokenCount: null,
            totalTokenCount: null,
        });
    });

    test("ignores absent and empty usage objects", () => {
        expect(addChatTokenUsage(undefined, undefined)).toBeUndefined();
        expect(addChatTokenUsage(undefined, {})).toBeUndefined();
    });
});

describe("addAguiTokenUsage", () => {
    test("aggregates AG-UI usage events including additional counts", () => {
        const first = addAguiTokenUsage(undefined, {
            inputTokenCount: 3,
            outputTokenCount: 5,
            totalTokenCount: 8,
            additionalCounts: { acceptedPredictionTokenCount: 2 },
        });

        expect(
            addAguiTokenUsage(first, {
                inputTokenCount: 7,
                outputTokenCount: 15,
                totalTokenCount: 22,
                additionalCounts: { acceptedPredictionTokenCount: 4 },
            }),
        ).toEqual({
            inputTokenCount: 10,
            outputTokenCount: 20,
            totalTokenCount: 30,
            cachedInputTokenCount: null,
            reasoningTokenCount: null,
            additionalCounts: { acceptedPredictionTokenCount: 6 },
        });
    });

    test("ignores absent and empty AG-UI usage events", () => {
        expect(addAguiTokenUsage(undefined, undefined)).toBeUndefined();
        expect(addAguiTokenUsage(undefined, {})).toBeUndefined();
    });
});
