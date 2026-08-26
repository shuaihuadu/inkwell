
export const applicationEntry = "out/main/index.js";
export const toolsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000101",
        name: "get_current_datetime",
        description: "获取当前日期时间，可选指定 IANA 或 Windows 时区标识符。",
        parametersJsonSchema: JSON.stringify({
            type: "object",
            required: [],
            properties: {},
            additionalProperties: false,
        }),
        createdTime: "2026-07-17T08:00:00Z",
        updatedTime: "2026-07-18T08:42:00Z",
    },
]);
export const skillsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000201",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        name: "合同审查规范",
        description: "按团队法务标准识别合同风险并输出分级建议。",
        content: [
            "# 合同审查规范",
            "",
            "先识别合同类型，再按高、中、低三级输出风险。每项风险必须引用原文。",
            "",
            "## 审查步骤",
            "",
            "1. 确认合同主体、标的、金额、履行期限与争议解决条款。",
            "2. 识别权利义务不对等、责任上限缺失和单方变更等风险。",
            "3. 对每项风险引用原文，并给出可直接替换的修改建议。",
            "",
            "## 输出要求",
            "",
            "不得脱离合同原文推断未约定的事实；信息不足时应明确标记待确认事项。",
        ].join("\n"),
        referenceFileUris: ["inkwell://skills/references/rule.md"],
        assetFileUris: ["inkwell://skills/assets/template.docx"],
        scriptFileUris: ["inkwell://skills/scripts/check.ps1"],
        createdTime: "2026-07-17T08:00:00Z",
        updatedTime: "2026-07-18T09:20:00Z",
    },
    {
        id: "0198a96d-19e4-7000-8000-000000000202",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
        name: "研发周报",
        description: "将工作记录整理为统一的研发周报格式。",
        content: "# 研发周报",
        referenceFileUris: [],
        assetFileUris: [],
        scriptFileUris: [],
        createdTime: "2026-07-16T08:00:00Z",
        updatedTime: "2026-07-17T02:08:00Z",
    },
]);
export const myAgentsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000301",
        name: "研发助手",
        avatarUri: null,
        descriptionExcerpt: "帮助团队分析代码并整理研发任务。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        isShared: true,
        latestPublishedVersionNumber: 3,
        hasUnpublishedChanges: true,
        updatedTime: "2026-07-18T12:00:00Z",
    },
    {
        id: "0198a96d-19e4-7000-8000-000000000302",
        name: "产品草稿",
        avatarUri: null,
        descriptionExcerpt: "尚未发布的产品分析 Agent。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
        isShared: false,
        latestPublishedVersionNumber: 0,
        hasUnpublishedChanges: false,
        updatedTime: "2026-07-18T13:00:00Z",
    },
]);
export const sharedAgentsResponse = JSON.stringify([
    {
        id: "0198a96d-19e4-7000-8000-000000000303",
        name: "合同审查助手",
        avatarUri: null,
        descriptionExcerpt: "识别合同风险并输出分级建议。",
        ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
        isShared: true,
        latestPublishedVersionNumber: 2,
        hasUnpublishedChanges: false,
        updatedTime: "2026-07-17T10:00:00Z",
    },
]);
export const editableAgent = {
    id: "0198a96d-19e4-7000-8000-000000000304",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
    name: "发布助手",
    avatarUri: null,
    description: "整理发布内容。",
    instructions: "输出简洁的发布说明。",
    buildOptions: {
        modelOptions: {
            modelId: "gpt-5.4",
            temperature: 0.7,
            topP: null,
            maxTokens: null,
        },
        chatHistoryOptions: {
            maxMessages: 40,
            reducerType: null,
            maxMessagesToRetrieve: null,
        },
        toolBindings: [],
        skills: [],
    },
    currentPublishedVersionId: null,
    latestPublishedVersionNumber: 0,
    isShared: false,
    sharedRevokedByAdminTime: null,
    createdTime: "2026-07-19T00:00:00Z",
    updatedTime: "2026-07-19T00:00:00Z",
};
export const editableAgentWithoutBindingCollections = {
    ...editableAgent,
    buildOptions: {
        modelOptions: editableAgent.buildOptions.modelOptions,
        chatHistoryOptions: editableAgent.buildOptions.chatHistoryOptions,
    },
};
export const publishedAgent = {
    ...editableAgent,
    id: "0198a96d-19e4-7000-8000-000000000301",
    name: "研发助手",
    currentPublishedVersionId: "0198a96d-19e4-7000-8000-000000000305",
    latestPublishedVersionNumber: 3,
};
export const sharedAgent = {
    ...editableAgent,
    id: "0198a96d-19e4-7000-8000-000000000303",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000002",
    name: "合同审查助手",
    description: "识别合同风险并输出分级建议。",
    currentPublishedVersionId: "0198a96d-19e4-7000-8000-000000000306",
    latestPublishedVersionNumber: 2,
    isShared: true,
};
export const clonedAgent = {
    ...sharedAgent,
    id: "0198a96d-19e4-7000-8000-000000000307",
    ownerUserId: "0198a96d-19e4-7000-8000-000000000001",
    name: "合同审查助手（副本）",
    currentPublishedVersionId: null,
    latestPublishedVersionNumber: 0,
    isShared: false,
};
