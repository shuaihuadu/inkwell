using System.ComponentModel;
using Inkwell.Agents;

namespace Inkwell.WebApi;

/// <summary>
/// CMS MCP 工具函数
/// 模拟 MCP 服务器提供的 CMS 数据查询能力
/// 后续可替换为真实的 MCP 服务器连接（WithStdioServerTransport）
/// </summary>
public sealed class CmsMcpTools(AgentRegistry agentRegistry)
{
    /// <summary>
    /// 查询 CMS 中的文章列表
    /// </summary>
    /// <param name="status">文章状态过滤（可选：Draft/InReview/Published/Archived）</param>
    /// <param name="limit">最大返回条数</param>
    /// <returns>文章列表信息</returns>
    [Description("从内容管理系统查询文章列表。可按状态过滤（Draft/InReview/Published/Archived）。")]
    public string QueryArticles(
        [Description("文章状态过滤，可选值：Draft/InReview/Published/Archived，为空返回全部")] string? status = null,
        [Description("最大返回条数，默认10")] int limit = 10)
    {
        // 模拟 CMS 数据（后续接入真实数据库）
        return $"""
            CMS 文章查询结果（状态：{status ?? "全部"}，最多 {limit} 条）：
            1. [Published] 《AI 写作助手的未来展望》 - 2026-04-01 / 阅读量：1,234
            2. [Published] 《内容营销趋势报告2026》 - 2026-03-28 / 阅读量：2,456
            3. [InReview] 《如何用 AI 提升 SEO 排名》 - 2026-04-10 / 等待审核
            4. [Draft] 《短视频内容策略指南》 - 2026-04-12 / 草稿
            5. [Published] 《品牌故事写作技巧》 - 2026-03-15 / 阅读量：987
            共 5 篇文章。
            """;
    }

    /// <summary>
    /// 获取平台运营数据概览
    /// </summary>
    /// <returns>平台数据概览</returns>
    [Description("获取 Inkwell 平台的运营数据概览，包括文章数、Agent 数、阅读量等。")]
    public string GetPlatformStats()
    {
        int agentCount = agentRegistry.Count;

        return $"""
            Inkwell 平台运营数据：
            - 注册 Agent 数量：{agentCount}
            - 已发布文章：128 篇
            - 草稿文章：23 篇
            - 待审核：7 篇
            - 本月总阅读量：45,678
            - 本月新增文章：12 篇
            - 审核通过率：89.3%
            """;
    }
}
