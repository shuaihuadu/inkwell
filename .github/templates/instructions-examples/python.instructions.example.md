---
applyTo: 'src/**/*.py'
---

<!--
这是一份 Python 代码规范的**参考样例**，不是 Harness Engineering 规范的一部分。

使用方式：
1. 把本文件复制到 .github/instructions/python.instructions.md（去掉 .example 后缀）
2. 按你项目的实际栈裁剪每一节，删除不适用的条目
3. 真正的硬约束应放进 ruff + mypy + pyproject.toml，让 CI 强制
   instructions 文件只对 AI 生效，不对人/CI 生效——能让 Linter 干的事不要写在这里
4. 完成后由项目负责人评审，与 AGENTS.md 的项目身份签字位同源

Harness 不维护本样例的内容深度；详细风格请查 PEP 8、PEP 484/612/695、
Google Python Style Guide、ruff 默认规则等权威来源。
-->

# Python 编码规范

> 与 [`coding-discipline.instructions.md`](./coding-discipline.instructions.md)（流程纪律）叠加生效。
> 风格层面的硬约束以 `ruff` + `mypy` + `pyproject.toml` 为准，本文件只补充 Linter 不便强制的部分。

## 1. 命名

- 模块 / 包：[ 待填，建议 lower_snake_case，禁止短横杠 ]
- 类：[ 待填，建议 PascalCase ]
- 函数 / 变量：[ 待填，建议 lower_snake_case ]
- 常量：[ 待填，建议 UPPER_SNAKE_CASE，模块顶部声明 ]
- "私有"约定：[ 待填，建议单下划线 `_internal`；双下划线仅用于 dunder ]
- 类型变量：[ 待填，建议 PascalCase + 短，如 `T`、`UserT` ]
- 不在本文件重复 ruff `pep8-naming` 已强制的命名规则

## 2. 错误处理

- 异常分层：[ 待填，建议自定义业务异常基类，基础设施异常不上抛到 API 边界 ]
- 不裸 `except:`：必须捕具体异常类型；`except Exception:` 也要在注释里写清原因
- 不吞异常：`except` 后必须重新抛 / 写结构化日志 / 转换异常类型，三选一
- 不用异常做控制流：用 `Optional[T]` / `Result` 风格的返回值显式表达失败
- 上下文管理：所有外部资源（文件 / 连接 / 锁）用 `with` 或 `async with`，不裸 `open()` + `close()`

## 3. 类型与异步

- 类型注解：所有 public API 必须有完整 type hints；`mypy --strict` 入 CI
- 禁用 `Any`：用 `object` / `TypeVar` / `Protocol`；逃生口要配注释说明
- 数据类：[ 待填，`@dataclass` / `pydantic.BaseModel` / `TypedDict` 选一并固定使用场景 ]
- 异步：IO 用 `async`/`await`；不混用 `asyncio` 与同步阻塞调用
- 取消：长跑任务接 `asyncio.CancelledError`，不要 `time.sleep()` 阻塞事件循环

## 4. 测试

- 测试框架：[ 待填，pytest / unittest 选一，建议 pytest ]
- 测试目录：[ 待填，建议 `tests/` 与源码同级，文件名 `test_*.py` ]
- 测试函数命名：[ 待填，建议 `test_<method>_<scenario>_<expected>` ]
- Mock：[ 待填，`unittest.mock` / `pytest-mock` / `respx` 选一 ]
- Fixture：用 `@pytest.fixture`，避免 setUp/tearDown 风格
- 覆盖率门槛：见 CI 配置（不在本文件重复）

## 5. 不在此文件强制

下列项交给自动化工具强制，不写在 instructions 里：

- 缩进 / 行长 / import 排序 / f-string → `ruff format` + `ruff check --select I`
- PEP 8 命名 / 圈复杂度 → `ruff check`
- 静态类型 → `mypy --strict` 入 CI
- 包管理 / Python 版本 → `pyproject.toml` `requires-python` + `uv` / `poetry` lockfile
- 测试覆盖率 → CI 任务（`pytest --cov` + 阈值断言）
