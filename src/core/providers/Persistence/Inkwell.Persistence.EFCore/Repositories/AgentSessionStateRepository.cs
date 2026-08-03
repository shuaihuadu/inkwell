// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentSessionStateRepository(InkwellDbContext db) : IAgentSessionStateRepository
{
    public async Task<AgentSessionState> AddSessionState(AgentSessionState sessionState, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sessionState);
        AgentSessionStateEntity entity = sessionState.ToEntity();
        _ = await db.Set<AgentSessionStateEntity>().AddAsync(entity, ct).ConfigureAwait(false);
        _ = await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return entity.ToModel();
    }

    public async Task<AgentSessionState?> FindSessionStateByConversation(Guid conversationId, CancellationToken ct = default)
    {
        AgentSessionStateEntity? entity = await db.Set<AgentSessionStateEntity>().AsNoTracking()
            .FirstOrDefaultAsync(state => state.ConversationId == conversationId, ct).ConfigureAwait(false);
        return entity?.ToModel();
    }

    public async Task<bool> UpdateSessionState(Guid conversationId, string sessionState, DateTimeOffset updatedTime, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionState);
        int changed = await db.Set<AgentSessionStateEntity>()
            .Where(state => state.ConversationId == conversationId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(state => state.SessionState, sessionState)
                    .SetProperty(state => state.UpdatedTime, updatedTime),
                ct)
            .ConfigureAwait(false);
        return changed == 1;
    }

    public async Task<bool> DeleteSessionState(Guid conversationId, CancellationToken ct = default)
    {
        int deleted = await db.Set<AgentSessionStateEntity>()
            .Where(state => state.ConversationId == conversationId)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);
        return deleted == 1;
    }
}
