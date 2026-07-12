// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Protocols;

internal sealed record class RoutingAgentSessionState(Guid AgentVersionId, JsonElement InnerState);