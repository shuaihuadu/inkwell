---
applyTo: 'src/**/*.{ts,tsx}'
---

<!--
这是一份 TypeScript 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/typescript.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 ESLint + Prettier + tsconfig + tsc --noEmit，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 TypeScript Handbook、
Google TypeScript Style Guide、ESLint typescript-eslint 推荐规则等权威来源。
-->

# TypeScript 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 ESLint + Prettier + `tsconfig.json` (strict) 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 类型 / 接口 / 类：[ 待填，建议 PascalCase ]
- 接口：[ 待填，建议直接 `User` 而非 `IUser`（与 .NET 习惯不同） ]
- 变量 / 函数：[ 待填，建议 camelCase ]
- 常量：[ 待填，建议 SCREAMING_SNAKE_CASE 或 camelCase（项目内统一即可） ]
- React 组件：[ 待填，建议 PascalCase，文件名同名 ]
- Hook：[ 待填，建议 `use` 前缀 ]
- 不在本文件重复 `eslint` 已强制的命名规则

## 2. 错误处理

- 异常类型：[ 待填，建议自定义 `Error` 子类带 `name` 字段，禁止抛字符串 ]
- 异步错误：所有 `async` 函数必须 `try/catch` 或显式 `.catch()`，不留 unhandled promise rejection
- 不用异常做控制流：用 `Result<T, E>` 类型或 discriminated union 显式表达失败
- 边界校验：在 API 入口用 `zod` / `valibot` 等运行时校验，不依赖 TS 类型在运行时仍存在

## 3. 类型与异步

- `strict: true`：tsconfig.json 必须启用，本文件不重复
- 禁用 `any`：用 `unknown` + type narrowing；逃生口必须配 `// eslint-disable-next-line` + 一句理由注释
- 禁用 `as` 强转：除非紧跟 type guard；尽量用 `satisfies` 而非 `as`
- Promise：所有异步 IO 用 `async/await`，不混用 `.then()` 链式；并发用 `Promise.all` / `Promise.allSettled`
- 取消：长跑任务接 `AbortSignal`，不要自己造 `cancelled` 标志位

## 4. 测试

- 测试框架：[ 待填，vitest / jest / mocha 选一并固定 ]
- 测试文件命名：[ 待填，建议 `*.spec.ts` 或 `*.test.ts` 与被测文件同目录 ]
- 测试方法命名：[ 待填，建议 `describe('Subject', () => it('should X when Y'))` ]
- Mock：[ 待填，vitest mock / msw / sinon 选一 ]，只 mock 外部边界，不 mock 你自己的纯函数
- React 组件：[ 待填，建议 `@testing-library/react`，避免 enzyme ]
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 缩进 / 引号 / 分号 / import 排序 → `Prettier` + `eslint-plugin-import`
- 类型检查 → `tsc --noEmit` 入 CI
- typescript-eslint 规则 → `.eslintrc.*` 配 `recommended-type-checked`
- 包版本锁定 → `package.json` engines + lockfile（`pnpm-lock.yaml` / `package-lock.json`）
- 测试覆盖率 → CI 任务（`vitest --coverage` + 阈值断言）
