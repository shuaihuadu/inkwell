import Editor from "@monaco-editor/react";

interface MarkdownEditorProps {
    "aria-label": string;
    appearance: "light" | "dark";
    value?: string;
    onChange?: (value: string) => void;
}

export function MarkdownEditor({
    "aria-label": ariaLabel,
    appearance,
    value = "",
    onChange,
}: MarkdownEditorProps) {
    return (
        <Editor
            aria-label={ariaLabel}
            height="480px"
            defaultLanguage="markdown"
            theme={appearance === "dark" ? "vs-dark" : "light"}
            value={value}
            onChange={(nextValue) => onChange?.(nextValue ?? "")}
            options={{
                accessibilitySupport: "on",
                automaticLayout: true,
                folding: false,
                fontSize: 13,
                lineHeight: 21,
                minimap: { enabled: false },
                overviewRulerLanes: 0,
                padding: { top: 12, bottom: 12 },
                renderLineHighlight: "line",
                scrollBeyondLastLine: false,
                wordWrap: "on",
            }}
        />
    );
}
