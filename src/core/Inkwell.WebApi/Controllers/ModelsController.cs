// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.WebApi.Controllers;

/// <summary>
/// 提供模型注册表只读查询 API。
/// </summary>
[Route("api/models")]
[Authorize(Policy = AuthorizationPolicies.RequireAuthenticatedUser)]
public sealed class ModelsController(IModelRegistryService modelRegistry) : InkwellControllerBase
{
    /// <summary>
    /// 获取所有已配置或自动发现的模型。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>包含发布方、家族、能力和可用性的模型列表。</returns>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<ModelDefinition>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ModelDefinition>>> ListAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<ModelDefinition> models = await modelRegistry
            .ListModelsAsync(cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(models);
    }

    /// <summary>
    /// 获取指定模型。
    /// </summary>
    /// <param name="modelId">业务模型标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模型定义。</returns>
    [HttpGet("{modelId}")]
    [ProducesResponseType<ModelDefinition>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ModelDefinition>> GetAsync(string modelId, CancellationToken cancellationToken)
    {
        ModelDefinition model = await modelRegistry
            .GetModelAsync(modelId, cancellationToken)
            .ConfigureAwait(false);

        return this.Ok(model);
    }
}
