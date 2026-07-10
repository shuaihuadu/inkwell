// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore;

/// <summary>把 final adapter 之间「Migrate vs EnsureCreated」的分歧抽到接口，base 不耦合具体 Provider 行为。</summary>
public interface IDbContextInitializer
{
    Task InitializeAsync(InkwellDbContext db, CancellationToken ct = default);
}
