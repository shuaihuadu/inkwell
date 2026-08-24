const compactTokenUnits = [
    [1_000_000_000, "B"],
    [1_000_000, "M"],
    [1_000, "k"],
] as const;

export function formatTokenCount(value: number, locale: string): string {
    const compactUnit = compactTokenUnits.find(
        ([threshold]) => value >= threshold,
    );
    const numberFormat = new Intl.NumberFormat(locale, {
        maximumFractionDigits: compactUnit ? 1 : 0,
    });
    return compactUnit
        ? `${numberFormat.format(value / compactUnit[0])}${compactUnit[1]}`
        : numberFormat.format(value);
}
