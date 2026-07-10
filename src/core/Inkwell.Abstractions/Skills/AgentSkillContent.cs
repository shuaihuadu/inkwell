// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentSkillContentResolver.ResolveAsync"/> 单条解析结果 DTO。</summary>
public sealed record class AgentSkillContent(
    Guid SkillId,
    string Name,
    string Description,
    string ContentMarkdown,
    IReadOnlyList<Uri> ReferenceFileUris,
    IReadOnlyList<Uri> AssetFileUris);
