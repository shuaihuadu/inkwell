// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供当前 LLM Provider 的实时模型查询和测试 API。
/// </summary>
[Route("api/models")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class ModelsController(ILLMProvider llmProvider) : InkwellControllerBase
{
    /// <summary>
    /// 获取当前 Provider 可访问的全部模型。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>实时模型列表。</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LLMModel>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LLMModel>>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<LLMModel> models = await llmProvider
            .ListModelsAsync(cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(models);
    }

    /// <summary>
    /// 获取指定模型。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模型定义。</returns>
    [HttpGet("{modelId}")]
    [ProducesResponseType<LLMModel>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LLMModel>> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        LLMModel model = await llmProvider
            .GetModelAsync(modelId, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(model);
    }

    /// <summary>
    /// 测试指定模型的连通性。
    /// </summary>
    /// <param name="modelId">模型标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>脱敏后的连通性测试结果。</returns>
    [HttpPost("{modelId}/test")]
    [Authorize(Policy = AuthorizationPolicies.RequireSuperUser)]
    [ProducesResponseType<LLMModelTestResult>(StatusCodes.Status200OK)]
    public async Task<ActionResult<LLMModelTestResult>> TestAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        LLMModelTestResult result = await llmProvider
            .TestModelAsync(modelId, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(result);
    }
}
