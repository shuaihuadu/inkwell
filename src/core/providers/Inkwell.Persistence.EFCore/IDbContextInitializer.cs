// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore;

/// <summary>把 final adapter 之间「Migrate vs EnsureCreated」的分歧抽到接口，base 不耦合具体 Provider 行为。</summary>
public interface IDbContextInitializer
{
    /// <summary>初始化指定的数据库上下文。</summary>
    /// <param name="db">要初始化的数据库上下文。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步初始化操作的任务。</returns>
    Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default);
}
