---
mode: 'agent'
description: '审计 task-board.md 与实际 commit/PR/分支的对齐：列出"板上 done 但代码没合"、"代码已合但板上还在 doing"、"陈年 draft"、"幽灵任务"四类失同步'
tools:
  [
    vscode/extensions,
    vscode/getProjectSetupInfo,
    vscode/installExtension,
    vscode/memory,
    vscode/newWorkspace,
    vscode/resolveMemoryFileUri,
    vscode/runCommand,
    vscode/vscodeAPI,
    vscode/askQuestions,
    vscode/toolSearch,
    execute/getTerminalOutput,
    execute/killTerminal,
    execute/sendToTerminal,
    execute/createAndRunTask,
    execute/runInTerminal,
    execute/runNotebookCell,
    read/terminalSelection,
    read/terminalLastCommand,
    read/getNotebookSummary,
    read/problems,
    read/readFile,
    read/viewImage,
    agent/runSubagent,
    browser/openBrowserPage,
    browser/readPage,
    browser/screenshotPage,
    browser/navigatePage,
    browser/clickElement,
    browser/dragElement,
    browser/hoverElement,
    browser/typeInPage,
    browser/runPlaywrightCode,
    browser/handleDialog,
    edit/createDirectory,
    edit/createFile,
    edit/createJupyterNotebook,
    edit/editFiles,
    edit/editNotebook,
    edit/rename,
    search/changes,
    search/codebase,
    search/fileSearch,
    search/listDirectory,
    search/textSearch,
    search/usages,
    web/fetch,
    web/githubRepo,
    web/githubTextSearch,
    todo,
  ]
---

# /sync-board — 审计 task-board.md 与代码的对齐

`task-board.md` 是"任务现状的快照"，但人手维护下经常会陈旧：合了还在 doing、删了还挂在板上、起了草忘了推进。本指令机械化对照真实仓库状态，列出**失同步项**和**修复建议**。

## 触发场景

- 周会前对账
- 长假回来不知道板上哪些已经过时
- 准备发版前确认 H5 任务收口情况
- 新人入职想先看看板上哪些任务真有事在做

## 你（AI）必须遵守

1. **每条结论必须有证据**：commit hash、PR 号、文件路径、grep 命中——**禁止主观判断"这个看起来废了"**
2. **不擅自改 `task-board.md`**：本指令只产出对账报告；修复留给 owner 自己或 `/new-task` / 提交流程
3. **失同步分四类**，明确归类：
   - `STALE_DOING` — 板上 `doing`，但近 N 天（默认 14）无任何 commit/PR 关联
   - `MISSED_DONE` — 板上 `doing` / `ready`，但已经合并的 commit 包含 `Task: T-NNN` 字段
   - `GHOST` — 板上有 `T-NNN`，但全仓库搜不到 `T-NNN` 字符串（包括 commit、PR、`docs/06-tasks/`）
   - `ZOMBIE_DRAFT` — 板上 `draft`，超过 N 天（默认 30）未变更
4. **回退周期可调**：用户能传 `--stale-days=14 --zombie-days=30`，默认按上面取值
5. **不破坏现有 owner**：`修复建议`只写"建议 owner 做什么"，不替 owner 做决定

## 输入

- `docs/06-tasks/task-board.md`（如不存在，提示用户先起一条 `/new-task`）
- 仓库 git 历史（用 `changes` 工具看最近 N 天的 commit）
- 可选参数：`--stale-days=N`、`--zombie-days=M`、`--branch=main`（默认看当前分支与 `main`）

## 输出

直接在对话里展示一份对账报告，结构：

```markdown
# Task Board 对账 · <YYYY-MM-DD HH:mm> · 分支 <name>

参数：stale-days=14, zombie-days=30, since=<YYYY-MM-DD>

## 1. STALE_DOING（板上 doing 但近期无活动）

| Task  | 标题 | owner | last-update        | 建议                                    |
| ----- | ---- | ----- | ------------------ | --------------------------------------- |
| T-NNN | ...  | ...   | <YYYY-MM-DD or 无> | 与 owner 确认是否还在做 / 是否要 cancel |

## 2. MISSED_DONE（已合入但板上没收口）

| Task  | 板上状态 | 实际 commit               | 建议                            |
| ----- | -------- | ------------------------- | ------------------------------- |
| T-NNN | doing    | <hash7>: <msg first line> | 把 status 改 done，并补 done-at |

## 3. GHOST（板上有但全仓搜不到）

| Task  | 标题 | created | 建议                                     |
| ----- | ---- | ------- | ---------------------------------------- |
| T-NNN | ...  | ...     | 与 owner 确认是否撤销，否则补 brief 文件 |

## 4. ZOMBIE_DRAFT（draft 状态过久）

| Task  | 标题 | created | 建议                                               |
| ----- | ---- | ------- | -------------------------------------------------- |
| T-NNN | ...  | ...     | 决定起做（status=ready）或撤销（status=cancelled） |

## 5. 健康指标

- 板上任务总数：N
- doing：N
- ready：N
- draft：N
- done（板上仍在）：N
- 失同步项：S（占比 S/N）
```

## 流程

1. 解析 `docs/06-tasks/task-board.md`，列出所有 `T-NNN` 行与状态
2. 列出近 `--stale-days` 天的 commit，提取每条 commit message 中的 `Task: T-NNN`
3. 全仓 grep `T-NNN` 字符串，确认每个板上任务是否在代码 / 文档 / commit 中出现
4. 按四类分组失同步项
5. 输出对账报告

## 完成后下一步

- 板上 owner 按"建议"逐项处理
- 处理后再跑一次 `/sync-board` 确认归零
- 如失同步比例长期高于 30%，考虑改用更轻的看板工具或缩短任务粒度
