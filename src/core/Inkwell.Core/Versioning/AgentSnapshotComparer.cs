// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 比较可编辑 Agent 定义与已发布快照中的运行配置。
/// </summary>
internal static class AgentSnapshotComparer
{
    /// <summary>
    /// 判断可编辑 Agent 定义是否与已发布快照一致。
    /// </summary>
    /// <param name="agent">可编辑 Agent 定义。</param>
    /// <param name="snapshot">已发布快照。</param>
    /// <returns>配置完全一致时返回 <see langword="true"/>。</returns>
    public static bool Matches(AgentDefinition agent, AgentSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(agent);
        ArgumentNullException.ThrowIfNull(snapshot);

        return agent.Name == snapshot.Name &&
            agent.AvatarUri == snapshot.AvatarUri &&
            agent.Description == snapshot.Description &&
            agent.Instructions == snapshot.Instructions &&
            JsonElement.DeepEquals(
                JsonSerializer.SerializeToElement(agent.BuildOptions),
                JsonSerializer.SerializeToElement(snapshot.BuildOptions));
    }
}