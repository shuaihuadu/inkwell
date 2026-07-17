---
id: H5-013
title: LLM Provider 与 LiteLLM 实时模型接入 · AI 任务简报
stage: H5
document_type: task-brief
status: draft
authors:
  - name: GitHub Copilot
    role: agent
reviewers: []
created: 2026-07-17
updated: 2026-07-17
upstream:
  - REQ-005
  - REQ-006
  - ADR-026
  - HD-019
tests: []
downstream: []
---

# H5-013 LLM Provider 与 LiteLLM 实时模型接入任务简报

## 1. 任务目标

删除 Inkwell 本地模型清单、多来源 Registry 和 Runtime 路由，建立公共 `ILLMProvider` 端口及独立 `LiteLLMProvider`。模型 API 和 Agent 设计实时读取 LiteLLM；Agent 执行只使用已保存的 `modelId` 创建统一 `IChatClient`。

## 2. 不做范围

- 不实现 LiteLLM Portal 的模型写操作。
- 不实现 Embedding、图片和视频的实际生成，只建立模型分类和可扩展端口边界。
- 不同时注册多个 LLM Provider，不实现跨 Provider fallback。
- 不修改 EF Migration 或数据库模型。

## 3. 上游设计引用

- `AGENTS.md` §3：Ports & Adapters 和 Provider 依赖方向。
- `docs/03-architecture/adr/ADR-026-model-gateway-litellm.md`：单一 LLM Provider 与 LiteLLM 事实源。
- `docs/04-detailed-design/Inkwell.Core/HD-019-Inkwell.Core.Models.md`：公共契约、分类、运行时和测试设计。

## 4. 测试引用

暂无独立 H4 TC；临时以 HD-019 §8 和本简报 §9 为验证依据，测试设计缺口记录在 §12。

## 5. 当前基线与问题

- 当前 `IModelRegistryService` 聚合 appsettings 与 LiteLLM，维护本地 metadata 和 `RuntimeId`。
- 当前 Agent Factory 先选择 Runtime Provider，再创建 Chat Client。
- 模型管理页仍为占位页，Agent 新建表单已调用 `/api/models`。

上述实现与 LiteLLM 单一事实源、单 Provider 部署目标不一致。

## 6. 允许修改的文件

- `AGENTS.md`
- `README.md`
- `docs/03-architecture/adr/ADR-026-model-gateway-litellm.md`
- `docs/04-detailed-design/Inkwell.Core/HD-019-Inkwell.Core.Models.md`
- `docs/06-implementation/**`
- `src/core/Inkwell.Abstractions/**`
- `src/core/Inkwell.Core/**`
- `src/core/providers/LLM/Inkwell.LLM.LiteLLM/**`
- `src/core/Inkwell.WebApi/**`
- `src/core/Inkwell.Worker/**`
- `src/core/Inkwell.AppHost/**`
- `src/app/desktop/src/**`
- `tests/Inkwell.Core.Tests/**`
- `tests/Inkwell.WebApi.Tests/**`
- solution、central package 和项目引用文件

## 7. 禁止修改

- `src/core/providers/Persistence/Inkwell.Persistence.*/Migrations/**`
- 数据库实体、Repository 和 schema
- LiteLLM Portal 数据库内容
- 文档 `status` / `reviewers` 人工签字位

## 8. 实现要求

1. 公共端口和 DTO 位于 `Inkwell.Abstractions`，不得出现 LiteLLM 私有类型。
2. `LiteLLMProvider` 位于独立 Provider csproj，不依赖 `Inkwell.Core`。
3. 删除配置模型来源、metadata 覆盖、`SourceId`/`RuntimeId`/`RemoteModelId` 和原生 Runtime 路由。
4. 实时合并 `/v1/models` 与 `/model_group/info`，保留原始 mode 并归一化类别。
5. Agent Factory 只接受 Chat 类模型，使用 `IChatLLMProvider` 创建 `IChatClient`。
6. 模型测试端点必须鉴权并避免泄漏凭据。

## 9. 测试要求

1. LiteLLM Provider 单元测试覆盖分类、能力、未知 mode、缺失能力和 HTTP 失败。
2. Agent Factory 测试覆盖 Chat 成功和非 Chat 拒绝。
3. ModelsController 测试覆盖列表、详情和测试端点授权。
4. 完整 Core/WebApi 测试与解决方案构建通过。
5. 运行中 LiteLLM 冒烟验证模型列表及一次真实 Chat 请求。

## 10. 验收命令

```shell
dotnet test tests/Inkwell.Core.Tests/Inkwell.Core.Tests.csproj --no-restore
dotnet test tests/Inkwell.WebApi.Tests/Inkwell.WebApi.Tests.csproj --no-restore
dotnet build Inkwell.slnx --no-restore
git diff --check
```

## 11. 完成标准

- LiteLLM Portal 模型实时出现在模型 API 和 Agent 下拉中。
- Inkwell 不保存第二份模型目录或上游私有配置。
- Agent 可使用 Chat 类模型完成真实调用，非 Chat 类模型不会进入 Chat Agent。
- §10 全部命令通过，无本任务新增 warning。

## 12. 风险、假设与待确认项

- Provider 更换后旧 `modelId` 可能失效；v1 接受显式迁移，不实现自动映射。
- Embedding、图片和视频执行 API 尚未做真实端到端验证，仅保留分类。
- 暂无独立 H4 TC，后续应补 Provider contract 测试矩阵。
- 待 Owner 确认：无；Owner 已明确要求进入文档、实现和测试阶段。

## 13. H5 交付格式

完成后记录修改文件、验证结果、偏差和六字段提交信息草稿，不运行 git commit。
