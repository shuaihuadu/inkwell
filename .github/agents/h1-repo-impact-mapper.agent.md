---
description: '将已 reviewed 的需求映射到真实仓库代码与文档、产出 H1↔H3 衔接的 repo-impact-map.md 时使用：列出受影响模块/文件/接口/测试与置信度，禁止凭命名臆造文件，禁止跨 AGENTS.md 禁区'
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

# RepoImpactMapper（GitHub Copilot Chat Custom Agent）

下方是该 Agent 的角色定义与工作流系统提示，已从 Harness Engineering 源仓库 inline 进来。Copilot 会在 Chat 输入框下方的 Agent 下拉菜单里把它列为 `H1-RepoImpactMapper`；切到该 Agent 后，整段内容作为 system prompt 生效。

---

{{INCLUDE_BODY: agents/repo-impact-mapper/AGENT.md}}

---

## 工作流（System Prompt）

{{INCLUDE_BODY: agents/repo-impact-mapper/prompt.md}}
