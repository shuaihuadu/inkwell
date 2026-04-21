using System.Text.Json;

namespace Inkwell.Workflows;

/// <summary>
/// 批量评估 Workflow 输入适配器
/// </summary>
/// <remarks>
/// <para>
/// 把用户在聊天框里发的自由文本转换成 <c>List&lt;ArticleEvaluation&gt;</c>，
/// 兼容两种常见输入格式：
/// </para>
/// <list type="number">
/// <item>
/// 完整 JSON 数组：<c>[{"title":"...","content":"..."}]</c>
/// （允许直接粘贴现成的评估批次）
/// </item>
/// <item>
/// 纯文本：段落之间以空行分隔；每段第一行作为标题、其余行作为内容。
/// 只有一段时，整段作为内容，标题取首行。
/// </item>
/// </list>
/// <para>
/// 适配失败时返回单元素列表，避免入口 Executor 拿到空集合后直接短路。
/// </para>
/// </remarks>
internal static class BatchEvaluationInputAdapter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// 解析原始文本为评估列表
    /// </summary>
    /// <param name="raw">原始输入</param>
    /// <returns>至少包含 1 个元素的评估列表</returns>
    public static List<ArticleEvaluation> Parse(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [new ArticleEvaluation { Title = "未命名文章", Content = string.Empty }];
        }

        string trimmed = raw.Trim();

        // 优先尝试 JSON 数组
        if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
        {
            try
            {
                List<ArticleEvaluation>? list = JsonSerializer.Deserialize<List<ArticleEvaluation>>(trimmed, JsonOptions);
                if (list is { Count: > 0 })
                {
                    return list;
                }
            }
            catch (JsonException)
            {
                // 回退到文本模式
            }
        }

        // 文本模式：按空行切分为多篇文章
        string[] blocks = trimmed
            .Split(["\n\n", "\r\n\r\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(static b => b.Trim())
            .Where(static b => b.Length > 0)
            .ToArray();

        if (blocks.Length == 0)
        {
            return [new ArticleEvaluation { Title = "未命名文章", Content = trimmed }];
        }

        List<ArticleEvaluation> articles = [];
        foreach (string block in blocks)
        {
            int firstNewline = block.IndexOf('\n');
            string title;
            string content;
            if (firstNewline > 0)
            {
                title = block[..firstNewline].Trim();
                content = block[(firstNewline + 1)..].Trim();
            }
            else
            {
                // 单行块：整段既作标题也作内容
                title = block.Length > 40 ? block[..40] + "..." : block;
                content = block;
            }

            articles.Add(new ArticleEvaluation
            {
                Title = string.IsNullOrWhiteSpace(title) ? "未命名文章" : title,
                Content = string.IsNullOrWhiteSpace(content) ? title : content
            });
        }

        return articles;
    }
}
