const streamingCatchUpFrames = 10;

export const getNextStreamingContent = (
    current: string,
    target: string,
): string => {
    if (current === target || !target.startsWith(current)) return target;

    const remainingCharacters = Array.from(target.slice(current.length));
    const revealCount = Math.max(
        1,
        Math.ceil(remainingCharacters.length / streamingCatchUpFrames),
    );
    return current + remainingCharacters.slice(0, revealCount).join("");
};
