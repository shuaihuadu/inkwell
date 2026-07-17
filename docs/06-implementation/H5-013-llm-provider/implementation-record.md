---
id: H5-013-RECORD
title: LLM Provider 与 LiteLLM 实时模型接入 · 实施记录
stage: H5
document_type: implementation-record
status: draft
implementation_state: implemented
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

# H5-013 LLM Provider 与 LiteLLM 实时模型接入实施记录

> 本文件只记录仓库中可核实的当前实现和验证证据。`status` / `reviewers` 由 Owner 人工维护。

## 1. 实施状态

- **结论**：公共 LLM Provider、LiteLLM adapter、Agent Runtime、模型 API 和桌面端模型管理已实现。
- **记录日期**：2026-07-17。
- **数据影响**：未修改数据库模型或 Migration；工作区原有未跟踪 Migration 保持不动。

## 2. 已实现内容

| 路径 / 符号 | 当前职责 | 对应目标 |
| --- | --- | --- |
| `Inkwell.Abstractions/LLM` | `ILLMProvider`、`IChatLLMProvider`、公共模型、分类和测试结果 | 可替换 Provider 端口 |
| `providers/LLM/Inkwell.LLM.LiteLLM` | 实时合并模型与 group 信息、归一化分类、创建 `IChatClient`、连通性测试 | LiteLLM 事实源 |
| `Inkwell.Core/AgentRuntime` | 校验 Chat 分类并构建 MAF `AIAgent` | Agent 执行 |
| `Inkwell.WebApi/Controllers/ModelsController` | 已认证列表/详情和 Super 连通性测试 | 模型管理 API |
| `src/app/desktop/src/features/models` | 分类筛选、能力展示、刷新与 Super 测试 | 模型管理界面 |
| `agent-workspace.tsx` | Agent 设计仅展示 Chat 类模型 | Agent 设计 |

旧多来源 Registry、本地 metadata 覆盖、`RuntimeId` 路由和 Azure OpenAI/LiteLLM 双 Runtime Provider 已删除。Azure OpenAI Embedding 依赖保留，不属于本任务的 Chat Runtime。

## 3. 验证证据

| 验证项 | 命令或范围 | 结果 |
| --- | --- | --- |
| Solution build | `dotnet build Inkwell.slnx` | 成功；本任务无新增 warning，保留 Provider contract 项目 20 条既有风格 warning |
| Provider 与 Agent Factory | `LiteLLMProviderTests` + `ModelRoutingAgentFactoryTests` | 8/8 通过 |
| ModelsController | `ModelsControllerTests` | 4/4 通过 |
| Core 完整回归 | `dotnet test tests/Inkwell.Core.Tests/Inkwell.Core.Tests.csproj --no-restore` | 21/21 通过 |
| WebApi 完整回归 | `dotnet test tests/Inkwell.WebApi.Tests/Inkwell.WebApi.Tests.csproj --no-restore` | 14/14 通过 |
| Desktop lint | `npm run lint` | 通过 |
| Desktop production build | `npm run build` | 通过 |
| Electron E2E | `npm run test:e2e` | 3/3 通过；覆盖两类模型、Super 测试和 1080x720 无页面级横向溢出 |
| Patch hygiene | `git diff --check` | 通过 |
| LiteLLM discovery | 真实 `/v1/models` 与 `/model_group/info` | 已验证，可读取当前模型及能力 |
| LiteLLM generation | 真实 Chat Completions 与 Responses 请求 | HTTP 200 |
| WebApi route/auth | 运行中 `GET /api/models`（无凭据） | HTTP 401，确认路由与认证策略生效 |

## 4. 验证边界

- 当前没有可安全复用的登录会话 token，未读取系统 Keychain，也未猜测账号密码；因此运行中 WebApi 尚未完成带认证的模型列表和 Super 测试请求。
- Embedding、图片生成和视频生成当前只做发现与分类，不提供执行接口。
- 暂无独立 H4 TC；当前测试以 HD-019 和 H5-013 任务简报为依据。

## 5. 已知风险

- Provider 更换后，已保存 Agent 的 `modelId` 可能失效；v1 不做自动映射。
- LiteLLM `/model_group/info` 缺失能力时公共契约保留 `null`，不推断为 `false`。
- 模型测试会产生一次最小 Chat 请求；非 Chat 模型不会发送生成请求。

## 6. 提交信息草稿

- **Design**: ADR-026, HD-019
- **Tests**: Provider 8/8, WebApi 4/4, Electron E2E 3/3
- **Verify**: solution build, desktop lint/build, diff check
- **Docs**: H5-013 task brief and implementation record
- **Risk**: authenticated live WebApi smoke remains; no schema changes
- **Task**: H5-013
