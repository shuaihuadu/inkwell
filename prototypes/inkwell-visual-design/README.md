# Inkwell Visual Design Lab

这是一个独立的 H1 视觉设计原型，用于比较 Inkwell 的主题、Logo、登录页和 AppShell 外壳方案。它不属于产品实现，不修改 `src/app/desktop/`，也不替代已经评审的产品需求与 UI 文档。

## 技术基线

- Ant Design `6.5.1`
- Ant Design Icons `6.3.2`
- Ant Design X `2.8.0`（Agent 对话页 / Agent 设计页内嵌对话面板专用，`peerDependencies` 要求 `antd ^6.1.1`，与本原型的 antd 6.5.1 兼容）
- React `19.2.7`
- Vite `8.1.4`
- Playwright `1.61.1`

Ant Design 与图标库版本是创建原型时 npm registry 返回的最新稳定版。

## 运行

```bash
npm install
npm run dev
```

生产构建与截图验证：

```bash
npm run build
npx playwright install chromium
npm run screenshot
```

Vite 开发服务器默认使用终端输出的本地地址。`npm run preview` 固定使用 `http://localhost:4174`，供 Playwright 使用。

## 评审入口

- `/`：Design Lab 总览
- `/themes`：三套主题及 light/dark Token 对比
- `/logos`：Inkwell 涟漪品牌标记及多尺寸预览
- `/login`：沉浸式工作台分栏与六种页面状态
- `/shell`：登录后 AppShell 外壳（顶栏 + 左侧 nav + 主区），含网络状态、全局错误条、关于弹层与完整的 Agent 库 → Agent 设计页钻取流程

右上角主题控件可以切换主题色与亮暗模式。页面中的“设计评审控件”仅用于切换原型状态，不是产品界面的一部分。

## 设计约定

以下规则适用于本原型内所有页面，不随单个页面评审结束而失效：

- **控件高度统一用 Ant Design 标准高度**（Button/Input/Select/Tabs 等），不做特殊调小/调大。
- **“紧凑”密度只能调控件之间的间距/留白**（gap、padding、margin），**不能调控件本身的高度**。AgentDesignPage 的 `density: compact` 模式以及日后新增的任何标准/紧凑切换都要按这条检查：变化的应该是间距，不是控件尺寸。
- **新增的产品页面设计统一挂载进 AppShell（`/shell`）的内容区，不再新开顶部路由**。顶部导航固定为 Design Lab / 主题 / Logo / 登录页 / AppShell 这几个“评审工具”入口，不随产品页面数量增长；具体某个产品页面长什么样，进 AppShell 之后通过其内部导航/钻取查看（例如点击 Agent 库卡片进入 Agent 设计页）。这样才能同屏看到“外壳 + 页面”的整体效果，也避免顶部菜单无限变长。

## 设计范围

本原型只探索已经进入 UI 说明的界面范围。它不实现后端调用、鉴权、文件解析、Token 生成、版本管理或 Agent 执行，也不增加注册、密码重置、工具市场、Skill Execution、多 Agent 编排等 v1 范围外功能。

品牌标记源文件为 `assets/logos/logo.svg`，由 Vite 统一打包并用于页面与 favicon。

详细覆盖关系与截图索引见 [coverage.md](coverage.md)。最终主题、Logo 和页面方案仍需人工评审决定，本原型不代表定稿。
