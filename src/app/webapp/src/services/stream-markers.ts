/**
 * AG-UI 文本流中嵌入的"结构化标记"统一解析层
 *
 * 后端 WorkflowChatClient 会在普通文本里插入形如
 *   <<<HITL_REQUEST:{json}>>>
 *   <<<TOOL_CALL:{json}>>>
 *   <<<TOOL_RESULT:{json}>>>
 * 的自定义标记。前端所有剥离 / 解析逻辑都走本模块，避免每种标记各写一份正则。
 */

/** 所有标记共用的结束符（与后端 WorkflowChatClient.HitlMarkerSuffix / ToolMarkerSuffix 对齐） */
export const MARKER_SUFFIX = ">>>";

/** 单个标记类型的正则规范 */
export interface MarkerSpec {
  /** 标记标签（譬如 HITL_REQUEST / TOOL_CALL / TOOL_RESULT） */
  readonly tag: string;
  /** 前缀字符串，用于调试或日志 */
  readonly prefix: string;
  /** 匹配已闭合标记（带 g 标志，可一次 matchAll 多处） */
  readonly closedRegex: RegExp;
  /** 匹配"前缀已到、结束符还没到"的末尾残片，用于流式中途隐藏 */
  readonly partialRegex: RegExp;
}

/**
 * 根据标签生成一个标记规范
 *
 * @param tag 譬如 "HITL_REQUEST"
 */
export function defineMarker(tag: string): MarkerSpec {
  const escaped = tag.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  return {
    tag,
    prefix: `<<<${tag}:`,
    closedRegex: new RegExp(`<<<${escaped}:(\\{[\\s\\S]*?\\})>>>`, "g"),
    partialRegex: new RegExp(`<<<${escaped}:[\\s\\S]*$`),
  };
}

/** HITL 审核请求 */
export const HITL_MARKER: MarkerSpec = defineMarker("HITL_REQUEST");
/** 工具调用开始 */
export const TOOL_CALL_MARKER: MarkerSpec = defineMarker("TOOL_CALL");
/** 工具调用结果 */
export const TOOL_RESULT_MARKER: MarkerSpec = defineMarker("TOOL_RESULT");

/** 解析结果 */
export interface MarkerExtraction {
  /**
   * 原文本中剥离掉 <strong>所有</strong>已闭合标记以及末尾残片后的纯文本，
   * 可直接作为展示内容（Markdown 正文）
   */
  stripped: string;
  /**
   * 每个 spec 对应的 payload JSON 字符串数组（按出现顺序）。
   * 调用方各自决定怎么解析：HITL 取最后一个，ToolCall / ToolResult 按 callId 合并。
   */
  payloads: Map<MarkerSpec, string[]>;
}

/**
 * 从原始文本中提取指定类型的标记 payload，并返回剥离后的纯文本
 */
export function extractMarkers(
  raw: string,
  specs: readonly MarkerSpec[],
): MarkerExtraction {
  const payloads = new Map<MarkerSpec, string[]>();
  let stripped = raw;

  // 先扫描所有已闭合标记、收集 payload
  for (const spec of specs) {
    const matches = Array.from(raw.matchAll(spec.closedRegex));
    payloads.set(
      spec,
      matches.map((m) => m[1]),
    );
  }

  // 再从文本中统一剥离
  for (const spec of specs) {
    if ((payloads.get(spec)?.length ?? 0) > 0) {
      stripped = stripped.replace(spec.closedRegex, "");
    }
  }

  // 最后把"前缀已到、尾标还没到"的末尾残片抹掉，避免用户看到半截 <<<...
  for (const spec of specs) {
    if (spec.partialRegex.test(stripped)) {
      stripped = stripped.replace(spec.partialRegex, "");
    }
  }

  return { stripped, payloads };
}

/**
 * 安全地把 payload JSON 字符串解析成对象，失败返回 undefined
 */
export function tryParseMarkerPayload<T>(raw: string): T | undefined {
  try {
    return JSON.parse(raw) as T;
  } catch {
    return undefined;
  }
}
