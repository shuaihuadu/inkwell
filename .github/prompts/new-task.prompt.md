---
mode: 'agent'
description: '从一个高层目标起一个 H5 编码任务：起草 ai-task-brief.md、登记 task-board.md，给出范围闭包与 Verify 命令；不直接改代码'
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

# /new-task — 起一个 H5 编码任务

把用户给出的高层目标拆成一个**人可签名、机可执行**的 H5 任务：产出一份 `ai-task-brief.md` 草稿，并在 `docs/06-tasks/task-board.md` 登记一行，状态置为 `draft`。

## 触发场景

- 用户描述了一个想做的小改动 / 小特性 / Bug 修复，但还没写任务说明
- 已有 H1 `requirements.md` 和 H3 设计，但缺一个聚焦的执行任务
- 在大需求被切片后给某个切片起任务

## 你（AI）必须遵守

1. **本指令只产出文档，不改源码、不跑测试、不开 PR**——`H5-CodingExecutor` 才执行
2. **`允许修改的文件清单`必须基于真实搜索**，每条都给可点击路径；找不到证据时进 `open-questions`，不要瞎猜
3. **`Verify 命令`必须可在 CI 直接复现**：给完整命令行（含工作目录、环境变量），禁止"按需运行单元测试"这种废话
4. **每条任务只对应一个目标**：超过一句话能讲清的，拆成多个任务、各自一行 `docs/06-tasks/task-board.md`
5. **任务编号 `T-NNN` 不复用**：从 `docs/06-tasks/task-board.md` 现存最大编号 +1；若文件还不存在则从 `T-001` 起
6. 输入信息不足以填满模板的核心字段时，**返回阻塞 + open-questions**，不要用 `<TBD>` / "待定" 占位混过去

## 输入

- 用户的一句话或一段目标描述
- 仓库根 `.github/templates/ai-task-brief.md`（任务说明模板）
- `docs/06-tasks/task-board.md`（项目任务看板；不存在时**按 `.github/templates/task-board.md` 创建到该位置**，不要复制到仓库根）
- 已有的需求 / 设计文档（如 `docs/01-requirements/`、`docs/04-detailed-design/`）

## 输出

1. **新文件**：`docs/06-tasks/T-NNN-<slug>.md`（按 `.github/templates/ai-task-brief.md` 模板填充）
   - 必填：`Task ID`、`目标`、`允许修改的文件`、`禁止修改的文件`、`Verify 命令`、`完成判据`、`回滚策略`
   - 可选但推荐：`关联 REQ`、`关联 ADR`、`关联设计文档章节`
2. **修改**：`docs/06-tasks/task-board.md` 追加一行（文件不存在则先按 `.github/templates/task-board.md` 创建到该位置）
   - 列：`T-NNN | 标题 | owner=<提问人或 ai> | status=draft | created=<YYYY-MM-DD> | brief=docs/06-tasks/T-NNN-<slug>.md`
3. **若信息不足**：返回阻塞 JSON：
   ```json
   { "blocked": true, "reason": "...", "open_questions": ["...", "..."] }
   ```

## 流程

1. 读 `docs/06-tasks/task-board.md`（不存在则按 `.github/templates/task-board.md` 创建、预设为空表），确认下一个 `T-NNN` 编号
2. 读用户目标，搜代码 / 文档定位"允许修改的文件"
3. 套用 `.github/templates/ai-task-brief.md` 起草任务说明
4. 把行追加进 `docs/06-tasks/task-board.md`
5. 输出"已创建 T-NNN"摘要 + 下一步建议（让用户人工审任务说明，审完置 `status=ready` 后再切 `H5-CodingExecutor`）

## 完成后下一步

- **人工**：审 `ai-task-brief.md` 草稿，确认范围/Verify 后改 `status=ready`
- **AI**：切 `H5-CodingExecutor` Agent 执行；执行完切 `H5-CommitAuditor` 校验提交
