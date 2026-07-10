using System.Buffers.Binary;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Inkwell;

namespace Inkwell.Persistence.EFCore.Postgres.Interceptors;

/// <summary>
/// 手动模拟 RowVersion 递增（8 字节大端计数器）；Postgres 无 SqlServer 原生自动更新列，
/// 且官方推荐的 <c>xmin</c> 方案要求 <c>uint</c> CLR 类型，与 <see cref="IHasRowVersion.RowVersion"/>（<c>byte[]</c>）不兼容
/// （2026-07-06 Owner picker 选项 A）。
/// <para>
/// ⚠️ 已知未验证假设（design-review-report.md §21 B20）：本方案在真实 PostgreSQL 上是否正确工作
/// （手动赋值是否被 Npgsql 持久化写入、并发冲突是否被正确捕获）尚待 Testcontainers PostgreSQL spike 验证，
/// H5 编码任务应尽快补齐该验证，验证结果可能推翻本实现。
/// </para>
/// </summary>
internal sealed class PostgresRowVersionInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        DbContext? context = eventData.Context;

        if (context is not null)
        {
            foreach (EntityEntry entry in context.ChangeTracker.Entries())
            {
                if (entry.Entity is IHasRowVersion && entry.State is EntityState.Added or EntityState.Modified)
                {
                    ApplyNextRowVersion(entry);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private static void ApplyNextRowVersion(EntityEntry entry)
    {
        PropertyEntry property = entry.Property(nameof(IHasRowVersion.RowVersion));
        byte[]? current = (byte[]?)property.CurrentValue;
        ulong counter = current is { Length: 8 } ? BinaryPrimitives.ReadUInt64BigEndian(current) : 0UL;

        byte[] next = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(next, counter + 1);
        property.CurrentValue = next;
    }
}
