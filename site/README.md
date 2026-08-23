# Inkwell Site

Inkwell 的 GitHub Pages 项目，使用 Astro 生成纯静态站点。

## 本地开发

```bash
npm --prefix site ci
npm --prefix site run dev
```

默认中文地址为 <http://localhost:4321/inkwell/>，英文地址为 <http://localhost:4321/inkwell/en/>。两条路由在构建时生成独立静态页面，并通过顶栏语言入口互相切换。

## 构建

```bash
npm --prefix site run build
```

构建产物位于 `site/dist/`。`astro.config.mjs` 中的 `base` 固定为 `/inkwell`，对应 GitHub Pages 项目站点路径。

## 截图

首页使用 `site/src/assets/screenshots/` 中的官网专用 Retina 截图。这些图片由可交互视觉原型按固定产品视口生成，并移除了 Design Lab 外壳；Astro 在构建时处理图片并写入静态产物，站点运行时不依赖原型服务。

`.github/workflows/pages.yml` 会在 `main` 分支的站点文件发生变化时自动构建并部署。首次发布前，需要在仓库 Settings -> Pages 中将 Source 设置为 **GitHub Actions**。
