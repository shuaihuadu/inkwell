// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 表示 Skill 可编辑内容的更新请求。
/// </summary>
/// <param name="Name">Skill 名称。</param>
/// <param name="Description">Skill 描述。</param>
/// <param name="Content">SKILL.md Markdown 内容。</param>
/// <param name="RowVersion">乐观并发版本。</param>
public sealed record class AgentSkillUpdateRequest(
    string Name,
    string Description,
    string Content,
    byte[] RowVersion);