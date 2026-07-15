import { useCallback, useRef, useState } from "react";
import { message } from "antd";
import type { HarnessStep } from "./HarnessThoughtChain";
import type { TodoItem } from "./TodosPanel";
import { createAGUIEventHandler } from "./agui/reducer";
import { playAGUITimeline } from "./agui/sse";
import { streamingDuration, streamingTextEvents } from "./agui/textStream";
import type { AGUIEvent } from "./agui/types";

// ─── 共用对话核心逻辑（UI-005 对话页 / UI-004 内嵌"开始对话"面板共用） ────────────
// 两处入口在 ui-spec.md 里参考的视觉形态不同（UI-005 是 Ant Design X "ultramodern" 全屏
// 布局，UI-004 内嵌面板是 "copilot" 侧栏布局），但"发一条消息 → 出现在消息流 → 等一小段
// 时间收到一条 mock 回复"这套核心状态与时序逻辑是完全一致的，所以抽成本 hook 共用，两个
// 页面只负责各自的布局 / Conversations 会话列表 / 空状态展示方式。

export type ChatRole = "user" | "ai" | "harness" | "todos" | "reasoning" | "loop";

export interface ChatMessage {
    id: string;
    role: ChatRole;
    /** user / ai：消息正文；reasoning（推理/深度思考）：思考过程文本；
     * harness/todos/loop：未使用（内容统一通过 harnessSteps/todos/loopSteps 字段携带） */
    content: string;
    time: string;
    /** 仅 ai 角色使用：Token 用量统计（对应 UsageDisplayObserver），展示在这条回复正文下方、
     * Actions 上方——不再单独占一条气泡，而是挂在触发这次用量统计的那条回复消息最后面 */
    usage?: string;
    /** 仅 harness 角色使用：一轮 plan→execute 自主循环的步骤链，用 ThoughtChain 承载 */
    harnessSteps?: HarnessStep[];
    /** 仅 todos 角色使用：Harness 维护的任务清单 */
    todos?: TodoItem[];
    /** 仅 reasoning 角色使用：是否仍在思考中（对应官方 console 里思考完成前后图标/标题会变） */
    reasoningLoading?: boolean;
    /** 仅 loop 角色使用：`LoopAgent` + `LoopEvaluator` 驱动的迭代优化循环（“第 N 轮”产出 → 评估器给 continue/stop 反馈），与 harnessSteps 同型，复用 ThoughtChain 承载 */
    loopSteps?: HarnessStep[];
}

export type MockReplyFactory = (userText: string) => string;

export function formatNow(): string {
    const now = new Date();
    return `${now.getHours().toString().padStart(2, "0")}:${now
        .getMinutes()
        .toString()
        .padStart(2, "0")}`;
}

// 默认单条 mock 回复里特意带上常见 Markdown 语法（加粗/列表/行内代码/链接/引用），方便
// 直接在原型里验证消息气泡的 Markdown 渲染效果（见 ChatMarkdown.tsx），不用另外造数据。
const defaultMockReply: MockReplyFactory = (text) =>
    [
        `收到，这是一条演示回复：**"${text}"**。本原型未接入真实模型，仅用于展示对话消息流样式，`,
        `消息气泡的正文支持 Markdown 渲染，比如：`,
        ``,
        `- 加粗、*斜体*、\`行内代码\``,
        `- 有序/无序列表`,
        `- [外部链接](https://ant.design/index-cn)`,
        ``,
        `> 引用块也会正确展示。`,
    ].join("\n");

export type SetMessages = (updater: (prev: ChatMessage[]) => ChatMessage[]) => void;

/**
 * 内部之间会相互引用（`useMockChat.ts` 需要 `agui/reducer.ts` 的 `createAGUIEventHandler`，
 * `agui/reducer.ts` 又回头需要本文件的 `ChatMessage`/`formatNow`）——这是故意的循环依赖：
 * 两边都只在函数体里调用对方的导出（不在模块顶层立即执行），ESM 循环引用在这种情况下是安全的；
 * 拆开成两个互不知道对方的独立实现反而会让“AI 回复如何变成 ChatMessage”这件事有两套可能跑偏的代码。
 */

/**
 * 管理一段对话的消息列表与"发送 → 等待 → 收到 mock 回复"的核心状态机。
 * 不关心消息如何渲染、也不关心多会话切换（由调用方结合 Conversations 组件自行管理，
 * 切换会话时调用 resetMessages 重新灌入该会话的历史消息即可）。
 */
