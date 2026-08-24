import type { ChatTokenUsage } from "../src/shared/network/contracts.js";

export interface OpenAIChatUsage {
    prompt_tokens?: number | null;
    completion_tokens?: number | null;
    total_tokens?: number | null;
    prompt_tokens_details?: {
        cached_tokens?: number | null;
    } | null;
    completion_tokens_details?: {
        reasoning_tokens?: number | null;
    } | null;
}

export interface AguiTokenUsage {
    inputTokenCount?: number | null;
    outputTokenCount?: number | null;
    totalTokenCount?: number | null;
    cachedInputTokenCount?: number | null;
    reasoningTokenCount?: number | null;
    additionalCounts?: Record<string, number> | null;
}

const addNullableCount = (
    current: number | null,
    incoming: number | null,
): number | null => {
    if (current === null) return incoming;
    if (incoming === null) return current;
    return current + incoming;
};

const toReportedCount = (value: unknown): number | null =>
    typeof value === "number" && Number.isFinite(value) ? value : null;

const mergeChatTokenUsage = (
    current: ChatTokenUsage | undefined,
    next: ChatTokenUsage,
): ChatTokenUsage => {
    if (!current) return next;

    const additionalCounts = {
        ...(current.additionalCounts ?? {}),
    };
    for (const [key, value] of Object.entries(next.additionalCounts ?? {})) {
        additionalCounts[key] = (additionalCounts[key] ?? 0) + value;
    }

    return {
        inputTokenCount: addNullableCount(
            current.inputTokenCount,
            next.inputTokenCount,
        ),
        outputTokenCount: addNullableCount(
            current.outputTokenCount,
            next.outputTokenCount,
        ),
        totalTokenCount: addNullableCount(
            current.totalTokenCount,
            next.totalTokenCount,
        ),
        cachedInputTokenCount: addNullableCount(
            current.cachedInputTokenCount,
            next.cachedInputTokenCount,
        ),
        reasoningTokenCount: addNullableCount(
            current.reasoningTokenCount,
            next.reasoningTokenCount,
        ),
        ...(Object.keys(additionalCounts).length > 0
            ? { additionalCounts }
            : {}),
    };
};

export const addChatTokenUsage = (
    current: ChatTokenUsage | undefined,
    incoming: OpenAIChatUsage | undefined,
): ChatTokenUsage | undefined => {
    if (!incoming) return current;

    const next: ChatTokenUsage = {
        inputTokenCount: toReportedCount(incoming.prompt_tokens),
        outputTokenCount: toReportedCount(incoming.completion_tokens),
        totalTokenCount: toReportedCount(incoming.total_tokens),
        cachedInputTokenCount: toReportedCount(
            incoming.prompt_tokens_details?.cached_tokens,
        ),
        reasoningTokenCount: toReportedCount(
            incoming.completion_tokens_details?.reasoning_tokens,
        ),
    };
    if (Object.values(next).every((value) => value === null)) return current;
    return mergeChatTokenUsage(current, next);
};

export const addAguiTokenUsage = (
    current: ChatTokenUsage | undefined,
    incoming: AguiTokenUsage | undefined,
): ChatTokenUsage | undefined => {
    if (!incoming) return current;

    const next: ChatTokenUsage = {
        inputTokenCount: toReportedCount(incoming.inputTokenCount),
        outputTokenCount: toReportedCount(incoming.outputTokenCount),
        totalTokenCount: toReportedCount(incoming.totalTokenCount),
        cachedInputTokenCount: toReportedCount(incoming.cachedInputTokenCount),
        reasoningTokenCount: toReportedCount(incoming.reasoningTokenCount),
        ...(incoming.additionalCounts
            ? { additionalCounts: { ...incoming.additionalCounts } }
            : {}),
    };
    if (
        Object.entries(next).every(
            ([key, value]) => key === "additionalCounts" || value === null,
        ) &&
        !next.additionalCounts
    ) {
        return current;
    }

    return mergeChatTokenUsage(current, next);
};
