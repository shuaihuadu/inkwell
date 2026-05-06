---
mode: 'agent'
description: '跑一次当前 H 阶段的 phase-gate-checklist：逐项核对、给出 PASS/FAIL/UNKNOWN，不达标即列出阻塞清单与补救建议'
tools:
  [
    search/codebase,
    search/textSearch,
    search/fileSearch,
    search/listDirectory,
    search/usages,
    search/changes,
    read/readFile,
    read/problems,
    read/getNotebookSummary,
  ]
---

# /run-gate — 跑当前阶段的 Phase Gate 检查

按照 `.github/templates/phase-gate-checklist.md` 的清单，**机械化**核对当前 H 阶段是否达到下一阶段的入门门槛。这是一个"拦门员"角色：宁可拦下来追问，也不放含糊的产物流入下游。

## 触发场景

- H1 `requirements.md` 写完，准备进入 H2 设计前
- H3 设计文档写完，准备进入 H4 测试用例前
- H4 测试用例写完，准备进入 H5 编码前
- H5 一组任务全部 done，准备进入 H6 发布前

## 你（AI）必须遵守

1. **每一项核对都要给证据**：文件路径 + 行号、或 commit hash、或检索关键词的结果数；**禁止主观判断**
2. **每一项的结论只能是 `PASS` / `FAIL` / `UNKNOWN`**——`UNKNOWN` 必须配 `reason` 与 `how_to_resolve`
3. **任何一项 `FAIL` 即门未过**，结论汇总写"阻塞"，下游 Agent 不该启动
4. **不擅自修文档**：本指令只检查、只汇报；修复留给对应阶段的 Agent
5. **不照搬模板**：模板里有占位符项，要换成本仓库的真实文件名 / 路径再核对
6. 仓库无 `phase-gate-checklist.md` 实例时，**用 `.github/templates/phase-gate-checklist.md` 现场实例化一次**，但要先反问"当前要核对哪个阶段（H1/H3/H4/H5）"

## 输入

- 用户应指明当前阶段（`H1` / `H3` / `H4` / `H5`）；未指明时反问
- `.github/templates/phase-gate-checklist.md`（模板源）
- 当前阶段产物文件（如 `docs/01-requirements/requirements.md`）
- 上游产物（如 H3 检查需要看 H1 的 `requirements.md`）

## 输出

按以下结构产出一份 markdown 报告（不写文件，直接展示给用户）：

```markdown
# Phase Gate · <H阶段> · <YYYY-MM-DD>

| #   | 项            | 结论                  | 证据 / 原因                     |
| --- | ------------- | --------------------- | ------------------------------- |
| 1   | <清单第 1 项> | PASS / FAIL / UNKNOWN | <文件:行号 / commit / 检索结果> |
| 2   | ...           | ...                   | ...                             |

## 阻塞汇总

- [ ] <FAIL 项 1> · 补救：<具体动作>
- [ ] <FAIL 项 2> · 补救：<具体动作>

## 结论

- ✅ 全部 PASS：可进入 <下一 H>
- ❌ 有 FAIL：阻塞，先解决上方阻塞项
- ⚠ 有 UNKNOWN：需补充信息后重跑 /run-gate
```

## 流程

1. 反问/确认当前阶段
2. 读 `.github/templates/phase-gate-checklist.md` 取本阶段清单
3. 逐项核对，每项写证据
4. 汇总结论
5. 若有 `FAIL`，列出补救清单和应该切到哪个 Agent 处理（如 H3 设计 FAIL → 切 `H3-DesignReviewer`）

## 完成后下一步

- 全 PASS：用户可推进到下一阶段
- 有 FAIL：按"阻塞汇总"补，再 `/run-gate` 复核
- 有 UNKNOWN：补完信息后 `/run-gate` 复核
