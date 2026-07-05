# Inkwell

项目身份与定位见 [AGENTS.md](AGENTS.md)；[Harness Engineering](https://github.com/shuaihuadu/harness-engineering) 规范本体以 vendored 形式落在 [.he/](.he/) 目录。

## 技术栈

| 层级   | 选型                                                          |
| ------ | ------------------------------------------------------------- |
| 客户端 | Electron · React · Vite · TypeScript · `@xyflow/react`        |
| 后端   | .NET 10 · ASP.NET Core · Microsoft Agent Framework            |
| 存储   | EF Core（InMemory / SQL Server / PostgreSQL）· Qdrant · Redis |
| 部署   | Docker Compose（dev）· AKS + Helm（prod）                     |

完整版本号、ADR 引用与模块边界见 [AGENTS.md](AGENTS.md)。

## 文档入口

- AI 协作单一事实源：[AGENTS.md](AGENTS.md)
- 工程规范操作手册：[.he/HANDBOOK.md](.he/HANDBOOK.md)
- 阶段产出：[docs/](docs/)
- ADR 目录：[docs/03-architecture/adr/](docs/03-architecture/adr/)

## 当前状态

H2 架构已 Approved（[评审记录](docs/07-reviews/2026-05-10-h2-architecture-review.md)），准备进入 H3 详细设计；当前仓库尚无产品代码。

## 许可证

[MIT License](LICENSE)
