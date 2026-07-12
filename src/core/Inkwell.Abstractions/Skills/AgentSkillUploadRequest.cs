// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentSkillCatalogService.UploadSkillAsync"/> 的请求 DTO。</summary>
public sealed record class AgentSkillUploadRequest
{
    /// <summary>
    /// 获取 SKILL.md 文件内容。
    /// </summary>
    public required string SkillMdContent { get; init; }

    /// <summary>
    /// 获取 Skill 包中的文件条目。
    /// </summary>
    public IReadOnlyList<AgentSkillPackageEntry> PackageEntries { get; init; } = [];
}

/// <summary>
/// 表示 Agent Skill 包中的文件条目。
/// </summary>
/// <param name="RelativePath">文件在 Skill 包中的相对路径。</param>
/// <param name="StorageUri">文件存储 URI。</param>
public sealed record class AgentSkillPackageEntry(string RelativePath, Uri StorageUri);
