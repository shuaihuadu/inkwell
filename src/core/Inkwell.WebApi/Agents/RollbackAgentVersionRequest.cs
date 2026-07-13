// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Agents;

/// <summary>
/// 回滚 Agent 版本请求。
/// </summary>
/// <param name="ChangeSummary">回滚变更摘要。</param>
public sealed record class RollbackAgentVersionRequest(string? ChangeSummary = null);
