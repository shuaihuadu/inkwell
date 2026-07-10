
namespace Inkwell;

/// <summary>
/// 会话业务 Model；按 (AgentId, OwnerUserId) 二元组归属一个团队成员与某个 Agent 之间的独立会话历史。
/// <see cref="OwnerUserId"/> 语义 = 会话参与用户，非 <c>AgentDefinition.OwnerUserId</c>（Agent 创建者）。
/// </summary>
public sealed record class AgentConversation : IHasTimestamps, IHasOwner, IHasRowVersion
{
    public required Guid Id { get; init; }

    public required Guid AgentId { get; init; }

    public required Guid OwnerUserId { get; init; }

    public string? Title { get; init; }

    public required DateTimeOffset CreatedTime { get; init; }

    public required DateTimeOffset UpdatedTime { get; init; }

    public byte[] RowVersion { get; init; } = [];
}
