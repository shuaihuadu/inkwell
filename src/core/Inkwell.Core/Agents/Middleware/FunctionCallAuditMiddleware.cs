using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Inkwell.Agents.Middleware;

/// <summary>
/// 函数调用审计中间件
/// 记录 Agent 的每次工具调用及其结果
/// </summary>
public sealed class FunctionCallAuditMiddleware(ILogger<FunctionCallAuditMiddleware> logger)
{
    /// <summary>
    /// 拦截函数调用，记录调用信息和结果
    /// </summary>
    public async ValueTask<object?> InvokeAsync(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        string functionName = context.Function.Name;
        string arguments = string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}"));

        logger.LogInformation("[ToolCall] Agent 调用工具: {FunctionName}({Arguments})",
            functionName, arguments);

        object? result = await next(context, cancellationToken);

        string resultPreview = result?.ToString()?[..Math.Min(result.ToString()!.Length, 200)] ?? "(null)";

        logger.LogInformation("[ToolCall] 工具 {FunctionName} 返回: {Result}",
            functionName, resultPreview);

        return result;
    }
}
