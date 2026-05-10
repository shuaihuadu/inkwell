---
applyTo: 'src/**/*.{js,jsx,mjs,cjs}'
---

<!--
这是一份 JavaScript 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/javascript.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 ESLint + Prettier，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Airbnb JavaScript Style Guide、
Standard JS、MDN JavaScript Reference、ESLint 推荐规则等权威来源。
注意：**新项目优先用 TypeScript**——本文件只针对存量 JS 代码或刻意保持 JS 的项目。
-->

# JavaScript 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 ESLint + Prettier 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 类 / 构造函数：[ 待填，建议 PascalCase ]
- 变量 / 函数：[ 待填，建议 camelCase ]
- 常量：[ 待填，建议 SCREAMING_SNAKE_CASE 或 camelCase（项目内统一即可） ]
- 私有约定：[ 待填，建议 `#privateField`（class 私有字段语法）或 `_camelCase`（约定式私有） ]
- 模块：[ 待填，建议优先 ESM，CJS 仅在工具链兼容必需时使用 ]

## 2. 错误处理

- 异常类型：[ 待填，建议自定义 `Error` 子类，禁止抛字符串 / 对象字面量 ]
- 异步错误：所有 `async` 函数必须 `try/catch` 或显式 `.catch()`
- 类型边界：JS 没有编译期类型，必须在 API 入口用 `zod` / `joi` / `ajv` 做运行时校验
- 严格模式：所有源文件 `'use strict'`（ESM 默认开启，CJS 顶部加）

## 3. 异步与并发

- 异步：默认 `async/await`，不混用 `.then()` 链式
- Promise 并发：`Promise.all` / `Promise.allSettled`，不要 `for...of await`
- 取消：长跑任务接 `AbortSignal`
- 不要 `new Promise(...)` 包同步代码——直接 `Promise.resolve()`

## 4. 测试

- 测试框架：[ 待填，jest / mocha / vitest 选一并固定 ]
- 测试文件命名：[ 待填，建议 `*.spec.js` 或 `__tests__/<name>.test.js` ]
- 测试方法命名：[ 待填，建议 `describe('Subject', () => it('should X when Y'))` ]
- Mock：[ 待填，jest.mock / sinon / msw 选一 ]
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 缩进 / 引号 / 分号 / 行尾逗号 → `Prettier`
- 命名 / 圈复杂度 / 未使用变量 → `ESLint`（推荐 `eslint:recommended` + 项目自配）
- import 排序 → `eslint-plugin-import`
- 包版本锁定 → `package.json` + lockfile
- 测试覆盖率 → CI 任务（`jest --coverage` + 阈值断言）
