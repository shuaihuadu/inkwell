# 原型覆盖矩阵

## 主题色

路由：`/themes`

- 碧海青：light / dark
- 朱砂橙：light / dark
- 曜石紫：light / dark
- 单主题详览与三主题对比
- Button、Input、Select、Tag、Badge、Alert、Table、Progress、Slider、Rate 等真实 Ant Design 组件
- Primary、Info、Success、Warning、Error、Text、Border、Container、Layout 等 Token 可视化

## Logo

路由：`/logos`

- `assets/logos/inkwell-mark.svg`：Inkwell 曜石紫涟漪品牌标记
- 每个方向覆盖 16、24、32、64、96 px
- 每个方向覆盖浅底、深底、主色底和应用图标裁切预览
- 每个方向列出设计意图和小尺寸风险

## 登录页

路由：`/login`

构图方向：沉浸式工作台分栏

状态：

- 默认
- 提交中
- 账号或密码错误
- 账号已锁
- 速率超限
- 离线

该方案覆盖账号、密码、显示密码、登录按钮、管理员联系提示、版本和 Build 信息。原型不提供注册与密码重置入口。

## Agent 设计页

路由：`/agent`

区段：

1. 基础信息
2. Instructions
3. 模型与参数
4. 工具
5. Skills
6. 长期记忆
7. 版本与调试

布局密度：

- 标准密度
- 紧凑密度

状态：

- 编辑中
- 新建草稿
- 只读（非 Owner）
- 提交中
- 提交失败
- 提交成功

只读与提交中状态会禁用编辑控件；提交失败与提交成功状态有明确反馈。顶部删除操作包含二次确认；“开始对话”会打开右侧会话抽屉，可选择推荐问题、输入并发送消息。

## 视口与截图

Playwright 配置覆盖：

- 1440 × 900：desktop-hd
- 1280 × 800：desktop-md
- 768 × 720：tablet
- 390 × 844：mobile

`npm run screenshot` 生成以下评审证据到 `screenshots/`：

- `01`–`02`：Design Lab 桌面与移动端
- `03`–`04`：三主题对比与平板视口
- `05`：正式 Logo 桌面视图
- `06`–`07`：工作台分栏桌面与移动视图
- `08`–`09`：Agent 会话抽屉与平板视图

截图测试同时检查主题、Logo、登录与 Agent 关键交互，以及页面横向溢出。表格允许自身横向滚动，但页面根节点不得产生横向滚动。

## 人工评审项

- 从三套主题中选择主方向，并决定是否保留独立暗色 Token
- 确认正式 Logo 在主题色、深色背景与小尺寸下的适配方式
- 确认工作台分栏在窄窗口下的降级方式
- 在 Agent 标准密度与紧凑密度之间确定默认值
- 决定 Agent 区段导航在窄窗口下采用折叠侧栏还是后续改为其他导航方式
