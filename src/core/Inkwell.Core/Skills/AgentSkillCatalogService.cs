// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.RegularExpressions;
using Inkwell.Persistence;

namespace Inkwell;

/// <summary><see cref="IAgentSkillCatalogService"/> 唯一实现；只读查询 + 上传解析与持久化。</summary>
internal sealed partial class AgentSkillCatalogService(IAgentSkillRepository skills, IPersistenceProvider persistence) : IAgentSkillCatalogService
{
    public async Task<IReadOnlyList<AgentSkillDefinition>> ListAvailableSkillsAsync(CancellationToken ct = default)
    {
        List<AgentSkillDefinition> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => skills.ListSkills(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return all;
    }

    public async Task<AgentSkillDefinition> GetSkillAsync(Guid skillId, CancellationToken ct = default) =>
        await skills.GetSkill(skillId, ct).ConfigureAwait(false);

    public async Task<AgentSkillDefinition> UploadSkillAsync(AgentSkillUploadRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!TryParseFrontmatter(request.SkillMdContent, out string? name, out string? description, out string? contentMarkdown))
        {
            throw new ArgumentException("SKILL.md frontmatter is missing or invalid.", nameof(request));
        }

        ValidatePackageStructure(request.PackageEntries, out IReadOnlyList<Uri>? referenceUris, out IReadOnlyList<Uri>? assetUris);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentSkillDefinition skill = new AgentSkillDefinition
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Description = description,
            Content = contentMarkdown,
            ReferenceFileUris = referenceUris,
            AssetFileUris = assetUris,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(
            innerCt => skills.AddSkill(skill, innerCt),
            ct).ConfigureAwait(false);
    }

    private static bool TryParseFrontmatter(string skillMdContent, out string name, out string description, out string contentMarkdown)
    {
        name = string.Empty;
        description = string.Empty;
        contentMarkdown = string.Empty;

        Match match = FrontmatterRegex().Match(skillMdContent);

        if (!match.Success)
        {
            return false;
        }

        string? parsedName = null;
        string? parsedDescription = null;

        foreach (string line in match.Groups["frontmatter"].Value.Split('\n'))
        {
            string trimmed = line.Trim();
            int separatorIndex = trimmed.IndexOf(':');

            if (separatorIndex <= 0)
            {
                continue;
            }

            string key = trimmed[..separatorIndex].Trim();
            string value = trimmed[(separatorIndex + 1)..].Trim();

            if (string.Equals(key, "name", StringComparison.OrdinalIgnoreCase))
            {
                parsedName = value;
            }
            else if (string.Equals(key, "description", StringComparison.OrdinalIgnoreCase))
            {
                parsedDescription = value;
            }
        }

        if (string.IsNullOrEmpty(parsedName) || string.IsNullOrEmpty(parsedDescription))
        {
            return false;
        }

        name = parsedName;
        description = parsedDescription;
        contentMarkdown = skillMdContent[match.Length..].Trim();

        return true;
    }

    private static void ValidatePackageStructure(IReadOnlyList<AgentSkillPackageEntry> entries, out IReadOnlyList<Uri> referenceUris, out IReadOnlyList<Uri> assetUris)
    {
        List<Uri> references = [];
        List<Uri> assets = [];

        foreach (AgentSkillPackageEntry entry in entries)
        {
            string normalized = entry.RelativePath.Replace('\\', '/').Trim();

            if (normalized.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Skill package contains disallowed 'scripts/' entry: '{entry.RelativePath}'.");
            }

            if (normalized.StartsWith("references/", StringComparison.OrdinalIgnoreCase))
            {
                references.Add(entry.StorageUri);
            }
            else if (normalized.StartsWith("assets/", StringComparison.OrdinalIgnoreCase))
            {
                assets.Add(entry.StorageUri);
            }
            else
            {
                throw new ArgumentException($"Unrecognized skill package entry: '{entry.RelativePath}'.");
            }
        }

        referenceUris = references;
        assetUris = assets;
    }

    [GeneratedRegex(@"\A---\s*\n(?<frontmatter>.*?)\n---\s*\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}
