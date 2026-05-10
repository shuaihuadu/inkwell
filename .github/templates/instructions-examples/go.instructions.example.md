---
applyTo: '**/*.go'
---

<!--
这是一份 Go 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/go.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 gofmt + go vet + golangci-lint，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 Effective Go、
Google Go Style Guide、Uber Go Style Guide、golangci-lint 默认规则等权威来源。
-->

# Go 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `gofmt` + `go vet` + `golangci-lint` 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 包名：[ 待填，建议短小全小写、单数、不带下划线，如 `httpclient` 而非 `http_client` ]
- 接口：[ 待填，单方法接口建议 `-er` 后缀，如 `Reader`、`Closer` ]
- 公开符号：PascalCase；私有符号：camelCase（不可改，gofmt 强制）
- 缩写：保持大小写一致，`URL` 不要写成 `Url`，`HTTPS` 不要写成 `Https`
- 错误变量：[ 待填，建议 `ErrXxx` 前缀，如 `ErrNotFound` ]
- 上下文参数：第一个参数必须是 `ctx context.Context`

## 2. 错误处理

- **必须**：错误立即处理，不囤积；不写 `_ = err`
- 包装错误：用 `fmt.Errorf("xxx: %w", err)` 保留链；不写 `fmt.Errorf("xxx: %v", err)`（断链）
- 哨兵错误：用 `errors.Is` / `errors.As` 比较，不用 `==`（除非确定不会被包装）
- 不 `panic` 应用代码：库可以；应用代码遇 panic 必须有 `recover` 兜底
- 不忽略 `defer` 的错误：如 `defer f.Close()` 在写文件时要 `defer func() { err = errors.Join(err, f.Close()) }()`

## 3. 并发

- goroutine 必须有明确退出路径：用 `context.Context` + `select`，禁止泄漏
- 共享状态：用 channel 传递所有权 / `sync.Mutex` / `sync/atomic`，不裸全局变量
- `WaitGroup`：`Add` 必须在 goroutine 启动前调用，不在 goroutine 内
- `errgroup.Group` 优于手写 sync.WaitGroup + error channel

## 4. 测试

- 测试文件：与被测文件同包，命名 `<name>_test.go`
- 测试函数：`func Test<Subject>_<Scenario>(t *testing.T)`
- 表驱动测试：[ 待填，建议默认表驱动，明显单例除外 ]
- Mock：[ 待填，标准 testing / testify/mock / mockery / 接口注入 选一 ]
- 子测试：用 `t.Run("scenario", func(t *testing.T) { ... })`
- 平行测试：默认 `t.Parallel()`，除非测试有共享状态
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 格式化（缩进 / 空行 / 大括号）→ `gofmt` / `goimports`
- 静态检查（未使用变量 / shadow / nil 解引用）→ `go vet`
- 综合 lint → `golangci-lint`（启用 `errcheck` / `staticcheck` / `gosimple` / `revive` 等）
- Go 版本 / 模块依赖 → `go.mod` `go` directive + `go.sum`
- 测试覆盖率 → CI 任务（`go test -cover` + 阈值断言）
