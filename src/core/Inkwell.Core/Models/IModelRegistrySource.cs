// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell;

/// <summary>
/// 定义模型注册表的数据来源。
/// </summary>
internal interface IModelRegistrySource
{
    /// <summary>
    /// 获取来源标识。
    /// </summary>
    string SourceId { get; }

    /// <summary>
    /// 获取当前来源中的模型。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>模型集合。</returns>
    Task<IReadOnlyList<ModelDefinition>> ListModelsAsync(CancellationToken cancellationToken = default);
}
