import { describe, expect, test } from "vitest";
import { getNextStreamingContent } from "./smooth-streaming-content";

describe("getNextStreamingContent", () => {
    test("reveals a small appended chunk one character at a time", () => {
        expect(getNextStreamingContent("回答", "回答正在生成")).toBe("回答正");
    });

    test("increases the reveal step when the pending content grows", () => {
        const target = "abcdefghijklmnopqrstuvwxyz1234";

        expect(getNextStreamingContent("", target)).toBe("abc");
    });

    test("applies non-appended content immediately", () => {
        expect(getNextStreamingContent("旧内容", "替换后的内容")).toBe(
            "替换后的内容",
        );
    });

    test("does not split a Unicode code point", () => {
        expect(getNextStreamingContent("", "😀回答")).toBe("😀");
    });
});
