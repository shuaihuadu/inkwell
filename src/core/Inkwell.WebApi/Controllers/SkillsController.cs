// Copyright (c) ShuaiHua Du. All rights reserved.

using System.IO.Compression;

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供 Skill 浏览、上传和维护 API。
/// </summary>
[Route("api/skills")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class SkillsController(
    IAgentSkillCatalogService skillCatalogService,
    IFileStorageProvider fileStorageProvider) : InkwellControllerBase
{
    private const int MaxArchiveEntries = 256;
    private const long MaxArchiveSizeBytes = 32 * 1024 * 1024;
    private const long MaxMarkdownSizeBytes = 2 * 1024 * 1024;
    private const string SkillContainer = "skills";

    /// <summary>获取全部 Skill。</summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>Skill 列表。</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AgentSkillDefinition>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AgentSkillDefinition>>> ListAsync(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<AgentSkillDefinition> skills = await skillCatalogService
            .ListAvailableSkillsAsync(cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(skills);
    }

    /// <summary>上传 SKILL.md 或 Skill zip 包。</summary>
    /// <param name="file">待上传文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建的 Skill。</returns>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxArchiveSizeBytes)]
    [ProducesResponseType<AgentSkillDefinition>(StatusCodes.Status201Created)]
    public async Task<ActionResult<AgentSkillDefinition>> UploadAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length is <= 0 or > MaxArchiveSizeBytes)
        {
            throw new ArgumentException("Skill upload must not be empty or exceed 32 MB.", nameof(file));
        }

        List<string> uploadedKeys = [];

        try
        {
            AgentSkillUploadRequest request = string.Equals(
                Path.GetExtension(file.FileName),
                ".zip",
                StringComparison.OrdinalIgnoreCase)
                ? await this.ReadZipAsync(file, uploadedKeys, cancellationToken).ConfigureAwait(false)
                : await ReadSkillMarkdownAsync(file, cancellationToken).ConfigureAwait(false);
            AgentSkillDefinition skill = await skillCatalogService
                .UploadSkillAsync(request, this.GetRequiredUserId(), cancellationToken)
                .ConfigureAwait(false);

            return this.Created($"/api/skills/{skill.Id}", skill);
        }
        catch
        {
            await this.DeleteUploadedFilesAsync(uploadedKeys, CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>更新指定 Skill。</summary>
    /// <param name="skillId">Skill 标识。</param>
    /// <param name="request">更新请求。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新后的 Skill。</returns>
    [HttpPut("{skillId:guid}")]
    [ProducesResponseType<AgentSkillDefinition>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AgentSkillDefinition>> UpdateAsync(
        Guid skillId,
        AgentSkillUpdateRequest request,
        CancellationToken cancellationToken)
    {
        AgentSkillDefinition skill = await skillCatalogService
            .UpdateSkillAsync(
                skillId,
                request,
                this.GetRequiredUserId(),
                this.GetRequiredIsSuper(),
                cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(skill);
    }

    /// <summary>删除指定 Skill。</summary>
    /// <param name="skillId">Skill 标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>无响应正文。</returns>
    [HttpDelete("{skillId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(
        Guid skillId,
        CancellationToken cancellationToken)
    {
        _ = await skillCatalogService
            .DeleteSkillAsync(
                skillId,
                this.GetRequiredUserId(),
                this.GetRequiredIsSuper(),
                cancellationToken)
            .ConfigureAwait(false);

        return this.NoContent();
    }

    private static async Task<AgentSkillUploadRequest> ReadSkillMarkdownAsync(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(Path.GetExtension(file.FileName), ".md", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Skill upload must be a .md or .zip file.", nameof(file));
        }

        if (file.Length > MaxMarkdownSizeBytes)
        {
            throw new ArgumentException("SKILL.md must not exceed 2 MB.", nameof(file));
        }

        await using Stream stream = file.OpenReadStream();
        using StreamReader reader = new(stream);
        string content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        return new AgentSkillUploadRequest { SkillMdContent = content };
    }

    private async Task<AgentSkillUploadRequest> ReadZipAsync(
        IFormFile file,
        List<string> uploadedKeys,
        CancellationToken cancellationToken)
    {
        await using Stream stream = file.OpenReadStream();
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, leaveOpen: false);
        if (archive.Entries.Count is 0 or > MaxArchiveEntries)
        {
            throw new ArgumentException("Skill package must contain between 1 and 256 entries.", nameof(file));
        }

        ZipArchiveEntry[] markdownEntries = [.. archive.Entries.Where(entry =>
            string.Equals(Path.GetFileName(entry.FullName), "SKILL.md", StringComparison.OrdinalIgnoreCase))];
        ZipArchiveEntry? skillMarkdown = markdownEntries.SingleOrDefault();

        if (skillMarkdown is null)
        {
            throw new ArgumentException("Skill package must contain exactly one SKILL.md.", nameof(file));
        }
        if (skillMarkdown.Length > MaxMarkdownSizeBytes)
        {
            throw new ArgumentException("SKILL.md must not exceed 2 MB.", nameof(file));
        }

        string root = skillMarkdown.FullName[..^"SKILL.md".Length];
        List<(ZipArchiveEntry Entry, string Path)> packageFiles = [];
        long totalSize = skillMarkdown.Length;

        foreach (ZipArchiveEntry entry in archive.Entries.Where(item => item.Length > 0 && !ReferenceEquals(item, skillMarkdown)))
        {
            if (!entry.FullName.StartsWith(root, StringComparison.Ordinal))
            {
                throw new ArgumentException("Every Skill package file must be inside the SKILL.md folder.", nameof(file));
            }

            string normalizedPath = NormalizeEntryPath(entry.FullName[root.Length..]);
            ValidatePackageFolder(normalizedPath);
            totalSize = checked(totalSize + entry.Length);

            if (totalSize > MaxArchiveSizeBytes)
            {
                throw new ArgumentException("Expanded Skill package must not exceed 32 MB.", nameof(file));
            }

            packageFiles.Add((entry, normalizedPath));
        }

        string content;
        await using (Stream markdownStream = skillMarkdown.Open())
        using (StreamReader reader = new(markdownStream))
        {
            content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        }

        Guid packageId = Guid.CreateVersion7();
        List<AgentSkillPackageEntry> packageEntries = [];

        foreach ((ZipArchiveEntry entry, string normalizedPath) in packageFiles)
        {
            string key = $"{packageId:N}/{normalizedPath}";
            await using Stream entryStream = entry.Open();
            _ = await fileStorageProvider.UploadAsync(
                SkillContainer,
                key,
                entryStream,
                new FileMetadata("application/octet-stream"),
                cancellationToken).ConfigureAwait(false);
            uploadedKeys.Add(key);
            packageEntries.Add(new AgentSkillPackageEntry(
                normalizedPath,
                new Uri($"inkwell://{SkillContainer}/{key}")));
        }

        return new AgentSkillUploadRequest
        {
            SkillMdContent = content,
            PackageEntries = packageEntries,
        };
    }

    private static string NormalizeEntryPath(string relativePath)
    {
        string normalized = relativePath.Replace('\\', '/').TrimStart('/');

        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Split('/').Any(segment => segment is "" or "." or ".."))
        {
            throw new ArgumentException($"Invalid Skill package path: '{relativePath}'.");
        }

        return normalized;
    }

    private static void ValidatePackageFolder(string normalizedPath)
    {
        if (!normalizedPath.StartsWith("references/", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.StartsWith("assets/", StringComparison.OrdinalIgnoreCase) &&
            !normalizedPath.StartsWith("scripts/", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unrecognized Skill package entry: '{normalizedPath}'.");
        }
    }

    private async Task DeleteUploadedFilesAsync(
        IReadOnlyList<string> uploadedKeys,
        CancellationToken cancellationToken)
    {
        foreach (string key in uploadedKeys)
        {
            await fileStorageProvider
                .DeleteAsync(SkillContainer, key, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}