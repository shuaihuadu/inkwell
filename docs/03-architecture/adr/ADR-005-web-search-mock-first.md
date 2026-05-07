---
id: ADR-005
title: 联网搜索（T-1）本期 Mock + 接口预留
stage: H2
status: accepted
authors:
  - name: H2-ArchitectAdvisor
    role: agent
date: 2026-05-07
upstream:
  - architecture-custom-agent
  - tech-selection-custom-agent
supersedes: []
superseded-by: []
---

# ADR-005：联网搜索（T-1）本期 Mock + 接口预留

## 上下文

[REQ-011 + AC-011-1 + ND-013](../../01-requirements/requirements.md) 锁定首批 Tool = T-1（联网搜索）+ T-3（代码沙盒）。
[GAP-005](../../01-requirements/repo-impact-map.md#3-缺失发现43) 联网搜索服务商本期未定。
[R8](../../01-requirements/requirements.md) 已识别"第三方搜索合规"风险用户接受。

H2 反问 Q7 用户答：
> 可以先 Mock 实现，后续再进行真实的实现

## 决策

- 引入 `IWebSearchTool` 接口（输入 `query: string`，输出 `Results: List<{Title, Snippet, Url}>`）
- 本期 `MockWebSearchTool` 实现：返回静态 JSON 含 query echo + 3~5 条预制摘要
- 生产实现：`BingWebSearchTool` / `TavilyWebSearchTool` / `BraveWebSearchTool` 等候选，待 [OQ-A-002](../open-questions-arch.md) 关闭
- Mock 实现已可覆盖 [E6 首次启用须知](../../01-requirements/requirements.md) + [AC-011-3/4/5](../../01-requirements/acceptance-criteria.md) 全部交互测试

## 备选项

| 备选 | 放弃理由 |
| --- | --- |
| 本期就接入 Bing Web Search API | 服务商未确定（[OQ-A-002](../open-questions-arch.md)），强行落地后切换成本高；Bing Web Search 未来下线公告（如有）需重评 |
| 本期接入 Tavily / Brave / Google CSE | 同上 |
| 本期不上线 T-1（仅 T-3） | 与 [ND-013](../../01-requirements/requirements.md) 锁定首批 = T-1 + T-3 冲突；P3 用户须知交互失去测试覆盖 |
| 用 `HttpClient` 直接调一个公开搜索 endpoint | 服务条款 / 速率 / 合规风险高 |

## 后果

### 正面

- Mock 实现 < 100 行，H5 编码任务卡轻量
- [E6 首次启用须知](../../01-requirements/requirements.md) / [AC-011-3](../../01-requirements/acceptance-criteria.md) 等交互测试无需真实服务商即可跑通
- vNext 切真实服务商时只换实现不改接口（[RISK-005](../risk-analysis.md)）

### 负面

- 真实服务商切换前 [R8 第三方搜索合规](../../01-requirements/requirements.md) 缓解措施未实战验证（如内容过滤 / 数据驻留 / 速率上限）
- Mock 不能验证真实延迟与失败模式（[AC-011-4 网络异常重试](../../01-requirements/acceptance-criteria.md) Mock 中只能"假装失败"）

### 中性

- vNext 启用真实服务商时，需 H4 阶段补真实供应商场景测试（不是 H2 工作）

## 状态

`accepted` · 2026-05-07
