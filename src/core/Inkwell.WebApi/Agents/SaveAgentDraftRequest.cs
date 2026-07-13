// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Agents;

/// <summary>
/// 保存 Agent 草稿请求。
/// </summary>
/// <param name="Snapshot">完整 Agent 快照。</param>
/// <param name="ChangeSummary">变更摘要。</param>
public sealed record class SaveAgentDraftRequest(AgentSnapshot Snapshot, string? ChangeSummary = null);
