// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary><see cref="IAgentSkillCatalogService.UploadSkillAsync"/> 的请求 DTO。</summary>
public sealed record class AgentSkillUploadRequest
{
    public required string SkillMdContent { get; init; }

    public IReadOnlyList<AgentSkillPackageEntry> PackageEntries { get; init; } = [];
}

public sealed record class AgentSkillPackageEntry(string RelativePath, Uri StorageUri);
