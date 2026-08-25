export const supportedLocales = ["zh-CN", "en-US"] as const;

export type AppLocale = (typeof supportedLocales)[number];

export type LocalePreference = AppLocale | "system";

export const defaultLocale: AppLocale = "zh-CN";

export const resources = {
    "zh-CN": {
        translation: {
            common: {
                add: "添加",
                cancel: "取消",
                close: "关闭",
                confirm: "确认",
                copy: "复制",
                delete: "删除",
                edit: "编辑",
                finish: "完成",
                itemCount: "共 {{count}} 项",
                listSeparator: "、",
                refresh: "刷新",
                retry: "重试",
                save: "保存",
                search: "搜索",
                unknown: "未知",
                unknownError: "未知错误",
                unavailable: "未提供",
                view: "查看",
                yes: "是",
                no: "否",
            },
            locale: {
                label: "界面语言",
                chinese: "简体中文",
                english: "English",
                system: "跟随系统",
            },
            auth: {
                platform: "Inkwell Agent 平台",
                usernamePlaceholder: "请输入账号",
                usernameRequired: "请输入账号",
                usernameTooLong: "账号长度不超过 64",
                passwordPlaceholder: "请输入密码",
                passwordRequired: "请输入密码",
                login: "登录",
                loggingIn: "登录中…",
                help: "如忘记密码或需要开通账号，请联系系统管理员",
                errors: {
                    invalidCredentials: "账号或密码错误，请重试",
                    accountLocked: "账号已被锁定，请联系系统管理员",
                    rateLimited: "登录过于频繁，请稍后重试",
                    offline: "网络异常，已断开。请检查网络连接",
                    unknown: "登录失败，请稍后重试",
                },
                changePassword: {
                    title: "修改密码",
                    currentPassword: "当前密码",
                    currentPasswordRequired: "请输入当前密码",
                    currentPasswordPlaceholder: "输入当前密码",
                    newPassword: "新密码",
                    newPasswordRequired: "请输入新密码",
                    newPasswordPlaceholder: "输入新密码",
                    newPasswordHelp:
                        "使用 8–128 个字符，且不能与当前密码相同。",
                    passwordLength: "密码长度应为 8–128 个字符",
                    passwordUnchanged: "新密码不能与当前密码相同",
                    confirmPassword: "确认新密码",
                    confirmPasswordRequired: "请再次输入新密码",
                    confirmPasswordPlaceholder: "再次输入新密码",
                    passwordMismatch: "两次输入的新密码不一致",
                    submit: "修改密码",
                    success: "密码已修改",
                    failed: "修改失败：{{message}}",
                },
                lock: {
                    title: "Inkwell 已锁定",
                    continueAs: "{{username}}，请输入密码继续",
                    passwordRequired: "请输入密码",
                    passwordPlaceholder: "密码",
                    unlock: "解锁",
                    switchAccount: "切换账号",
                    logout: "登出",
                    errors: {
                        invalidPassword: "密码错误，请重试",
                        accountLocked: "账号已被锁定，请联系系统管理员",
                        offline: "网络异常，已断开。请检查网络连接",
                        unknown: "解锁失败，请稍后重试",
                    },
                },
            },
            shell: {
                navigation: {
                    main: "主导航",
                    workspace: "工作区",
                    agentSpace: "Agent 空间",
                    resources: "资源中心",
                    tools: "工具",
                    skills: "Skills",
                    models: "模型",
                    system: "系统管理",
                    users: "用户管理",
                },
                aboutInkwell: "关于 Inkwell",
                appearanceSwitch: "切换外观",
                connection: {
                    online: "后台服务正常",
                    reconnecting: "重连中",
                    offline: "后台服务异常",
                },
                errors: {
                    offline:
                        "网络连接已断开，正在尝试重新连接。写操作已禁用，恢复后自动收起",
                    reconnecting: "正在连接后台服务，写操作暂不可用",
                    "rate-limited": "操作过于频繁，请稍后再试",
                    rateLimitedWithRetry:
                        "操作过于频繁，请在 {{seconds}} 秒后重试",
                    "service-unavailable": "后台服务暂时不可用，请稍后重试",
                },
                guide: "使用指南",
                quickStart: "快速开始",
                faq: "常见问题",
                help: "帮助",
                settings: "个人设置",
                changePassword: "修改密码",
                administration: "管理",
                logout: "登出",
                openUserMenu: "打开用户菜单",
                comingSoon: "即将上线",
                placeholderEntry: "占位入口 · 即将上线",
                quickStartDescription:
                    "完成这些关键步骤，建立从配置到团队使用的完整工作流。",
                quickStartSteps: {
                    create: {
                        title: "创建一个 Agent",
                        description: "填写名称和用途",
                    },
                    configure: {
                        title: "完成核心配置",
                        description: "补充 Instructions 并选择模型",
                    },
                    run: {
                        title: "进行一次试运行",
                        description: "用真实问题检查回答",
                    },
                    publish: {
                        title: "发布第一个版本",
                        description: "生成可用于对话的正式版本",
                    },
                    share: {
                        title: "按需共享给团队",
                        description: "允许成员只读查看和使用",
                    },
                },
                goToAgentSpace: "前往 Agent 空间",
                about: {
                    version: "版本",
                    buildNumber: "构建号",
                    commit: "提交",
                    qrAlt: "公众号二维码",
                    followAuthor: "扫码关注作者公众号",
                },
                appearanceMode: "外观模式",
                light: "亮色",
                dark: "暗色",
                system: "跟随系统",
                themeColor: "主题色",
                themes: {
                    amethyst: "曜石紫",
                    terracotta: "朱砂橙",
                    teal: "碧海青",
                },
            },
            editor: {
                unsavedTitle: "有未保存的修改",
                unsavedContent: "离开后，本次修改将丢失。",
                leave: "仍然离开",
                continueEditing: "继续编辑",
            },
            agents: {
                space: {
                    title: "Agent 空间",
                    create: "新建 Agent",
                    mine: "我的",
                    shared: "团队共享",
                    refreshLabel: "刷新 Agent",
                    searchPlaceholder: "搜索 Agent",
                    filters: {
                        all: "全部 {{count}}",
                        published: "已发布 {{count}}",
                        draft: "草稿 {{count}}",
                    },
                    configComingSoon: "Agent 配置页将在下一项工作中接入。",
                    loadError: "加载失败，请检查网络后重试",
                    actionSuccess: {
                        share: "Agent 已共享给团队",
                        unshare: "已撤销团队共享",
                        revoke: "已由管理员撤销共享",
                    },
                    actionFailed: "Agent 操作失败。",
                    actions: {
                        edit: "编辑配置",
                        editLabel: "编辑 {{name}}",
                        share: "共享已发布版本",
                        shareLabel: "共享 {{name}}",
                        revoke: "撤销共享",
                        revokeLabel: "撤销 {{name}} 共享",
                        view: "查看详情",
                        viewLabel: "查看 {{name}} 详情",
                    },
                    status: {
                        draft: "草稿",
                        shared: "已共享",
                        unpublished: "未发布",
                        unpublishedChanges: "有未发布的修改",
                    },
                    noDescription: "暂无描述",
                    empty: {
                        filtered: "没有符合条件的 Agent",
                        mine: "还没有自己的 Agent，点击“新建 Agent”开始",
                        shared: "团队成员还没有共享 Agent",
                    },
                    dialogs: {
                        revokeTitle: "撤销「{{name}}」的共享",
                        revokeContent:
                            "撤销后，其他成员将无法继续访问；Owner 原件不会被删除。",
                        confirmRevoke: "确认撤销",
                        shareTitle: "共享「{{name}}」",
                        shareContent:
                            "共享后，团队成员可以查看并使用该 Agent 的已发布版本。",
                        confirmShare: "确认共享",
                    },
                },
                editor: {
                    sections: {
                        basic: "基础信息",
                        instructions: "Instructions",
                        model: "模型与参数",
                        tools: "工具",
                        skills: "Skills",
                        version: "版本",
                    },
                    messages: {
                        saved: "已存为草稿，未影响已发布版本",
                        saveFailed: "草稿保存失败。",
                        avatarUploaded: "头像已上传，请保存 Agent",
                        avatarUploadFailed: "头像上传失败。",
                        published: "已发布为 v{{version}}",
                        publishFailed: "发布失败，草稿已保留。",
                        cloned: "已复制为我的 Agent",
                        cloneFailed: "复制 Agent 失败。",
                        rolledBack: "已回滚，生成新版本 v{{version}}",
                        rollbackFailed: "回滚失败。",
                        deleted: "Agent 已删除",
                        deleteFailed: "删除 Agent 失败。",
                    },
                    deleteDialog: {
                        title: "删除「{{name}}」？",
                        content:
                            "将永久删除该 Agent、全部版本及相关会话历史，操作不可恢复。",
                        confirm: "确认删除",
                    },
                    loadFailed: "Agent 加载失败",
                    backLabel: "返回 Agent 空间",
                    newAgent: "新建 Agent",
                    versionOwner: "v{{version}} · Owner: {{owner}}",
                    notSaved: "尚未保存",
                    delete: "删除 Agent",
                    trial: "试运行",
                    publish: "发布",
                    copyToMine: "复制为我的 Agent",
                    readonly: "这是其他成员共享的 Agent，当前为只读模式。",
                    navigationLabel: "Agent 配置区段",
                    expandNavigation: "展开配置导航",
                    collapseNavigation: "收起配置导航",
                    instructionsHelp:
                        "给 Agent 的系统指令。支持 Markdown，超过 32K 字符时给出警告。",
                    instructionsHelpLabel: "系统指令说明",
                    publishDialog: {
                        title: "发布新版本",
                        description:
                            "发布后将成为新的正式版本，正在使用该 Agent 的对话会从下一轮开始使用新版本。",
                        changeSummary: "变更说明（可选）",
                        changeSummaryPlaceholder:
                            "说明本次修改的内容，会记录到版本历史里",
                        shareAfterPublish: "发布后共享给团队",
                        shareHint: "团队成员将可以查看并使用本次发布的新版本。",
                    },
                    basic: {
                        avatar: "头像",
                        changeAvatar: "更换头像",
                        changeAvatarLabel: "更换 Agent 头像",
                        avatarReadonly: "Agent 头像",
                        clickToChange: "点击更换",
                        name: "Agent 名称",
                        nameRequired: "请输入 Agent 名称",
                        nameTooLong: "名称不能超过 50 个字符",
                        namePlaceholder: "Agent 名称（1–50 字符）",
                        description: "描述",
                        descriptionPlaceholder:
                            "简要描述这个 Agent 的职责和能力",
                        teamVisible: "团队可见",
                        teamVisibleHelp:
                            "对应 Agent 管理态的 IsShared，不随版本快照回滚。",
                        shared: "已共享",
                        private: "仅自己",
                    },
                    bindings: {
                        noTools: "统一工具管理中暂无可用工具",
                        skillsDescription:
                            "从统一 Skill 管理中选择需要挂载到当前 Agent 的 Skill。",
                        noSkills: "统一 Skill 管理中暂无可用 Skill",
                    },
                    version: {
                        none: "此 Agent 尚未发布",
                        rollbackTitle: "回滚到 v{{version}}？",
                        rollbackContent:
                            "将基于该历史版本生成一个新的发布版本，不会覆盖版本历史。",
                        confirmRollback: "确认回滚",
                        history: "版本历史",
                        historyDescription:
                            "发布和回滚会生成不可变版本；保存草稿不会出现在这里。",
                        compare: "版本对比",
                        columns: {
                            version: "版本",
                            status: "状态",
                            savedTime: "保存时间",
                            savedBy: "保存人",
                            summary: "变更摘要",
                            actions: "操作",
                        },
                        published: "已发布",
                        historical: "历史版本",
                        previousCompare: "与上一版比较",
                        latestCompare: "与最新版比较",
                        rollbackThis: "回滚到本版",
                        noSummary: "无摘要",
                        versus: "对比",
                        field: "字段",
                        identical: "两个版本的配置完全相同。",
                        emptyValue: "无",
                        defaultValue: "默认",
                        snapshot: {
                            basic: "基础信息",
                            name: "名称",
                            avatar: "头像",
                            description: "描述",
                            instructions: "Instructions",
                            model: "模型与参数",
                            modelId: "模型",
                            chatHistory: "对话历史配置",
                            capabilities: "能力",
                            tools: "工具",
                            skills: "Skills",
                        },
                    },
                    instructions: {
                        tooLong: "Instructions 较长，可能挤占模型上下文",
                        empty: "暂无 Instructions",
                    },
                    model: {
                        runtime: "运行模型",
                        runtimeRequired: "请选择运行模型",
                        vision: "视觉",
                        tools: "工具",
                        structuredOutput: "结构化输出",
                        generationParameters: "生成参数",
                        generationDescription:
                            "关闭“自定义”后使用模型默认值，不写入请求参数。",
                        temperature: "温度",
                        temperatureDescription: "控制输出的随机性",
                        topP: "Top P",
                        topPDescription: "限制候选词概率范围",
                        maxTokens: "最大 Token 数",
                        maxTokensDescription: "限制单次输出长度",
                        context: "上下文",
                        contextDescription:
                            "超过该数量时，最早的历史消息会被裁剪，避免无限增长挤占模型上下文。",
                        maxMessages: "最大消息记录数",
                        messageUnit: "条",
                        custom: "自定义",
                        default: "默认",
                    },
                    state: {
                        unsaved: "未保存",
                        draft: "未发布的草稿",
                        changed: "有未发布的修改",
                        published: "已发布",
                    },
                },
                details: {
                    title: "Agent 详情",
                    closeLabel: "关闭 Agent 详情",
                    version: "版本：v{{version}}",
                    noDescription: "暂无描述",
                    instructionsCopied: "Instructions 已复制",
                    copyFailed: "复制失败，请重试",
                    overview: "版本概览",
                    changeSummary: "变更摘要",
                    noSummary: "无",
                    runtimeModel: "运行模型",
                    notConfigured: "未配置",
                    characterCount: "{{count}} 字符",
                    copyInstructions: "复制 Instructions",
                    collapse: "收起",
                    expand: "展开全文",
                    modelAndContext: "模型与上下文",
                    historyLimit: "历史消息上限",
                    tools: "工具",
                    noTools: "未挂载工具",
                    noSkills: "未挂载 Skill",
                    defaultValue: "默认",
                },
            },
            chat: {
                composer: {
                    placeholder: "输入消息，Enter 发送，Shift + Enter 换行",
                    attachmentUnavailable: "附件发送暂未开放",
                    addAttachment: "添加附件",
                },
                prompts: {
                    capabilities: {
                        label: "了解能力",
                        description:
                            "请介绍你能提供哪些帮助，并给出几个具体示例",
                    },
                    tasks: {
                        label: "推荐任务",
                        description: "根据你的能力，推荐三个适合立即开始的任务",
                    },
                    questionTips: {
                        label: "提问建议",
                        description: "告诉我怎样提问能让你给出更好的回答",
                    },
                },
                messages: {
                    stopped: "已停止生成",
                    retryLabel: "重试失败消息",
                    regenerate: "重新生成",
                    copyLabel: "复制第 {{number}} 条消息",
                    feedbackLabel: "评价第 {{number}} 条消息",
                },
                usage: {
                    label: "Token 用量",
                    summaryLabel: "用量",
                    input: "输入",
                    output: "输出",
                    total: "总计",
                    tokens: "tokens",
                },
                activity: {
                    title: {
                        loaded: "加载 Skill：{{name}}",
                        resource: "读取资源：{{name}}",
                        script: "运行脚本：{{name}}",
                        tool: "调用工具：{{name}}",
                    },
                    readInstructions: "读取 Skill 指令",
                    fromSkill: "来自 {{name}}",
                    toolFunction: "函数 {{name}}",
                    status: {
                        loading: "调用中",
                        failed: "调用失败",
                        stopped: "已停止",
                        loaded: "加载成功",
                        success: "调用成功",
                    },
                    parameters: "参数",
                    calls: "工具调用",
                    failedCount: "{{count}} 项失败",
                    stoppedCount: "{{count}} 项已停止",
                    completedCount: "{{count}} 项已完成",
                },
                panel: {
                    newConversation: "新会话",
                    historyGroup: "历史会话",
                    errors: {
                        historyLoad: "会话历史加载失败。",
                        historyRefresh: "会话历史刷新失败。",
                        agentCall: "Agent 调用失败。",
                        messagesLoad: "会话消息加载失败。",
                        deleteConversation: "删除会话失败。",
                        clearConversation: "清空会话失败。",
                    },
                    deleteDialog: {
                        title: "删除这个会话？",
                        content: "会话及其全部消息将被永久删除，操作不可恢复。",
                        confirm: "确认删除",
                    },
                    clearDialog: {
                        title: "清空当前会话？",
                        content:
                            "全部消息将被永久删除，但会话仍保留在历史列表中。",
                        confirm: "确认清空",
                    },
                    selectAgent: "选择一个 Agent 开始对话",
                    back: "返回 Agent 空间",
                    model: "模型：{{model}}",
                    modelNotConfigured: "未配置",
                    versionLoading: "版本加载中",
                    version: "版本：v{{version}}",
                    viewDetails: "查看 Agent 详情",
                    clearConversation: "清空当前会话",
                    scrollToLatest: "滚动到最新消息",
                    conversations: "会话",
                    expandConversations: "展开会话",
                    collapseConversations: "收起会话",
                    createConversation: "新建会话",
                    currentDraft: "当前草稿",
                    publishedVersion: "已发布 v{{version}}",
                    newTrial: "新建试运行会话",
                    trialList: "试运行会话列表",
                    closeTrial: "关闭试运行",
                    trialWelcomeTitle: "从一个研究问题开始",
                    trialWelcomeDescription:
                        "我可以协助检索资料、梳理证据并生成结构化报告。",
                    startConversation: "开始新的对话",
                    publishedMessage: "消息将由当前 Agent 的已发布版本处理。",
                    usingDraft: "当前使用已保存草稿",
                    usingPublished: "当前使用已发布版本 v{{version}}",
                },
            },
            tools: {
                title: "工具",
                description:
                    "查看 Agent 可使用的工具。工具帮助 Agent 查询信息、调用服务或完成具体操作。",
                refreshLabel: "刷新工具",
                searchPlaceholder: "搜索名称或描述",
                loadFailed: "工具列表加载失败，请稍后重试",
                empty: "当前没有已注册的工具",
                filteredEmpty: "没有匹配的工具，请清除搜索条件",
                columns: {
                    name: "名称",
                    description: "描述",
                    parameters: "参数",
                    updatedTime: "更新时间",
                    actions: "操作",
                },
                parameterCount: "{{count}} 项",
                viewLabel: "查看 {{name}}",
                details: {
                    title: "Tool 详情",
                    closeLabel: "关闭 Tool 详情",
                    parameterCount: "{{count}} 个参数",
                    parameters: "参数",
                    noParameters: "此工具没有参数",
                    type: "类型",
                    required: "必填",
                    allowedValues: "可选值",
                    rawSchema: "原始 JSON Schema",
                    viewSchema: "查看原始 Schema",
                    timeInformation: "时间信息",
                },
            },
            models: {
                categories: {
                    Unknown: "未知",
                    Chat: "对话",
                    Embedding: "嵌入",
                    ImageGeneration: "图像生成",
                    VideoGeneration: "视频生成",
                },
                capability: {
                    supported: "支持",
                    unsupported: "不支持",
                },
                test: {
                    success: "{{model}} {{category}}最小请求成功 · {{latency}}",
                    failed: "模型连接测试失败",
                    action: "测试",
                    actionLabel: "测试 {{name}}",
                    failureStatus: "失败",
                },
                title: "模型",
                description:
                    "查看 LiteLLM 实时发现的模型与能力。此列表只读，模型配置在 LiteLLM 中维护。",
                management: "模型管理",
                filterLabel: "筛选模型类型",
                allTypes: "全部类型",
                refreshLabel: "刷新模型",
                searchPlaceholder: "搜索模型标识或提供方",
                loadFailed: "无法读取 LiteLLM 模型，请重试",
                empty: "LiteLLM 当前未返回模型",
                filteredEmpty: "在所选条件内没有结果，请清除筛选",
                columns: {
                    id: "模型标识",
                    type: "模型类型",
                    provider: "提供方",
                    tokenLimit: "Token 上限",
                    tokenSummary: "输入 {{input}} / 输出 {{output}}",
                    vision: "视觉",
                    tools: "工具",
                    structured: "结构化",
                    reasoning: "推理",
                    actions: "操作",
                },
                viewLabel: "查看 {{name}}",
                details: {
                    title: "模型详情",
                    closeLabel: "关闭模型详情",
                    unknownMode: "模式未知",
                    unknownProvider: "提供方未知",
                    information: "模型信息",
                    modelId: "模型 ID",
                    category: "模型类型",
                    providerMode: "提供方模式",
                    ownedBy: "所有者",
                    tokenLimit: "Token 上限",
                    maxInput: "最大输入",
                    maxOutput: "最大输出",
                    tokens: "Token",
                    capabilities: "能力",
                    toolCalls: "工具调用",
                    structuredOutput: "结构化输出",
                    dataSourceDescription:
                        "数据来自 LiteLLM 实时发现，不在 Inkwell 中保存副本。",
                },
            },
            skills: {
                validation: {
                    namePattern:
                        "机器名称只能包含小写字母、数字和单个连字符，且不能以连字符开头或结尾",
                    missingFrontmatter: "SKILL.md 缺少有效的 YAML frontmatter",
                    missingFields: "SKILL.md 必须包含名称和描述",
                    invalidName: "SKILL.md {{message}}",
                    archiveSkillFile: "压缩包必须包含且只包含一个 SKILL.md",
                    filesOutsideRoot: "所有文件必须位于 SKILL.md 所在文件夹内",
                    unsupportedFolder:
                        "包内文件只能放在 references、assets 或 scripts 文件夹",
                },
                owner: {
                    me: "我",
                    others: "其他成员",
                    mine: "我上传的",
                },
                messages: {
                    saved: "Skill 已保存",
                    saveFailed: "Skill 保存失败，请稍后重试",
                    deleted: "Skill 已删除",
                    deleteFailed: "Skill 删除失败，请稍后重试",
                    parseFailed: "Skill 文件无法解析",
                    uploaded: "Skill 已上传",
                    uploadFailed: "Skill 上传失败，请检查文件结构",
                },
                title: "Skills",
                description:
                    "查看和管理 Agent 的 Skill。Skill 通过任务说明和参考资料，教 Agent 如何完成特定工作。",
                upload: "上传 Skill",
                filters: {
                    all: "全部归属",
                    mine: "我上传的",
                    others: "其他成员",
                },
                refreshLabel: "刷新 Skills",
                searchPlaceholder: "搜索名称、描述或所有者",
                loadFailed: "Skills 加载失败，请稍后重试",
                empty: "还没有 Skill",
                filteredEmpty: "在所选条件内没有结果，请清除筛选",
                columns: {
                    name: "名称",
                    description: "描述",
                    owner: "所有者",
                    resources: "资料",
                    updatedTime: "更新时间",
                    actions: "操作",
                },
                resourceSummary:
                    "{{references}} 引用 · {{assets}} 素材 · {{scripts}} 脚本",
                actions: {
                    viewLabel: "查看 {{name}}",
                    editLabel: "编辑 {{name}}",
                    deleteLabel: "删除 {{name}}",
                },
                deleteDialog: {
                    title: "删除「{{name}}」",
                    content:
                        "删除后，新配置将无法再选择此 Skill；已保存草稿与已发布版本继续使用各自 Snapshot。确认删除？",
                    confirm: "确认删除",
                },
                details: {
                    editTitle: "编辑 Skill",
                    title: "Skill 详情",
                    closeLabel: "关闭 Skill 详情",
                    machineName: "机器名称",
                    machineNameHelp:
                        "用于 Skill 发现和 load_skill 调用，格式为 kebab-case。",
                    machineNameRequired: "请输入机器名称",
                    machineNameTooLong: "机器名称不能超过 64 个字符",
                    machineNamePlaceholder: "例如 code-review",
                    descriptionRequired: "请输入描述",
                    content: "SKILL.md 内容",
                    contentRequired: "请输入内容",
                    owner: "所有者：{{owner}}",
                    referenceCount: "引用：{{count}} 个（只读）",
                    assetCount: "素材：{{count}} 个（只读）",
                    scriptCount: "脚本：{{count}} 个（只读）",
                    characterCount: "{{count}} 字符",
                    collapse: "收起",
                    expand: "展开全文",
                    resources: "资源",
                    references: "References",
                    assets: "Assets",
                    scripts: "Scripts",
                    timeInformation: "时间信息",
                    createdTime: "创建时间",
                    updatedTime: "更新时间",
                },
                uploadDialog: {
                    title: "上传 Skill",
                    start: "开始上传",
                    selectFile: "选择 Skill 文件夹压缩包或 SKILL.md",
                    hint: "名称和描述读取自 SKILL.md 的 YAML frontmatter，上传后可在详情中编辑。支持 references/、assets/ 和 scripts/。",
                    parsing: "正在解析 SKILL.md...",
                    preview: "SKILL.md 解析预览",
                    machineName: "机器名称",
                    description: "描述",
                    packageResources: "包内资源",
                    packageSummary:
                        "{{references}} 个 references · {{assets}} 个 asset · {{scripts}} 个 scripts",
                },
            },
            users: {
                neverLoggedIn: "从未登录",
                errors: {
                    add: "添加失败：{{message}}",
                    action: "操作失败：{{message}}",
                    reset: "重置失败：{{message}}",
                },
                actionSuccess: {
                    unlock: "{{username}} 已解锁",
                    disable: "{{username}} 已禁用",
                    enable: "{{username}} 已启用",
                },
                status: {
                    active: "正常",
                    locked: "已锁定",
                    disabled: "已禁用",
                },
                confirmations: {
                    unlock: {
                        title: "解锁账号 {{username}}",
                        content: "解锁后，该用户可以立即重新尝试登录。",
                        confirm: "确认解锁",
                    },
                    disable: {
                        title: "禁用账号 {{username}}",
                        content:
                            "禁用是持续的管理状态。该用户将无法登录，且需要管理员重新启用。",
                        confirm: "确认禁用",
                    },
                    enable: {
                        title: "启用账号 {{username}}",
                        content: "启用后，该用户可以使用原密码登录。",
                        confirm: "确认启用",
                    },
                    reset: {
                        title: "重置 {{username}} 的密码",
                        content:
                            "重置后，原密码立即失效。系统将生成仅显示一次的临时密码。",
                        confirm: "确认重置",
                    },
                },
                title: "用户管理",
                description: "添加用户，重置密码，并管理账号的锁定或禁用状态。",
                add: "添加用户",
                filters: {
                    statusLabel: "筛选账号状态",
                    allStatus: "全部状态",
                    roleLabel: "筛选账号角色",
                    allRoles: "全部角色",
                },
                refreshLabel: "刷新用户",
                searchPlaceholder: "搜索用户名",
                loadFailed: "用户列表加载失败，请重试",
                empty: "当前没有用户",
                filteredEmpty: "在所选条件内没有结果，请清除筛选",
                columns: {
                    username: "用户名",
                    role: "角色",
                    status: "状态",
                    lastLogin: "最后登录",
                    createdTime: "创建时间",
                    actions: "操作",
                },
                manage: "管理",
                manageLabel: "管理 {{username}}",
                create: {
                    successTitle: "用户已添加",
                    title: "添加用户",
                    created: "{{username}} 已创建",
                    temporaryPasswordNotice:
                        "请立即将临时密码交给该用户。关闭此窗口后，临时密码将不再显示。",
                    temporaryPassword: "临时密码",
                    mustChangePassword: "用户首次登录后必须设置新密码。",
                    usernameRequired: "请输入用户名",
                    usernameTooLong: "用户名不能超过 100 个字符",
                    usernameExists: "用户名已存在",
                    usernamePlaceholder: "输入用户名",
                    role: "角色",
                    adminWarning:
                        "Admin 可以管理部署内的用户和共享资源，请仅授予可信人员。",
                    passwordHint:
                        "系统会生成一次性临时密码，并在创建成功后显示。",
                },
                management: {
                    title: "管理用户",
                    titleWithName: "管理用户 · {{username}}",
                    resetSuccess: "{{username}} 的密码已重置",
                    resetNotice:
                        "请立即将临时密码交给该用户。关闭窗口后将不再显示。",
                    nextLoginChange: "用户下次登录时必须设置新密码。",
                    password: "密码",
                    passwordDescription:
                        "生成一次性临时密码，并要求用户下次登录时设置新密码。",
                    resetPassword: "重置密码",
                    loginStatus: "登录状态",
                    currentAccount: "不能禁用当前登录账号。",
                    autoLocked: "该账号因登录失败次数过多被系统自动锁定。",
                    unlock: "解锁",
                    enable: "启用用户",
                    disable: "禁用用户",
                },
            },
            guide: {
                title: "使用指南",
                searchPlaceholder: "搜索指南",
                navigationLabel: "使用指南章节",
                noMatches: "没有匹配的指南",
                sections: {
                    quickStart: {
                        label: "快速开始",
                        description: "从创建到共享，完成第一个 Agent。",
                    },
                    create: {
                        label: "创建与配置",
                        description:
                            "设置基础信息、Instructions、模型、Tools 和 Skills。",
                    },
                    publish: {
                        label: "保存与发布",
                        description: "理解草稿、已发布版本和未发布修改。",
                    },
                    share: {
                        label: "共享与复制",
                        description:
                            "把已发布 Agent 交给团队使用，或复制独立副本。",
                    },
                    faq: {
                        label: "常见问题",
                        description: "快速处理版本、共享和权限相关疑问。",
                    },
                },
                quickStart: {
                    eyebrow: "大约 5 分钟",
                    title: "创建并发布第一个 Agent",
                    description:
                        "沿着一条完整路径认识 Inkwell。每一步都可以稍后返回，不会强制锁定操作顺序。",
                    steps: {
                        create: {
                            title: "创建 Agent",
                            content: "填写名称和用途，建立一个未发布草稿。",
                        },
                        configure: {
                            title: "完成核心配置",
                            content: "补充 Instructions，并选择运行模型。",
                        },
                        trial: {
                            title: "试运行",
                            content: "用真实问题检查回答是否符合预期。",
                        },
                        publish: {
                            title: "发布版本",
                            content: "把当前草稿固化为可用于对话的正式版本。",
                        },
                        share: {
                            title: "按需共享",
                            content: "允许团队成员只读查看和使用已发布版本。",
                        },
                    },
                    open: "打开快速开始",
                    goToAgents: "前往 Agent 空间",
                },
                create: {
                    eyebrow: "Agent 配置",
                    title: "先定义职责，再补充能力",
                    description:
                        "建议先用最小配置完成一次试运行，再逐步挂载 Tools 与 Skills。",
                    items: {
                        basics: {
                            title: "基础信息",
                            description:
                                "用清晰名称和简短描述说明 Agent 的职责边界。",
                        },
                        instructions: {
                            title: "Instructions",
                            description:
                                "写明目标、约束、输出格式和无法完成时的处理方式。",
                        },
                        model: {
                            title: "模型与参数",
                            description:
                                "选择运行模型；没有明确原因时保留默认生成参数。",
                        },
                        capabilities: {
                            title: "Tools 与 Skills",
                            description:
                                "只挂载任务真正需要的能力，减少不确定行为。",
                        },
                    },
                    trialNote:
                        "试运行使用当前已保存配置；正式对话始终使用已发布版本。",
                },
                publish: {
                    eyebrow: "版本生命周期",
                    title: "保存草稿不等于发布",
                    description:
                        "草稿用于继续编辑，发布用于生成新的正式版本。两者不会互相替代。",
                    items: {
                        save: {
                            title: "保存",
                            description:
                                "保存当前配置为草稿，不影响已发布版本和进行中的对话。",
                        },
                        publish: {
                            title: "发布",
                            description:
                                "把草稿固化成新版本；新的对话轮次开始使用该版本。",
                        },
                        changes: {
                            title: "有未发布的修改",
                            description:
                                "说明已发布版本仍可用，但当前草稿包含更新。",
                        },
                    },
                    draft: "未发布的草稿",
                    changed: "有未发布的修改",
                    published: "已发布",
                },
                share: {
                    eyebrow: "团队协作",
                    title: "共享使用权，不共享编辑权",
                    description:
                        "团队成员可以查看和使用已发布版本，但不能修改 Owner 的 Agent。",
                    items: {
                        share: {
                            title: "共享",
                            description:
                                "只暴露当前已发布版本；未发布修改不会提前生效。",
                        },
                        revoke: {
                            title: "撤销共享",
                            description:
                                "团队成员失去访问权限，Owner 原件和配置不会被删除。",
                        },
                        copy: {
                            title: "复制为我的 Agent",
                            description:
                                "创建独立副本，复制者成为新副本的 Owner。",
                        },
                    },
                },
                faq: {
                    eyebrow: "常见问题",
                    title: "快速找到当前状态的含义",
                    description:
                        "这些问题覆盖 Agent 创建、版本和团队共享中的常见困惑。",
                    items: {
                        open: {
                            title: "为什么卡片点击进入对话？",
                            description:
                                "已发布 Agent 的主要任务是使用；通过卡片操作进入编辑或只读详情。",
                        },
                        edit: {
                            title: "为什么团队成员不能编辑共享 Agent？",
                            description:
                                "共享只授予查看和使用权限；需要修改时请复制为自己的 Agent。",
                        },
                        revoke: {
                            title: "撤销共享会删除 Agent 吗？",
                            description:
                                "不会。它只移除团队可见性，Owner 的原件和版本历史保持不变。",
                        },
                        changes: {
                            title: "修改后为什么对话没有变化？",
                            description:
                                "保存草稿不会影响正式对话，需要发布新版本后才会生效。",
                        },
                    },
                },
            },
        },
    },
    "en-US": {
        translation: {
            common: {
                add: "Add",
                cancel: "Cancel",
                close: "Close",
                confirm: "Confirm",
                copy: "Copy",
                delete: "Delete",
                edit: "Edit",
                finish: "Done",
                itemCount: "{{count}} item",
                itemCount_other: "{{count}} items",
                listSeparator: ", ",
                refresh: "Refresh",
                retry: "Retry",
                save: "Save",
                search: "Search",
                unknown: "Unknown",
                unknownError: "Unknown error",
                unavailable: "Not provided",
                view: "View",
                yes: "Yes",
                no: "No",
            },
            locale: {
                label: "Display language",
                chinese: "简体中文",
                english: "English",
                system: "System",
            },
            auth: {
                platform: "Inkwell Agent Platform",
                usernamePlaceholder: "Enter your username",
                usernameRequired: "Enter your username",
                usernameTooLong: "Username must be 64 characters or fewer",
                passwordPlaceholder: "Enter your password",
                passwordRequired: "Enter your password",
                login: "Sign in",
                loggingIn: "Signing in…",
                help: "Contact your administrator if you need an account or forgot your password.",
                errors: {
                    invalidCredentials:
                        "The username or password is incorrect.",
                    accountLocked:
                        "This account is locked. Contact your administrator.",
                    rateLimited: "Too many sign-in attempts. Try again later.",
                    offline: "You are offline. Check your network connection.",
                    unknown: "Sign-in failed. Try again later.",
                },
                changePassword: {
                    title: "Change password",
                    currentPassword: "Current password",
                    currentPasswordRequired: "Enter your current password",
                    currentPasswordPlaceholder: "Enter current password",
                    newPassword: "New password",
                    newPasswordRequired: "Enter a new password",
                    newPasswordPlaceholder: "Enter new password",
                    newPasswordHelp:
                        "Use 8–128 characters and choose a different password.",
                    passwordLength: "Password must be 8–128 characters",
                    passwordUnchanged:
                        "New password must differ from the current password",
                    confirmPassword: "Confirm new password",
                    confirmPasswordRequired: "Enter the new password again",
                    confirmPasswordPlaceholder: "Enter new password again",
                    passwordMismatch: "The new passwords do not match",
                    submit: "Change password",
                    success: "Password changed",
                    failed: "Could not change password: {{message}}",
                },
                lock: {
                    title: "Inkwell is locked",
                    continueAs: "{{username}}, enter your password to continue",
                    passwordRequired: "Enter your password",
                    passwordPlaceholder: "Password",
                    unlock: "Unlock",
                    switchAccount: "Switch account",
                    logout: "Sign out",
                    errors: {
                        invalidPassword: "Incorrect password. Try again.",
                        accountLocked:
                            "This account is locked. Contact your administrator.",
                        offline:
                            "You are offline. Check your network connection.",
                        unknown: "Could not unlock. Try again later.",
                    },
                },
            },
            shell: {
                navigation: {
                    main: "Main navigation",
                    workspace: "Workspace",
                    agentSpace: "Agent space",
                    resources: "Resources",
                    tools: "Tools",
                    skills: "Skills",
                    models: "Models",
                    system: "System",
                    users: "User management",
                },
                aboutInkwell: "About Inkwell",
                appearanceSwitch: "Toggle appearance",
                connection: {
                    online: "Service connected",
                    reconnecting: "Reconnecting",
                    offline: "Service unavailable",
                },
                errors: {
                    offline:
                        "The connection was lost. Reconnecting now. Write actions are disabled until service is restored.",
                    reconnecting:
                        "Connecting to the service. Write actions are temporarily unavailable.",
                    "rate-limited": "Too many requests. Try again later.",
                    rateLimitedWithRetry:
                        "Too many requests. Try again in {{seconds}} seconds.",
                    "service-unavailable":
                        "The service is temporarily unavailable. Try again later.",
                },
                guide: "User guide",
                quickStart: "Quick start",
                faq: "FAQ",
                help: "Help",
                settings: "Preferences",
                changePassword: "Change password",
                administration: "Administration",
                logout: "Sign out",
                openUserMenu: "Open user menu",
                comingSoon: "Coming soon",
                placeholderEntry: "Placeholder · Coming soon",
                quickStartDescription:
                    "Complete these steps to build a workflow from configuration to team use.",
                quickStartSteps: {
                    create: {
                        title: "Create an agent",
                        description: "Add its name and purpose",
                    },
                    configure: {
                        title: "Complete the core setup",
                        description: "Add instructions and select a model",
                    },
                    run: {
                        title: "Run a test",
                        description: "Check the response with a real question",
                    },
                    publish: {
                        title: "Publish the first version",
                        description:
                            "Create a version that can be used in chat",
                    },
                    share: {
                        title: "Share with your team",
                        description: "Allow members to view and use it",
                    },
                },
                goToAgentSpace: "Go to Agent space",
                about: {
                    version: "Version",
                    buildNumber: "Build",
                    commit: "Commit",
                    qrAlt: "Official account QR code",
                    followAuthor:
                        "Scan to follow the author's official account",
                },
                appearanceMode: "Appearance",
                light: "Light",
                dark: "Dark",
                system: "System",
                themeColor: "Theme color",
                themes: {
                    amethyst: "Purple",
                    terracotta: "Orange",
                    teal: "Teal",
                },
            },
            editor: {
                unsavedTitle: "Unsaved changes",
                unsavedContent: "Your changes will be lost if you leave.",
                leave: "Leave anyway",
                continueEditing: "Keep editing",
            },
            agents: {
                space: {
                    title: "Agent space",
                    create: "New Agent",
                    mine: "Mine",
                    shared: "Shared with team",
                    refreshLabel: "Refresh Agents",
                    searchPlaceholder: "Search Agents",
                    filters: {
                        all: "All {{count}}",
                        published: "Published {{count}}",
                        draft: "Drafts {{count}}",
                    },
                    configComingSoon:
                        "Agent configuration will be connected in the next task.",
                    loadError:
                        "Could not load Agents. Check your connection and try again.",
                    actionSuccess: {
                        share: "Agent shared with the team",
                        unshare: "Team access revoked",
                        revoke: "Shared access revoked by an administrator",
                    },
                    actionFailed: "Agent action failed.",
                    actions: {
                        edit: "Edit configuration",
                        editLabel: "Edit {{name}}",
                        share: "Share published version",
                        shareLabel: "Share {{name}}",
                        revoke: "Revoke access",
                        revokeLabel: "Revoke access to {{name}}",
                        view: "View details",
                        viewLabel: "View details for {{name}}",
                    },
                    status: {
                        draft: "Draft",
                        shared: "Shared",
                        unpublished: "Unpublished",
                        unpublishedChanges: "Unpublished changes",
                    },
                    noDescription: "No description",
                    empty: {
                        filtered: "No Agents match the current filters",
                        mine: "You have no Agents yet. Create one to get started.",
                        shared: "No team members have shared an Agent yet",
                    },
                    dialogs: {
                        revokeTitle: "Revoke access to “{{name}}”?",
                        revokeContent:
                            "Other members will lose access. The owner's Agent will not be deleted.",
                        confirmRevoke: "Revoke access",
                        shareTitle: "Share “{{name}}”?",
                        shareContent:
                            "Team members will be able to view and use the published version of this Agent.",
                        confirmShare: "Share",
                    },
                },
                editor: {
                    sections: {
                        basic: "Basic details",
                        instructions: "Instructions",
                        model: "Model and parameters",
                        tools: "Tools",
                        skills: "Skills",
                        version: "Versions",
                    },
                    messages: {
                        saved: "Draft saved without changing the published version",
                        saveFailed: "Could not save the draft.",
                        avatarUploaded:
                            "Avatar uploaded. Save the Agent to apply it.",
                        avatarUploadFailed: "Could not upload the avatar.",
                        published: "Published as v{{version}}",
                        publishFailed:
                            "Publishing failed. The draft was preserved.",
                        cloned: "Copied to my Agents",
                        cloneFailed: "Could not copy the Agent.",
                        rolledBack: "Rolled back as new version v{{version}}",
                        rollbackFailed: "Could not roll back the Agent.",
                        deleted: "Agent deleted",
                        deleteFailed: "Could not delete the Agent.",
                    },
                    deleteDialog: {
                        title: "Delete “{{name}}”?",
                        content:
                            "This permanently deletes the Agent, every version, and its conversation history. This action cannot be undone.",
                        confirm: "Delete Agent",
                    },
                    loadFailed: "Could not load the Agent",
                    backLabel: "Back to Agent space",
                    newAgent: "New Agent",
                    versionOwner: "v{{version}} · Owner: {{owner}}",
                    notSaved: "Not saved",
                    delete: "Delete Agent",
                    trial: "Test run",
                    publish: "Publish",
                    copyToMine: "Copy to my Agents",
                    readonly:
                        "This Agent was shared by another team member and is read-only.",
                    navigationLabel: "Agent configuration sections",
                    expandNavigation: "Expand configuration navigation",
                    collapseNavigation: "Collapse configuration navigation",
                    instructionsHelp:
                        "System instructions for the Agent. Markdown is supported; a warning appears above 32K characters.",
                    instructionsHelpLabel: "About system instructions",
                    publishDialog: {
                        title: "Publish new version",
                        description:
                            "This becomes the new production version. Active chats will use it from their next turn.",
                        changeSummary: "Change summary (optional)",
                        changeSummaryPlaceholder:
                            "Describe this update for the version history",
                        shareAfterPublish:
                            "Share with the team after publishing",
                        shareHint:
                            "Team members will be able to view and use this new version.",
                    },
                    basic: {
                        avatar: "Avatar",
                        changeAvatar: "Change avatar",
                        changeAvatarLabel: "Change Agent avatar",
                        avatarReadonly: "Agent avatar",
                        clickToChange: "Click to change",
                        name: "Agent name",
                        nameRequired: "Enter an Agent name",
                        nameTooLong: "Name must be 50 characters or fewer",
                        namePlaceholder: "Agent name (1–50 characters)",
                        description: "Description",
                        descriptionPlaceholder:
                            "Briefly describe this Agent's role and capabilities",
                        teamVisible: "Visible to team",
                        teamVisibleHelp:
                            "Controls the Agent-level IsShared state and is not rolled back with version snapshots.",
                        shared: "Shared",
                        private: "Only me",
                    },
                    bindings: {
                        noTools: "No Tools are available in Tool management",
                        skillsDescription:
                            "Select Skills from Skill management to attach to this Agent.",
                        noSkills: "No Skills are available in Skill management",
                    },
                    version: {
                        none: "This Agent has not been published",
                        rollbackTitle: "Roll back to v{{version}}?",
                        rollbackContent:
                            "A new published version will be created from this historical version. Existing history is preserved.",
                        confirmRollback: "Roll back",
                        history: "Version history",
                        historyDescription:
                            "Publishing and rollback create immutable versions. Saved drafts do not appear here.",
                        compare: "Compare versions",
                        columns: {
                            version: "Version",
                            status: "Status",
                            savedTime: "Saved",
                            savedBy: "Saved by",
                            summary: "Change summary",
                            actions: "Actions",
                        },
                        published: "Published",
                        historical: "Historical",
                        previousCompare: "Compare with previous",
                        latestCompare: "Compare with latest",
                        rollbackThis: "Roll back to this version",
                        noSummary: "No summary",
                        versus: "versus",
                        field: "Field",
                        identical:
                            "These versions have identical configurations.",
                        emptyValue: "None",
                        defaultValue: "Default",
                        snapshot: {
                            basic: "Basic details",
                            name: "Name",
                            avatar: "Avatar",
                            description: "Description",
                            instructions: "Instructions",
                            model: "Model and parameters",
                            modelId: "Model",
                            chatHistory: "Chat history settings",
                            capabilities: "Capabilities",
                            tools: "Tools",
                            skills: "Skills",
                        },
                    },
                    instructions: {
                        tooLong:
                            "Long Instructions may consume too much model context",
                        empty: "No Instructions",
                    },
                    model: {
                        runtime: "Runtime model",
                        runtimeRequired: "Select a runtime model",
                        vision: "Vision",
                        tools: "Tools",
                        structuredOutput: "Structured output",
                        generationParameters: "Generation parameters",
                        generationDescription:
                            "Turn off Custom to use the model default and omit the parameter from requests.",
                        temperature: "Temperature",
                        temperatureDescription: "Controls output randomness",
                        topP: "Top P",
                        topPDescription:
                            "Limits the candidate token probability range",
                        maxTokens: "Max Tokens",
                        maxTokensDescription: "Limits the response length",
                        context: "Context",
                        contextDescription:
                            "Older messages are trimmed above this limit to prevent unbounded context growth.",
                        maxMessages: "Maximum message history",
                        messageUnit: "messages",
                        custom: "Custom",
                        default: "Default",
                    },
                    state: {
                        unsaved: "Unsaved",
                        draft: "Unpublished draft",
                        changed: "Unpublished changes",
                        published: "Published",
                    },
                },
                details: {
                    title: "Agent details",
                    closeLabel: "Close Agent details",
                    version: "Version v{{version}}",
                    noDescription: "No description",
                    instructionsCopied: "Instructions copied",
                    copyFailed: "Could not copy. Try again.",
                    overview: "Version overview",
                    changeSummary: "Change summary",
                    noSummary: "None",
                    runtimeModel: "Runtime model",
                    notConfigured: "Not configured",
                    characterCount: "{{count}} character",
                    characterCount_other: "{{count}} characters",
                    copyInstructions: "Copy Instructions",
                    collapse: "Collapse",
                    expand: "Expand",
                    modelAndContext: "Model and context",
                    historyLimit: "Message history limit",
                    tools: "Tools",
                    noTools: "No Tools attached",
                    noSkills: "No Skills attached",
                    defaultValue: "Default",
                },
            },
            chat: {
                composer: {
                    placeholder:
                        "Type a message. Press Enter to send or Shift+Enter for a new line.",
                    attachmentUnavailable: "Attachments are not available yet",
                    addAttachment: "Add attachment",
                },
                prompts: {
                    capabilities: {
                        label: "Explore capabilities",
                        description:
                            "Describe how you can help and give me a few specific examples",
                    },
                    tasks: {
                        label: "Suggest tasks",
                        description:
                            "Based on your capabilities, suggest three tasks we can start now",
                    },
                    questionTips: {
                        label: "Asking tips",
                        description:
                            "Tell me how to ask questions so you can give better answers",
                    },
                },
                messages: {
                    stopped: "Generation stopped",
                    retryLabel: "Retry failed message",
                    regenerate: "Regenerate",
                    copyLabel: "Copy message {{number}}",
                    feedbackLabel: "Rate message {{number}}",
                },
                usage: {
                    label: "Token usage",
                    summaryLabel: "Usage",
                    input: "Input",
                    output: "Output",
                    total: "Total",
                    tokens: "tokens",
                },
                activity: {
                    title: {
                        loaded: "Load Skill: {{name}}",
                        resource: "Read resource: {{name}}",
                        script: "Run script: {{name}}",
                        tool: "Call tool: {{name}}",
                    },
                    readInstructions: "Read Skill instructions",
                    fromSkill: "From {{name}}",
                    toolFunction: "Function {{name}}",
                    status: {
                        loading: "Running",
                        failed: "Failed",
                        stopped: "Stopped",
                        loaded: "Loaded",
                        success: "Succeeded",
                    },
                    parameters: "Arguments",
                    calls: "Tool calls",
                    failedCount: "{{count}} failed",
                    stoppedCount: "{{count}} stopped",
                    completedCount: "{{count}} completed",
                },
                panel: {
                    newConversation: "New conversation",
                    historyGroup: "Conversation history",
                    errors: {
                        historyLoad: "Could not load conversation history.",
                        historyRefresh:
                            "Could not refresh conversation history.",
                        agentCall: "Agent call failed.",
                        messagesLoad: "Could not load conversation messages.",
                        deleteConversation:
                            "Could not delete the conversation.",
                        clearConversation: "Could not clear the conversation.",
                    },
                    deleteDialog: {
                        title: "Delete this conversation?",
                        content:
                            "The conversation and all its messages will be permanently deleted. This action cannot be undone.",
                        confirm: "Delete conversation",
                    },
                    clearDialog: {
                        title: "Clear this conversation?",
                        content:
                            "All messages will be permanently deleted, but the conversation will remain in history.",
                        confirm: "Clear conversation",
                    },
                    selectAgent: "Select an Agent to start chatting",
                    back: "Back to Agent space",
                    model: "Model: {{model}}",
                    modelNotConfigured: "Not configured",
                    versionLoading: "Loading version",
                    version: "Version v{{version}}",
                    viewDetails: "View Agent details",
                    clearConversation: "Clear current conversation",
                    scrollToLatest: "Scroll to latest message",
                    conversations: "Conversations",
                    expandConversations: "Expand conversations",
                    collapseConversations: "Collapse conversations",
                    createConversation: "New conversation",
                    currentDraft: "Current draft",
                    publishedVersion: "Published v{{version}}",
                    newTrial: "New test conversation",
                    trialList: "Test conversation list",
                    closeTrial: "Close test run",
                    trialWelcomeTitle: "Start with a research question",
                    trialWelcomeDescription:
                        "I can help find sources, organize evidence, and produce a structured report.",
                    startConversation: "Start a new conversation",
                    publishedMessage:
                        "Messages will be handled by the current published Agent version.",
                    usingDraft: "Using the saved draft",
                    usingPublished: "Using published version v{{version}}",
                },
            },
            tools: {
                title: "Tools",
                description:
                    "View the Tools available to Agents for retrieving information, calling services, and completing actions.",
                refreshLabel: "Refresh Tools",
                searchPlaceholder: "Search names or descriptions",
                loadFailed: "Could not load Tools. Try again later.",
                empty: "No Tools are registered",
                filteredEmpty: "No Tools match your search",
                columns: {
                    name: "Name",
                    description: "Description",
                    parameters: "Parameters",
                    updatedTime: "Updated",
                    actions: "Actions",
                },
                parameterCount: "{{count}} item",
                parameterCount_other: "{{count}} items",
                viewLabel: "View {{name}}",
                details: {
                    title: "Tool details",
                    closeLabel: "Close Tool details",
                    parameterCount: "{{count}} parameter",
                    parameterCount_other: "{{count}} parameters",
                    parameters: "Parameters",
                    noParameters: "This Tool has no parameters",
                    type: "Type",
                    required: "Required",
                    allowedValues: "Allowed values",
                    rawSchema: "Raw JSON Schema",
                    viewSchema: "View raw Schema",
                    timeInformation: "Time information",
                },
            },
            models: {
                categories: {
                    Unknown: "Unknown",
                    Chat: "Chat",
                    Embedding: "Embedding",
                    ImageGeneration: "Image generation",
                    VideoGeneration: "Video generation",
                },
                capability: {
                    supported: "Supported",
                    unsupported: "Not supported",
                },
                test: {
                    success:
                        "{{model}} {{category}} minimal request succeeded · {{latency}}",
                    failed: "Model connection test failed",
                    action: "Test",
                    actionLabel: "Test {{name}}",
                    failureStatus: "Failed",
                },
                title: "Models",
                description:
                    "View models and capabilities discovered from LiteLLM in real time. This list is read-only; configure models in LiteLLM.",
                management: "Manage models",
                filterLabel: "Filter model types",
                allTypes: "All types",
                refreshLabel: "Refresh models",
                searchPlaceholder: "Search model IDs or providers",
                loadFailed: "Could not load LiteLLM models. Try again.",
                empty: "LiteLLM returned no models",
                filteredEmpty: "No models match the current filters",
                columns: {
                    id: "Model ID",
                    type: "Model type",
                    provider: "Provider",
                    tokenLimit: "Token limits",
                    tokenSummary: "Input {{input}} / Output {{output}}",
                    vision: "Vision",
                    tools: "Tools",
                    structured: "Structured",
                    reasoning: "Reasoning",
                    actions: "Actions",
                },
                viewLabel: "View {{name}}",
                details: {
                    title: "Model details",
                    closeLabel: "Close model details",
                    unknownMode: "Unknown mode",
                    unknownProvider: "Unknown provider",
                    information: "Model information",
                    modelId: "Model ID",
                    category: "Category",
                    providerMode: "Provider mode",
                    ownedBy: "Owned by",
                    tokenLimit: "Token limits",
                    maxInput: "Maximum input",
                    maxOutput: "Maximum output",
                    tokens: "tokens",
                    capabilities: "Capabilities",
                    toolCalls: "Tool calls",
                    structuredOutput: "Structured output",
                    dataSourceDescription:
                        "Data is discovered from LiteLLM in real time and is not stored in Inkwell.",
                },
            },
            skills: {
                validation: {
                    namePattern:
                        "Machine names may contain lowercase letters, numbers, and single hyphens, and cannot start or end with a hyphen",
                    missingFrontmatter:
                        "SKILL.md is missing valid YAML frontmatter",
                    missingFields:
                        "SKILL.md must include a name and description",
                    invalidName: "SKILL.md: {{message}}",
                    archiveSkillFile:
                        "The archive must contain exactly one SKILL.md",
                    filesOutsideRoot:
                        "All files must be inside the folder containing SKILL.md",
                    unsupportedFolder:
                        "Package files may only be placed in references, assets, or scripts folders",
                },
                owner: {
                    me: "Me",
                    others: "Other members",
                    mine: "Uploaded by me",
                },
                messages: {
                    saved: "Skill saved",
                    saveFailed: "Could not save the Skill. Try again later.",
                    deleted: "Skill deleted",
                    deleteFailed:
                        "Could not delete the Skill. Try again later.",
                    parseFailed: "Could not parse the Skill file",
                    uploaded: "Skill uploaded",
                    uploadFailed:
                        "Could not upload the Skill. Check the file structure.",
                },
                title: "Skills",
                description:
                    "View and manage Skills that teach Agents how to complete specific work through instructions and reference material.",
                upload: "Upload Skill",
                filters: {
                    all: "All owners",
                    mine: "Uploaded by me",
                    others: "Other members",
                },
                refreshLabel: "Refresh Skills",
                searchPlaceholder: "Search names, descriptions, or owners",
                loadFailed: "Could not load Skills. Try again later.",
                empty: "No Skills yet",
                filteredEmpty: "No Skills match the current filters",
                columns: {
                    name: "Name",
                    description: "Description",
                    owner: "Owner",
                    resources: "Resources",
                    updatedTime: "Updated",
                    actions: "Actions",
                },
                resourceSummary:
                    "{{references}} references · {{assets}} assets · {{scripts}} scripts",
                actions: {
                    viewLabel: "View {{name}}",
                    editLabel: "Edit {{name}}",
                    deleteLabel: "Delete {{name}}",
                },
                deleteDialog: {
                    title: "Delete “{{name}}”?",
                    content:
                        "New configurations will no longer be able to select this Skill. Saved drafts and published versions will keep using their snapshots.",
                    confirm: "Delete Skill",
                },
                details: {
                    editTitle: "Edit Skill",
                    title: "Skill details",
                    closeLabel: "Close Skill details",
                    machineName: "Machine name",
                    machineNameHelp:
                        "Used for Skill discovery and load_skill calls. Must use kebab-case.",
                    machineNameRequired: "Enter a machine name",
                    machineNameTooLong:
                        "Machine name must be 64 characters or fewer",
                    machineNamePlaceholder: "For example, code-review",
                    descriptionRequired: "Enter a description",
                    content: "SKILL.md content",
                    contentRequired: "Enter content",
                    owner: "Owner: {{owner}}",
                    referenceCount: "References: {{count}} (read-only)",
                    assetCount: "Assets: {{count}} (read-only)",
                    scriptCount: "Scripts: {{count}} (read-only)",
                    characterCount: "{{count}} character",
                    characterCount_other: "{{count}} characters",
                    collapse: "Collapse",
                    expand: "Expand",
                    resources: "Resources",
                    references: "References",
                    assets: "Assets",
                    scripts: "Scripts",
                    timeInformation: "Time information",
                    createdTime: "Created",
                    updatedTime: "Updated",
                },
                uploadDialog: {
                    title: "Upload Skill",
                    start: "Upload",
                    selectFile:
                        "Select a zipped Skill folder or a SKILL.md file",
                    hint: "The name and description come from the SKILL.md YAML frontmatter and can be edited after upload. references/, assets/, and scripts/ are supported.",
                    parsing: "Parsing SKILL.md...",
                    preview: "SKILL.md preview",
                    machineName: "Machine name",
                    description: "Description",
                    packageResources: "Package resources",
                    packageSummary:
                        "{{references}} references · {{assets}} assets · {{scripts}} scripts",
                },
            },
            users: {
                neverLoggedIn: "Never signed in",
                errors: {
                    add: "Could not add user: {{message}}",
                    action: "Account action failed: {{message}}",
                    reset: "Could not reset password: {{message}}",
                },
                actionSuccess: {
                    unlock: "{{username}} was unlocked",
                    disable: "{{username}} was disabled",
                    enable: "{{username}} was enabled",
                },
                status: {
                    active: "Active",
                    locked: "Locked",
                    disabled: "Disabled",
                },
                confirmations: {
                    unlock: {
                        title: "Unlock {{username}}?",
                        content:
                            "The user can immediately try to sign in again.",
                        confirm: "Unlock account",
                    },
                    disable: {
                        title: "Disable {{username}}?",
                        content:
                            "The user will remain unable to sign in until an administrator enables the account again.",
                        confirm: "Disable account",
                    },
                    enable: {
                        title: "Enable {{username}}?",
                        content:
                            "The user will be able to sign in with their existing password.",
                        confirm: "Enable account",
                    },
                    reset: {
                        title: "Reset the password for {{username}}?",
                        content:
                            "The current password will stop working immediately. A temporary password will be shown once.",
                        confirm: "Reset password",
                    },
                },
                title: "User management",
                description:
                    "Add users, reset passwords, and manage locked or disabled accounts.",
                add: "Add user",
                filters: {
                    statusLabel: "Filter account status",
                    allStatus: "All statuses",
                    roleLabel: "Filter account role",
                    allRoles: "All roles",
                },
                refreshLabel: "Refresh users",
                searchPlaceholder: "Search usernames",
                loadFailed: "Could not load users. Try again.",
                empty: "No users",
                filteredEmpty: "No users match the current filters",
                columns: {
                    username: "Username",
                    role: "Role",
                    status: "Status",
                    lastLogin: "Last sign-in",
                    createdTime: "Created",
                    actions: "Actions",
                },
                manage: "Manage",
                manageLabel: "Manage {{username}}",
                create: {
                    successTitle: "User added",
                    title: "Add user",
                    created: "{{username}} was created",
                    temporaryPasswordNotice:
                        "Give this temporary password to the user now. It will not be shown again after this window closes.",
                    temporaryPassword: "Temporary password",
                    mustChangePassword:
                        "The user must set a new password after their first sign-in.",
                    usernameRequired: "Enter a username",
                    usernameTooLong: "Username must be 100 characters or fewer",
                    usernameExists: "Username already exists",
                    usernamePlaceholder: "Enter username",
                    role: "Role",
                    adminWarning:
                        "Admins can manage users and shared resources in this deployment. Grant this role only to trusted people.",
                    passwordHint:
                        "A one-time temporary password will be shown after the user is created.",
                },
                management: {
                    title: "Manage user",
                    titleWithName: "Manage user · {{username}}",
                    resetSuccess: "Password reset for {{username}}",
                    resetNotice:
                        "Give this temporary password to the user now. It will not be shown again after closing this window.",
                    nextLoginChange:
                        "The user must set a new password at their next sign-in.",
                    password: "Password",
                    passwordDescription:
                        "Generate a one-time temporary password and require a new password at the next sign-in.",
                    resetPassword: "Reset password",
                    loginStatus: "Sign-in status",
                    currentAccount:
                        "You cannot disable the account you are currently using.",
                    autoLocked:
                        "This account was automatically locked after too many failed sign-in attempts.",
                    unlock: "Unlock",
                    enable: "Enable user",
                    disable: "Disable user",
                },
            },
            guide: {
                title: "User guide",
                searchPlaceholder: "Search the guide",
                navigationLabel: "User guide sections",
                noMatches: "No matching guide sections",
                sections: {
                    quickStart: {
                        label: "Quick start",
                        description:
                            "Create and share your first Agent from start to finish.",
                    },
                    create: {
                        label: "Create and configure",
                        description:
                            "Set basic details, Instructions, models, Tools, and Skills.",
                    },
                    publish: {
                        label: "Save and publish",
                        description:
                            "Understand drafts, published versions, and unpublished changes.",
                    },
                    share: {
                        label: "Share and copy",
                        description:
                            "Share a published Agent with your team or make an independent copy.",
                    },
                    faq: {
                        label: "FAQ",
                        description:
                            "Find quick answers about versions, sharing, and permissions.",
                    },
                },
                quickStart: {
                    eyebrow: "About 5 minutes",
                    title: "Create and publish your first Agent",
                    description:
                        "Follow one complete path through Inkwell. You can return to any step later.",
                    steps: {
                        create: {
                            title: "Create an Agent",
                            content:
                                "Add a name and purpose to create an unpublished draft.",
                        },
                        configure: {
                            title: "Complete the core setup",
                            content:
                                "Add Instructions and select a runtime model.",
                        },
                        trial: {
                            title: "Run a test",
                            content:
                                "Use a real question to check the response.",
                        },
                        publish: {
                            title: "Publish a version",
                            content:
                                "Turn the current draft into a version available for chat.",
                        },
                        share: {
                            title: "Share when ready",
                            content:
                                "Let team members view and use the published version.",
                        },
                    },
                    open: "Open quick start",
                    goToAgents: "Go to Agent space",
                },
                create: {
                    eyebrow: "Agent setup",
                    title: "Define the role before adding capabilities",
                    description:
                        "Start with the smallest setup needed for a test, then add Tools and Skills.",
                    items: {
                        basics: {
                            title: "Basic details",
                            description:
                                "Use a clear name and short description to define the Agent's role.",
                        },
                        instructions: {
                            title: "Instructions",
                            description:
                                "Specify goals, constraints, output format, and failure behavior.",
                        },
                        model: {
                            title: "Model and parameters",
                            description:
                                "Choose a runtime model and keep default generation settings unless needed.",
                        },
                        capabilities: {
                            title: "Tools and Skills",
                            description:
                                "Attach only the capabilities required for the task.",
                        },
                    },
                    trialNote:
                        "Tests use the saved configuration. Production chats always use a published version.",
                },
                publish: {
                    eyebrow: "Version lifecycle",
                    title: "Saving a draft does not publish it",
                    description:
                        "Drafts remain editable. Publishing creates a new production version.",
                    items: {
                        save: {
                            title: "Save",
                            description:
                                "Save the current draft without changing published versions or active chats.",
                        },
                        publish: {
                            title: "Publish",
                            description:
                                "Create a new version for subsequent chat turns.",
                        },
                        changes: {
                            title: "Unpublished changes",
                            description:
                                "The published version remains available while the draft contains updates.",
                        },
                    },
                    draft: "Unpublished draft",
                    changed: "Unpublished changes",
                    published: "Published",
                },
                share: {
                    eyebrow: "Team collaboration",
                    title: "Share access, not edit rights",
                    description:
                        "Team members can view and use the published version but cannot edit the owner's Agent.",
                    items: {
                        share: {
                            title: "Share",
                            description:
                                "Expose only the current published version. Draft changes stay private.",
                        },
                        revoke: {
                            title: "Revoke access",
                            description:
                                "Remove team access without deleting the owner's Agent or configuration.",
                        },
                        copy: {
                            title: "Copy to my Agents",
                            description:
                                "Create an independent copy owned by the person who copies it.",
                        },
                    },
                },
                faq: {
                    eyebrow: "FAQ",
                    title: "Understand the current state",
                    description:
                        "Answers to common questions about creating, versioning, and sharing Agents.",
                    items: {
                        open: {
                            title: "Why does clicking a card open chat?",
                            description:
                                "Published Agents are ready to use. Use the card actions to edit or view details.",
                        },
                        edit: {
                            title: "Why can't team members edit a shared Agent?",
                            description:
                                "Sharing grants view and use access only. Make a personal copy to edit it.",
                        },
                        revoke: {
                            title: "Does revoking access delete the Agent?",
                            description:
                                "No. It only removes team visibility; the owner's Agent and version history remain.",
                        },
                        changes: {
                            title: "Why hasn't chat changed after an edit?",
                            description:
                                "Saving a draft does not affect production chats. Publish a new version first.",
                        },
                    },
                },
            },
        },
    },
} as const;
