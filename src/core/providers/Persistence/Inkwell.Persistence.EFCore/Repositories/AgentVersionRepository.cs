// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentVersionRepository(InkwellDbContext db) : IAgentVersionRepository
{
    public async Task<AgentVersion> AddVersionAsync(AgentVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        AgentVersionEntity entity = version.ToEntity();
        db.Set<AgentVersionEntity>().Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentVersion> UpdateVersionAsync(AgentVersion version, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(version);

        AgentVersionEntity entity = version.ToEntity();
        db.Set<AgentVersionEntity>().Update(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return entity.ToModel();
    }

    public async Task<AgentVersion> GetVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        AgentVersionEntity? entity = await db.Set<AgentVersionEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(version => version.Id == versionId, cancellationToken)
            .ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"Agent version not found: id={versionId}");
    }

    public async Task<IReadOnlyList<AgentVersion>> ListVersionsByAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        List<AgentVersionEntity> entities = await db.Set<AgentVersionEntity>()
            .AsNoTracking()
            .Where(version => version.AgentId == agentId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. entities.Select(AgentVersionMappingExtensions.ToModel)];
    }

    public async Task<IReadOnlyDictionary<Guid, AgentVersion>> FindVersionsByIdsAsync(IReadOnlyList<Guid> versionIds, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(versionIds);

        if (versionIds.Count == 0)
        {
            return new Dictionary<Guid, AgentVersion>();
        }

        List<AgentVersionEntity> entities = await db.Set<AgentVersionEntity>()
            .AsNoTracking()
            .Where(version => versionIds.Contains(version.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return entities.Select(AgentVersionMappingExtensions.ToModel).ToDictionary(version => version.Id);
    }
}