// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class AgentSessionStateRepository(InkwellDbContext db) : IAgentSessionStateRepository
{
    public async Task<AgentSessionState?> GetSessionStateOrDefault(Guid conversationId, CancellationToken ct = default)
    {
        AgentSessionStateEntity? entity = await db.Set<AgentSessionStateEntity>().AsNoTracking()
            .FirstOrDefaultAsync(state => state.ConversationId == conversationId, ct).ConfigureAwait(false);
        return entity?.ToModel();
    }

    public async Task AddSessionState(AgentSessionState state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        db.Set<AgentSessionStateEntity>().Add(state.ToEntity());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateSessionState(AgentSessionState state, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(state);
        string serializedState = state.SerializedState.GetRawText();
        int changed = await db.Set<AgentSessionStateEntity>()
            .Where(entity => entity.ConversationId == state.ConversationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(entity => entity.SerializedState, serializedState)
                .SetProperty(entity => entity.Revision, state.Revision)
                .SetProperty(entity => entity.LastRunId, state.LastRunId)
                .SetProperty(entity => entity.UpdatedTime, state.UpdatedTime), ct)
            .ConfigureAwait(false);
        if (changed == 0)
        {
            throw new KeyNotFoundException($"Agent session state not found: conversationId={state.ConversationId}");
        }
    }

    public async Task<bool> DeleteSessionStateByConversation(Guid conversationId, CancellationToken ct = default) =>
        await db.Set<AgentSessionStateEntity>().Where(state => state.ConversationId == conversationId).ExecuteDeleteAsync(ct).ConfigureAwait(false) == 1;
}