export function useMockChat(
    initialMessages: ChatMessage[] = [],
    mockReply: MockReplyFactory = defaultMockReply,
) {
    const [messages, setMessages] = useState<ChatMessage[]>(initialMessages);
    const [replying, setReplying] = useState(false);
    const [input, setInput] = useState("");
    // 用 ref 记录最新消息列表/replying 状态/mockReply 回调，让下面这一批函数能用
    // `useCallback(fn, [])` 绝对稳定地包装——不依赖任何会变的闭包变量，这样上层的
    // `useChatBubbleRoles`/`createChatBubbleRoles` 才能真正 memo 住，不会因为调用方每次
    // 重渲染都重建 role 配置对象（否则 Bubble 的 `typing` 流式动画会因为 role 对象身份
    // 每次都变而不断重新开始，实际表现为"永远卡在第一个 step"）。
    const messagesRef = useRef(messages);
    messagesRef.current = messages;
    const replyingRef = useRef(replying);
    replyingRef.current = replying;
    const mockReplyRef = useRef(mockReply);
    mockReplyRef.current = mockReply;
    const inputRef = useRef(input);
    inputRef.current = input;

    const resetMessages = useCallback((next: ChatMessage[] = []) => {
        setMessages(next);
    }, []);

    /** 普通单条 mock 回复也走跟 Harness/Agent Loop 演示同一套 AG-UI 事件管线
     * （`RUN_STARTED` → `TEXT_MESSAGE_START/CONTENT/END` → `RUN_FINISHED`，经
     * `agui/sse.ts` 编解码一轮再喂给 `agui/reducer.ts`），不是另外一套手写的
     * `setTimeout` 逐字追加逻辑——两条路径最终都统一收敛到同一个 reducer，不会有
     * 两份"AI 回复怎么变成 ChatMessage"的平行实现。原来不用 Ant Design X `Bubble`
     * 自带的 `typing` 动画的理由依然成立：它内部基于 `useLayoutEffect` +
     * `requestAnimationFrame` 循环，且没有清理 rAF 的 effect cleanup，React
     * `StrictMode`（`main.tsx` 已启用）的双重 effect 调用会把这个循环打断在第一帧；
     * `playAGUITimeline` 内部只用普通 `window.setTimeout`，不受影响。 */
    const appendReply = useCallback((userText: string) => {
        setReplying(true);
        const runId = `run-${Date.now()}`;
        const messageId = `a-${Date.now() + 1}`;
        const text = mockReplyRef.current(userText);
        const timeline: Array<{ at: number; event: AGUIEvent }> = [
            { at: 700, event: { type: "RUN_STARTED", threadId: runId, runId } },
            { at: 700, event: { type: "TEXT_MESSAGE_START", messageId, role: "assistant" } },
            ...streamingTextEvents(messageId, text, 700),
        ];
        const endAt = 700 + streamingDuration(text);
        timeline.push({ at: endAt, event: { type: "TEXT_MESSAGE_END", messageId } });
        timeline.push({ at: endAt, event: { type: "RUN_FINISHED", threadId: runId, runId } });
        playAGUITimeline(timeline, createAGUIEventHandler(setMessages, setReplying));
    }, []);

    const sendMessage = useCallback(
        (text: string) => {
            const trimmed = text.trim();
            if (!trimmed || replyingRef.current) return;
            setMessages((prev) => [
                ...prev,
                { id: `u-${Date.now()}`, role: "user", content: trimmed, time: formatNow() },
            ]);
            appendReply(trimmed);
        },
        [appendReply],
    );

    /** 重新生成最后一条 AI 回复（对齐 ultramodern / copilot 气泡 footer 的"重新生成"动作） */
    const retryLast = useCallback(() => {
        if (replyingRef.current) return;
        const lastUser = [...messagesRef.current].reverse().find((m) => m.role === "user");
        if (!lastUser) return;
        appendReply(lastUser.content);
    }, [appendReply]);

    /** 两处页面 handleSubmit 的三行样板（trim 校验 + 发送 + 清空输入框）完全一致，收进这里。
     * 注意：不能用 `setInput(current => { ...; sendMessage(text); return ""; })` 这种写法去读
     * 最新 input——setState 的函数式 updater 必须是纯函数，React StrictMode 会故意双调用
     * updater 来捕获这类副作用，若在 updater 里调用 sendMessage 会导致消息真的发两遍（读
     * inputRef 避开这个坑）。 */
    const submit = useCallback(
        (value?: string) => {
            const text = value ?? inputRef.current;
            if (!text.trim()) return;
            sendMessage(text);
            setInput("");
        },
        [sendMessage],
    );

    /** "新建会话"的空会话保护：当前若已经是空会话直接提示，不做任何操作；否则清空消息，
     * 调用方（自己维护多会话列表的那一处）在返回 true 后再继续处理会话切换/新建逻辑。 */
    const startNewSession = useCallback(
        (next: ChatMessage[] = []) => {
            if (messagesRef.current.length === 0) {
                message.error("当前已经是一个新会话了");
                return false;
            }
            resetMessages(next);
            return true;
        },
        [resetMessages],
    );

    return {
        messages,
        setMessages,
        resetMessages,
        replying,
        setReplying,
        sendMessage,
        retryLast,
        input,
        setInput,
        submit,
        startNewSession,
    };
}
