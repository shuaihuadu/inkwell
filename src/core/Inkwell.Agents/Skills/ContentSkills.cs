using System.ComponentModel;
using System.Text;

namespace Inkwell.Agents.Skills;

/// <summary>
/// Markdown 格式校验技能
/// 检查 Markdown 文本的格式问题并给出修复建议
/// </summary>
public static class MarkdownLintSkill
{
    /// <summary>
    /// 检查 Markdown 文本的格式问题
    /// </summary>
    /// <param name="markdownContent">Markdown 内容</param>
    /// <returns>检查结果报告</returns>
    [Description("检查 Markdown 文本的格式问题，如标题缺少空格、行尾多余空格、连续空行等。")]
    public static string Lint(
        [Description("Markdown 内容")] string markdownContent)
    {
        List<string> issues = [];
        string[] lines = markdownContent.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            int lineNum = i + 1;

            // 检查标题后没有空格
            if (line.StartsWith('#') && !line.StartsWith("# ") && !line.StartsWith("## ") && !line.StartsWith("### ") && line.Length > 1 && line[1] != '#')
            {
                issues.Add($"第 {lineNum} 行：标题 '#' 后缺少空格");
            }

            // 检查行尾多余空格
            if (line.EndsWith(' ') || line.EndsWith('\t'))
            {
                issues.Add($"第 {lineNum} 行：行尾有多余空格");
            }

            // 检查连续空行
            if (i > 0 && string.IsNullOrWhiteSpace(line) && string.IsNullOrWhiteSpace(lines[i - 1]))
            {
                issues.Add($"第 {lineNum} 行：连续空行");
            }
        }

        if (issues.Count == 0)
        {
            return "Markdown 格式检查通过，未发现问题。";
        }

        StringBuilder report = new();
        report.AppendLine($"发现 {issues.Count} 个格式问题：");

        foreach (string issue in issues)
        {
            report.AppendLine($"  - {issue}");
        }

        return report.ToString();
    }
}

/// <summary>
/// 文章字数统计与可读性评估技能
/// </summary>
public static class ReadabilitySkill
{
    /// <summary>
    /// 统计文章字数和段落数，计算可读性指标
    /// </summary>
    /// <param name="content">文章内容</param>
    /// <returns>可读性报告</returns>
    [Description("统计文章字数、段落数、句子数和平均句长，评估可读性等级。")]
    public static string Analyze(
        [Description("文章内容")] string content)
    {
        int charCount = content.Length;
        int chineseCharCount = content.Count(c => c >= '\u4e00' && c <= '\u9fff');
        int paragraphCount = content.Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries).Length;
        int sentenceCount = content.Split(['。', '！', '？', '.', '!', '?'], StringSplitOptions.RemoveEmptyEntries).Length;

        double avgSentenceLength = charCount > 0 && sentenceCount > 0 ? Math.Round((double)charCount / sentenceCount, 1) : 0;

        // 简单可读性评估
        string readabilityLevel;
        if (avgSentenceLength < 20)
        {
            readabilityLevel = "易读（适合大众）";
        }
        else if (avgSentenceLength < 35)
        {
            readabilityLevel = "适中（适合一般读者）";
        }
        else
        {
            readabilityLevel = "偏难（建议简化长句）";
        }

        return $"""
            文章统计报告：
            - 总字符数：{charCount}
            - 中文字符数：{chineseCharCount}
            - 段落数：{paragraphCount}
            - 句子数：{sentenceCount}
            - 平均句长：{avgSentenceLength} 字符/句
            - 可读性等级：{readabilityLevel}
            """;
    }
}

/// <summary>
/// 敏感词扫描技能
/// </summary>
public static class SensitiveWordSkill
{
    private static readonly HashSet<string> s_sensitiveWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "暴力", "赌博", "毒品", "色情", "虚假宣传", "诈骗"
    };

    /// <summary>
    /// 扫描文章中的敏感词
    /// </summary>
    /// <param name="content">文章内容</param>
    /// <returns>扫描结果</returns>
    [Description("扫描文章中的敏感词和违禁关键词。")]
    public static string Scan(
        [Description("文章内容")] string content)
    {
        List<string> found = [];

        foreach (string word in s_sensitiveWords)
        {
            if (content.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                found.Add(word);
            }
        }

        if (found.Count == 0)
        {
            return "敏感词扫描通过，未发现违禁内容。";
        }

        return $"发现 {found.Count} 个敏感词：{string.Join("、", found)}。请修改后重新提交。";
    }
}
