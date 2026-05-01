---
applyTo: '**'
---

# 提交信息规范

任何对本仓库的提交（包括 PR 中的每条 commit）必须遵守下面的格式。该规则与 Harness Engineering 规范 §8.6（H5 提交要求）、`agents/_shared/io-contracts.md` §4 保持一致。

## 1. 模板

```text
<type>(<scope>): <summary>

Design: HD-xxx
Tests: TC-xxx, TC-yyy
Verify: dotnet test
Docs: updated | not needed
Risk: none | <short note>
Task: TASK-xxx
```

## 2. 字段说明

| 字段     | 是否必填 | 说明                                                                   |
| -------- | -------- | ---------------------------------------------------------------------- |
| `Design` | 必填     | 实现的设计编号（`HD-NNN` / `API-NNN` / `DB-NNN`），允许多个用 `,` 分隔 |
| `Tests`  | 必填     | 覆盖的测试用例编号（`TC-NNN`），允许多个                               |
| `Verify` | 必填     | 实际运行过的验收命令                                                   |
| `Docs`   | 必填     | `updated` 或 `not needed`；`not needed` 必须能在 PR 描述里解释         |
| `Risk`   | 必填     | `none` 或具体遗留风险一句话                                            |
| `Task`   | 必填     | 对应的 `ai-task-brief.md` 编号（`TASK-NNN`）                           |

## 3. type / scope

- `type` 取自 conventional commits：`feat / fix / docs / refactor / test / chore / build / ci / style / perf`
- `scope` 取自项目模块名（如 `core` / `webapi` / `webapp`）；跨模块用 `*`

## 4. AI 协作约束

- 当用户在 Chat 中请求"写一条提交信息"时，**严格按照本模板**输出，不得遗漏字段；缺信息时反问而非自行编造。
- 当 `Design` / `Tests` / `Task` 编号无法在仓库中定位到对应文档时，提醒用户先建立追溯记录再提交。
- 不得在提交信息中夹带"AI 生成"等元注释——追溯由 `Task:` 字段承担，提交记录本身保持中立。
