// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Agents;

/// <summary>
/// Agent 头像上传响应。
/// </summary>
/// <param name="AvatarUri">已上传头像的持久 URI。</param>
public sealed record class AgentAvatarUploadResponse(Uri AvatarUri);
