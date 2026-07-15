import type { JsonPatchOperation } from "./types";

// ─── 最小 RFC 6902 JSON Patch 应用器 ───────────────────────────────────────────
// 只支持本原型 Activity content 用得到的路径形状：`/<数组字段名>/<下标或"->` （比如
// `/steps/0` 替换第 0 项，`/steps/-` 在末尾追加一项），以及 add/replace/remove 三种
// 操作——不是完整的 RFC 6902 实现（没有嵌套对象路径、没有 move/copy/test），够用即可。

/** 把一批 JSON Patch 操作应用到某个 Activity 的结构化 content 上，返回一份新对象
 * （不原地修改，方便调用方直接拿去 setMessages）。 */
export function applyJsonPatch(
    content: Record<string, unknown>,
    patch: JsonPatchOperation[],
): Record<string, unknown> {
    let next: Record<string, unknown> = { ...content };
    for (const operation of patch) {
        const [arrayKey, indexToken] = operation.path.split("/").filter(Boolean);
        const current = Array.isArray(next[arrayKey]) ? [...(next[arrayKey] as unknown[])] : [];
        const index = indexToken === "-" ? current.length : Number(indexToken);
        if (operation.op === "add") {
            current.splice(index, 0, operation.value);
        } else if (operation.op === "replace") {
            current[index] = operation.value;
        } else if (operation.op === "remove") {
            current.splice(index, 1);
        }
        next = { ...next, [arrayKey]: current };
    }
    return next;
}
