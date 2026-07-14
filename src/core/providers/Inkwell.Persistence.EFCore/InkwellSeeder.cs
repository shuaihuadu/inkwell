// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Diagnostics;
using Inkwell.Persistence.EFCore.Entities;
using Microsoft.Extensions.Logging;

namespace Inkwell.Persistence.EFCore;

/// <summary>幂等 seed 入口；启动期由 <see cref="MigrationRunner"/> 调用。</summary>
internal sealed class InkwellSeeder(InkwellDbContext db, ILogger<InkwellSeeder> logger)
{
    /// <summary>
    /// 默认管理员账号密码哈希（字面量，非运行时计算），明文密码 = <c>admin</c>。离线通过
    /// <c>Inkwell.Auth.PasswordHasher.Hash("admin")</c> 预先计算得出（PBKDF2-HMACSHA256，
    /// 迭代 600,000 次，盐 16 字节，输出 32 字节）。<see cref="InkwellSeeder"/> 不引用 <c>Inkwell.Auth</c>
    /// （跨层依赖，AGENTS.md §3.2 禁止），仅使用本预计算字面量。生产部署后应强制修改此默认密码。
    /// </summary>
    private const string DefaultAdminPasswordHash =
        "PBKDF2$600000$nlRFjDAWja7C0zFbWPNDGQ==$pgLLl4+b5j2/B2hF0aoFjcrgutvb4+dwl9EV4vjEWxk=";

    public async Task SeedAsync(CancellationToken ct = default)
    {
        logger.LogInformation("Seed begin");
        Stopwatch sw = Stopwatch.StartNew();

        int inserted = await this.SeedDefaultAdminAsync(ct).ConfigureAwait(false);

        sw.Stop();
        logger.LogInformation("Seed done totalSegments={N} totalInserted={M} elapsed={Ms}ms", 1, inserted, sw.ElapsedMilliseconds);
    }

    /// <summary>Seed 段：默认管理员账号（幂等，按 Username 唯一键判定，非 Id 判定）。</summary>
    private async Task<int> SeedDefaultAdminAsync(CancellationToken ct)
    {
        const string SegmentName = "DefaultAdmin";

        try
        {
            bool exists = await db.Set<UserEntity>().AnyAsync(x => x.Username == "admin", ct).ConfigureAwait(false);

            if (exists)
            {
                logger.LogInformation("Seed {SegmentName} ok inserted={NewRowCount}", SegmentName, 0);

                return 0;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            db.Set<UserEntity>().Add(new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = "admin",
                PasswordHash = DefaultAdminPasswordHash,
                IsSuper = true,
                IsLocked = false,
                FailedUnlockAttempts = 0,
                CreatedTime = now,
                UpdatedTime = now,
            });
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            logger.LogInformation("Seed {SegmentName} ok inserted={NewRowCount}", SegmentName, 1);

            return 1;
        }
        catch (DbUpdateException dbEx)
        {
            // 并发场景下两个实例都可能通过上方预检查后尝试插入；数据库 Username 唯一索引拦下重复数据，
            // 这里将唯一约束冲突当作已被其他实例种过的正常幂等结果处理，不向上抛异常（ADR-024 §幂等性保证）。
            logger.LogInformation(dbEx, "Seed {SegmentName} skipped: already seeded by another instance (unique constraint conflict)", SegmentName);

            return 0;
        }
        catch (Exception inner) when (inner is not OperationCanceledException)
        {
            logger.LogError(inner, "Seed {SegmentName} failed", SegmentName);
            Activity.Current?.AddException(inner);
            throw new InvalidOperationException($"Seeder segment '{SegmentName}' failed", inner);
        }
    }
}
