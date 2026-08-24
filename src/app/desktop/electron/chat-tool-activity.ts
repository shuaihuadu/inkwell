import type { SkillRunActivity } from "../src/shared/network/contracts.js";

export interface ChatCompletionToolCall {
    index?: number;
    id?: string;
    function?: {
        name?: string;
        arguments?: string;
    };
}

export const toToolRunActivity = (
    toolCall: ChatCompletionToolCall,
): SkillRunActivity | null => {
    const functionName = toolCall.function?.name;
    if (!functionName) return null;

    let argumentsValue: Record<string, unknown> = {};
    try {
        argumentsValue = JSON.parse(
            toolCall.function?.arguments ?? "{}",
        ) as Record<string, unknown>;
    } catch {
        return null;
    }

    const isSkillTool =
        functionName === "load_skill" ||
        functionName === "read_skill_resource" ||
        functionName === "run_skill_script";
    const skillNameValue = isSkillTool
        ? (argumentsValue.skillName ?? argumentsValue.skill_name)
        : functionName;
    if (typeof skillNameValue !== "string" || !skillNameValue.trim()) {
        return null;
    }

    const targetValue =
        functionName === "read_skill_resource"
            ? (argumentsValue.resourceName ?? argumentsValue.resource_name)
            : functionName === "run_skill_script"
              ? (argumentsValue.scriptName ?? argumentsValue.script_name)
              : undefined;
    const type =
        functionName === "load_skill"
            ? "skill-loaded"
            : functionName === "read_skill_resource"
              ? "skill-resource-read"
              : functionName === "run_skill_script"
                ? "skill-script-run"
                : "tool-called";

    return {
        callId:
            toolCall.id ??
            `${functionName}:${skillNameValue}:${typeof targetValue === "string" ? targetValue : ""}`,
        type,
        skillName: skillNameValue,
        functionName,
        argumentsJson: toolCall.function?.arguments ?? "{}",
        status: "loading",
        ...(typeof targetValue === "string" && targetValue.trim()
            ? { targetName: targetValue }
            : {}),
    };
};
