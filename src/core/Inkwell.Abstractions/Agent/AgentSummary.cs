// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>列表卡片投影 DTO；不含 Instructions / ModelParameters / ToolBindings 明细。</summary>
public sealed record class AgentSummary(
    Guid Id,
    string Name,
    Uri? AvatarUri,
    string? DescriptionExcerpt,
    Guid OwnerUserId,
    bool IsShared,
    int CurrentVersion,
    DateTimeOffset UpdatedTime);
