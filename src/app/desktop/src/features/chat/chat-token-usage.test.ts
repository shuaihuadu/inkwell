import { describe, expect, test } from "vitest";
import { formatTokenCount } from "./token-count-format";

describe("formatTokenCount", () => {
    test.each([
        [980, "980"],
        [1_200, "1.2k"],
        [12_345, "12.3k"],
        [1_200_000, "1.2M"],
    ])("formats %i as %s", (value, expected) => {
        expect(formatTokenCount(value, "zh-CN")).toBe(expected);
    });
});
