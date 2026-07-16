// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.WebApi.Errors;

namespace Inkwell.WebApi.Tests.Controllers;

/// <summary>验证 API 异常到 Problem Details 的稳定映射。</summary>
[TestClass]
public sealed class ApiExceptionHandlerTests
{
    /// <summary>验证一般无效操作仍为 409，但不冒充会话 Run 冲突。</summary>
    /// <returns>表示异步测试操作的任务。</returns>
    [TestMethod]
    public async Task TryHandleAsync_WithInvalidOperation_WritesGeneralConflictAsync()
    {
        // Arrange
        RecordingProblemDetailsService problemDetailsService = new();
        ApiExceptionHandler handler = new(problemDetailsService);
        DefaultHttpContext context = new();

        // Act
        bool handled = await handler.TryHandleAsync(context, new InvalidOperationException("conflict"), CancellationToken.None);

        // Assert
        Assert.IsTrue(handled);
        Assert.AreEqual(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.IsNull(problemDetailsService.WrittenProblemDetails?.Type);
    }

    private sealed class RecordingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetails? WrittenProblemDetails { get; private set; }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            this.WrittenProblemDetails = context.ProblemDetails;
            return ValueTask.FromResult(true);
        }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            this.WrittenProblemDetails = context.ProblemDetails;
            return ValueTask.CompletedTask;
        }
    }
}
