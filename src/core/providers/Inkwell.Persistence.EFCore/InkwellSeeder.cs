// Copyright (c) ShuaiHua Du. All rights reserved.

using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inkwell.Persistence.EFCore;

/// <summary>幂等 seed 入口；启动期由 <see cref="MigrationRunner"/> 调用。</summary>
internal sealed class InkwellSeeder(InkwellDbContext db, IOptions<PersistenceOptions> options, ILogger<InkwellSeeder> logger)
{
    private const int PasswordHashIterations = 600_000;
    private const int PasswordSaltSize = 16;
    private const int PasswordHashSize = 32;

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
            string adminPassword = options.Value.Seed.AdminPassword;

            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                throw new InvalidOperationException("Configuration 'Inkwell:Persistence:Seed:AdminPassword' must not be empty.");
            }

            db.Set<UserEntity>().Add(new UserEntity
            {
                Id = Guid.CreateVersion7(),
                Username = "admin",
                PasswordHash = HashPassword(adminPassword),
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

    private static string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(PasswordSaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordHashIterations, HashAlgorithmName.SHA256, PasswordHashSize);

        return $"PBKDF2${PasswordHashIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }
}
