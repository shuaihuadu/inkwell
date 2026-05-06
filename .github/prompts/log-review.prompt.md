---
mode: 'agent'
description: '把会议/PR/Issue 评审记录誊写到 review-record.md：保留发言人、决议、行动项、争议未决，机械化、不脑补'
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

# /log-review — 把评审记录归档为 review-record.md

把一次评审（设计评审 / PR 评审 / 需求评审 / 立项会等）的原始记录，按 `.github/templates/review-record.md` 的结构整理成可追溯的归档文件。重点是**保留原话、保留分歧、保留行动项**，绝不"提炼总结"成一团模糊的水货。

## 触发场景

- 一次会议结束，用户把会议纪要 / 录音转写 / 聊天记录贴进来
- PR 上一轮评审完成，需要把评论与决议归档
- 异步评审（Issue 长串讨论）结束，需要落档

## 你（AI）必须遵守

1. **不改原话**：发言人 → 内容引用，必要时只做错别字修正与脱敏；**禁止"翻译"成更"专业"的表述**
2. **必须保留分歧**：达成一致写"决议"，没达成的写"争议未决"，未来回看才知道"为什么这么定"
3. **每个 `行动项` 必须有 `owner` 和 `due`**——没有就回到原始文本里追问，没有就写 `owner=<TBD-未指定>`、`due=<TBD-未指定>` 并放进"待澄清"
4. **追溯链必须打齐**：`关联 REQ / ADR / Task / PR / Commit`——能链就链，链不到就写 `<无>` 而不是省略
5. **脱敏**：去掉电话、邮件正文私密信息、API key 等；保留发言人姓名（如有公开身份）或角色（如"架构师 A"）
6. **不评价**：不写"这个想法不错"之类的主观评价，只搬事实
7. 文件命名 `docs/07-reviews/YYYY-MM-DD-<topic-slug>.md`，不存在的目录就建

## 输入

- 用户贴的原始文本（会议纪要 / 评论合集 / 聊天记录）
- 评审主题（如"H3 详细设计评审 · 用户中心模块"）
- 评审日期（默认今日，用户可覆盖）
- 关联追溯（用户给的 REQ/ADR/PR 编号；没给就反问"是否有可关联的需求/设计/PR"）
- `.github/templates/review-record.md`（模板源）

## 输出

新文件 `docs/07-reviews/YYYY-MM-DD-<topic-slug>.md`，按 `.github/templates/review-record.md` 结构填写，包含：

- frontmatter：`date`、`topic`、`participants`、`type`、`status`、`upstream`
- 正文章节：
  - `## 1. 背景` — 1-3 句话说为什么开这次评审
  - `## 2. 出席与角色`
  - `## 3. 关键发言（按时间序）` — 发言人 → 引用
  - `## 4. 决议` — 达成一致的事
  - `## 5. 争议未决` — 没谈拢的点 + 暂定立场
  - `## 6. 行动项` — `T-NNN | 说什么 | owner | due | status=open`
  - `## 7. 追溯` — 关联 REQ / ADR / PR / Commit / Task
  - `## 8. 待澄清` — owner/due 未指明的问题，回头要补的信息

## 流程

1. 反问/确认主题、日期、关联追溯（缺则要）
2. 通读原始文本，识别发言人和章节边界
3. 套模板填正文
4. 写文件
5. 输出"已归档至 docs/07-reviews/...，行动项 N 项，待澄清 M 项"

## 完成后下一步

- 行动项要进 `task-board.md` 时，对每条切 `/new-task`
- 决议涉及设计变更时，切 `H2-ArchitectAdvisor` 走 ADR
- 待澄清要补的：直接 @ 责任人，或再起一次评审
