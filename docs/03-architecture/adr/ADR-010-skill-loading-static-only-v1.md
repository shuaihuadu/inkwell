---
id: ADR-010-skill-loading-static-only-v1
stage: H2
status: reviewed
authors:
  - name: H2-ArchitectAdvisor
    role: agent
reviewers: [ Inkwell ]
created: 2026-05-08
updated: 2026-05-08
upstream:
  - REQ-inkwell-agent-platform
  - repo-impact-map-inkwell-agent-platform
  - ADR-003
downstream: []
---

# ADR-010 Skill 加载：v1 仅静态加载（不预留 Executor 接口）

## 上下文

[REQ-008 Skills](../../01-requirements/requirements.md) 要求支持 [agentskills.io 格式](https://agentskills.io)：

- Discovery：解析 `SKILL.md` frontmatter（id / name / description）
- Activation：根据当前对话上下文匹配 description，命中后激活
- Execution：当 Skill 中有 SCRIPT 部分时执行（Bash / PowerShell / Python）

[Q-A7](../open-questions-arch.md) 用户答 "A 仅静态加载（v1 不允许执行任意脚本）"。[EX-008](../../01-requirements/requirements.md) 要求"Skill 在加载 / 激活 / 执行任一阶段失败时，UI 显式可见 + 不影响主对话"。

## 决策

**v1 仅实现 Skill Discovery + Activation 两阶段；不实现 Execution。Skill 中的 SCRIPT 段在 v1 被忽略，并在 UI 加 banner 提醒。后端不实现 `ISkillExecutor` 接口，避免给"未来执行任意脚本"留接口债。**

- 后端模块：[`Inkwell.Skills`](../../01-requirements/repo-impact-map.md) 只暴露 `ISkillRegistry`（Discovery）+ `ISkillActivator`（Activation）。
- 加载源：v1 支持"管理员上传 SKILL.md 文件 + 静态目录扫描"两种来源；不支持 Git pull / URL 远程加载。
- Skill 内容注入：命中后把 Skill 的 markdown body（去掉 SCRIPT block）作为 system message 追加到对话上下文。
- SCRIPT block 处理：解析时识别但不执行；UI Skill 详情页显示"该 Skill 含可执行脚本，v1 暂不支持"banner。
- 失败处理（[EX-008](../../01-requirements/requirements.md)）：Discovery 失败 → 该 Skill 不进 registry + 管理员收到错误日志；Activation 失败 → 默认未命中 + 对话照常进行。

## 备选项

### 备选 A（Q-A7 §B）：v1 仅静态，但预留 `ISkillExecutor` 接口

- **放弃理由**：(1) 接口预留没有真正的实现，是 [.NET YAGNI](https://learn.microsoft.com/dotnet/standard/design-guidelines/) 反例，会留在代码里成为技术债（[hx-doc-gardener / tech-debt-gc](../../../.he/docs/tech-debt-gc.md) 会扫到）；(2) 真正决定 v2 是否做 Execution 时，安全模型需要重新设计 — 现在的接口只能基于"v1 假设"，v2 大概率改签名；(3) 不预留接口反而是更诚实的边界声明。

### 备选 B（Q-A7 §C）：v1 仅静态 + 命令白名单（git / curl 等只读命令）

- **放弃理由**：(1) 白名单的安全模型门槛比"完全不执行"高 — 命令注入 / 路径穿越 / 资源耗尽 攻击面繁多；(2) v1 范围风险已签字（[OQ-006](../../01-requirements/open-questions.md)），不应再扩；(3) 用户场景里（Agent 平台 v1）绝大多数 Skill 是 prompt template + 描述，不需要执行命令。

### 备选 C：v1 直接做 Execution（Docker sandbox）

- **放弃理由**：(1) Docker-in-Docker 在 AKS Pod 中存在权限模型复杂性；(2) 沙箱逃逸 / 资源限制 / 凭据隔离需要专门安全审查；(3) 与 [OQ-006](../../01-requirements/open-questions.md) v1 范围严重冲突。

## 后果

### 正面

- 安全边界清晰："不执行任何脚本"是最强承诺，不存在沙箱逃逸风险。
- 实现路径最短：Skill 即"带 frontmatter 的 markdown"，只需 markdown 解析器 + 字符串拼接进 system prompt。
- v2 切到执行模式时，重新设计 `ISkillExecutor` 与沙箱模型，不会被 v1 的占位接口绑架。
- [agentskills.io](https://agentskills.io) 格式天然兼容（SCRIPT 块只是被忽略，不影响 Discovery / Activation）。

### 负面

- v1 用户期望"我写个 Skill 让 Agent 自动跑 git status"会落空 — 通过 UI banner + 文档明确告知。
- 后期切到 v2 Execution 模式时，部分 Skill 需要重写（v1 没用 SCRIPT 的 Skill 不受影响）。
- v1 仅静态加载意味着 Skill 没有"系统集成"语义 — 想做"调用第三方 API"必须走 [REQ-007 工具调用](../../01-requirements/requirements.md) 路径，不是 Skill 路径。

### 中性

- "管理员上传 SKILL.md"是 v1 唯一接入口；Git / URL 远程加载留 v2。
- Skill 与 Agent 的关系是"Agent 可启用一组 Skill"，命中后 Skill 进 system prompt — 这与 [agentskills.io 标准模式](https://agentskills.io) 一致。

## 状态

- **状态**：accepted
- **首次发布**：2026-05-08
- **关联**：supersedes 无；上游 [ADR-003](./ADR-003-agent-engine-microsoft-agent-framework.md) / [Q-A7](../open-questions-arch.md)
- **置信度**：high（Q-A7 已答；与 OQ-006 范围控制理念一致）
