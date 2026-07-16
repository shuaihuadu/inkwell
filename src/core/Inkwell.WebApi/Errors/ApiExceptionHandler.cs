// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.AspNetCore.Diagnostics;

namespace Inkwell.WebApi.Errors;

/// <summary>
/// 将业务层预期异常转换为统一的 HTTP Problem Details 响应。
/// </summary>
/// <param name="problemDetailsService">Problem Details 响应写入服务。</param>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        int? statusCode = exception switch
        {
            ArgumentException => StatusCodes.Status400BadRequest,
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            KeyNotFoundException => StatusCodes.Status404NotFound,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => null,
        };

        if (statusCode is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = statusCode.Value;
        ProblemDetails problemDetails = new()
        {
            Status = statusCode.Value,
            Title = GetTitle(statusCode.Value),
            Detail = exception.Message,
        };

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        }).ConfigureAwait(false);
    }

    private static string GetTitle(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "Invalid request",
        StatusCodes.Status403Forbidden => "Access denied",
        StatusCodes.Status404NotFound => "Resource not found",
        StatusCodes.Status409Conflict => "Operation conflict",
        _ => "Request failed",
    };
}