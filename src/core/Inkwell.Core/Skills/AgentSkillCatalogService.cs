// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Text.RegularExpressions;

namespace Inkwell;

/// <summary><see cref="IAgentSkillCatalogService"/> 唯一实现；查询、上传与维护 Skill。</summary>
internal sealed partial class AgentSkillCatalogService(IPersistenceProvider persistence) : IAgentSkillCatalogService
{
    private readonly IAgentSkillRepository _skills = persistence.GetRepository<IAgentSkillRepository>();

    public async Task<IReadOnlyList<AgentSkillDefinition>> ListAvailableSkillsAsync(CancellationToken ct = default)
    {
        List<AgentSkillDefinition> all = await PaginationHelper.CollectAllAsync(
            (pagination, innerCt) => this._skills.ListSkills(pagination, SortOrder.ByCreatedAtDesc, innerCt),
            ct).ConfigureAwait(false);

        return all;
    }

    public async Task<AgentSkillDefinition> GetSkillAsync(Guid skillId, CancellationToken ct = default) =>
        await this._skills.GetSkill(skillId, ct).ConfigureAwait(false);

    public async Task<AgentSkillDefinition> UploadSkillAsync(
        AgentSkillUploadRequest request,
        Guid ownerUserId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("OwnerUserId must not be empty.", nameof(ownerUserId));
        }

        if (!TryParseFrontmatter(request.SkillMdContent, out string? name, out string? description, out string? contentMarkdown))
        {
            throw new ArgumentException("SKILL.md frontmatter is missing or invalid.", nameof(request));
        }

        ValidatePackageStructure(
            request.PackageEntries,
            out IReadOnlyList<Uri>? referenceUris,
            out IReadOnlyList<Uri>? assetUris,
            out IReadOnlyList<Uri>? scriptUris);

        DateTimeOffset now = DateTimeOffset.UtcNow;
        AgentSkillDefinition skill = new()
        {
            Id = Guid.CreateVersion7(),
            OwnerUserId = ownerUserId,
            Name = name,
            Description = description,
            Content = contentMarkdown,
            ReferenceFileUris = referenceUris,
            AssetFileUris = assetUris,
            ScriptFileUris = scriptUris,
            CreatedTime = now,
            UpdatedTime = now,
        };

        return await persistence.ExecuteInTransactionAsync(
            innerCt => this._skills.AddSkill(skill, innerCt),
            ct).ConfigureAwait(false);
    }

    public async Task<AgentSkillDefinition> UpdateSkillAsync(
        Guid skillId,
        AgentSkillUpdateRequest request,
        Guid actorUserId,
        bool actorIsSuper,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateEditableFields(request.Name, request.Description, request.Content);

        AgentSkillDefinition skill = await this._skills.GetSkill(skillId, ct).ConfigureAwait(false);
        ValidateManagementPermission(skill, actorUserId, actorIsSuper);
        AgentSkillDefinition updated = skill with
        {
            Name = request.Name,
            Description = request.Description,
            Content = request.Content,
            RowVersion = request.RowVersion,
            UpdatedTime = DateTimeOffset.UtcNow,
        };

        return await persistence.ExecuteInTransactionAsync(
            innerCt => this._skills.UpdateSkill(updated, innerCt),
            ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteSkillAsync(
        Guid skillId,
        Guid actorUserId,
        bool actorIsSuper,
        CancellationToken ct = default) =>
        await persistence.ExecuteInTransactionAsync(async innerCt =>
        {
            AgentSkillDefinition skill = await this._skills.GetSkill(skillId, innerCt).ConfigureAwait(false);
            ValidateManagementPermission(skill, actorUserId, actorIsSuper);
            return await this._skills.DeleteSkill(skillId, innerCt).ConfigureAwait(false);
        }, ct).ConfigureAwait(false);

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

    private static void ValidatePackageStructure(
        IReadOnlyList<AgentSkillPackageEntry> entries,
        out IReadOnlyList<Uri> referenceUris,
        out IReadOnlyList<Uri> assetUris,
        out IReadOnlyList<Uri> scriptUris)
    {
        List<Uri> references = [];
        List<Uri> assets = [];
        List<Uri> scripts = [];

        foreach (AgentSkillPackageEntry entry in entries)
        {
            string normalized = entry.RelativePath.Replace('\\', '/').Trim();

            if (normalized.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase))
            {
                scripts.Add(entry.StorageUri);
            }
            else if (normalized.StartsWith("references/", StringComparison.OrdinalIgnoreCase))
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
        scriptUris = scripts;
    }

    private static void ValidateManagementPermission(
        AgentSkillDefinition skill,
        Guid actorUserId,
        bool actorIsSuper)
    {
        if (!actorIsSuper && skill.OwnerUserId != actorUserId)
        {
            throw new UnauthorizedAccessException(
                $"User '{actorUserId}' cannot manage skill '{skill.Id}'.");
        }
    }

    private static void ValidateEditableFields(string name, string description, string content)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80)
        {
            throw new ArgumentException("Name must be between 1 and 80 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(description) || description.Length > 240)
        {
            throw new ArgumentException(
                "Description must be between 1 and 240 characters.",
                nameof(description));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content must not be empty.", nameof(content));
        }
    }

    [GeneratedRegex(@"\A---\s*\n(?<frontmatter>.*?)\n---\s*\n", RegexOptions.Singleline)]
    private static partial Regex FrontmatterRegex();
}
