// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.Agents.AI;

namespace Inkwell.WebApi.Protocols;

internal sealed class RoutingAgentSession(Guid agentVersionId, AgentSession innerSession) : AgentSession(innerSession.StateBag)
{
    public Guid AgentVersionId { get; } = agentVersionId;

    public AgentSession InnerSession { get; set; } = innerSession;
}