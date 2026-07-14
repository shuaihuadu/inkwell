# Inkwell Visual Design Lab

这是一个独立的 H1 视觉设计原型，用于比较 Inkwell 的主题、Logo、登录页和 Agent 设计页方案。它不属于产品实现，不修改 `src/app/desktop/`，也不替代已经评审的产品需求与 UI 文档。

## 技术基线

- Ant Design `6.5.1`
- Ant Design Icons `6.3.2`
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
- `/agent`：Agent 设计页的七个区段、两种密度与六种状态

右上角主题控件可以切换主题色与亮暗模式。页面中的“设计评审控件”仅用于切换原型状态，不是产品界面的一部分。

## 设计范围

本原型只探索已经进入 UI 说明的界面范围。它不实现后端调用、鉴权、文件解析、Token 生成、版本管理或 Agent 执行，也不增加注册、密码重置、工具市场、Skill Execution、多 Agent 编排等 v1 范围外功能。

品牌标记源文件为 `assets/logos/logo.svg`，由 Vite 统一打包并用于页面与 favicon。

详细覆盖关系与截图索引见 [coverage.md](coverage.md)。最终主题、Logo 和页面方案仍需人工评审决定，本原型不代表定稿。
