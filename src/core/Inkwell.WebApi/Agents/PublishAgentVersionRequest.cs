// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Agents;

/// <summary>
/// 发布 Agent 版本请求。
/// </summary>
/// <param name="ChangeSummary">变更摘要。</param>
public sealed record class PublishAgentVersionRequest(string? ChangeSummary = null);
