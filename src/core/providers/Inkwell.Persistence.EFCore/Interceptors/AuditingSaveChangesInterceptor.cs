using System.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Inkwell;

namespace Inkwell.Persistence.EFCore.Interceptors;

/// <summary>
/// <c>SaveChangesAsync</c> 前对实现三 mixin 的 Entity 自动填充 <c>CreatedTime</c>/<c>UpdatedTime</c>、
/// 校验 <see cref="IHasOwner.OwnerUserId"/> != <see cref="Guid.Empty"/>。
/// </summary>
internal sealed class AuditingSaveChangesInterceptor(TimeProvider clock, ILogger<AuditingSaveChangesInterceptor> logger) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        DbContext? context = eventData.Context;

        if (context is not null)
        {
            DateTimeOffset now = clock.GetUtcNow();

            foreach (EntityEntry entry in context.ChangeTracker.Entries())
            {
                this.ApplyTimestamps(entry, now);
                this.ValidateOwner(entry);
            }
        }

        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void ApplyTimestamps(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is not IHasTimestamps)
        {
            return;
        }

        if (entry.State == EntityState.Added)
        {
            entry.Property(nameof(IHasTimestamps.CreatedTime)).CurrentValue = now;
            entry.Property(nameof(IHasTimestamps.UpdatedTime)).CurrentValue = now;
        }
        else if (entry.State == EntityState.Modified)
        {
            entry.Property(nameof(IHasTimestamps.CreatedTime)).IsModified = false;
            entry.Property(nameof(IHasTimestamps.UpdatedTime)).CurrentValue = now;
        }
    }

    private void ValidateOwner(EntityEntry entry)
    {
        if (entry.Entity is not IHasOwner owner || entry.State is not (EntityState.Added or EntityState.Modified))
        {
            return;
        }

        if (owner.OwnerUserId == Guid.Empty)
        {
            ArgumentException ex = new ArgumentException("OwnerUserId cannot be Guid.Empty", nameof(IHasOwner.OwnerUserId));

            logger.LogError(ex, "Audit failed: OwnerUserId is empty for {EntityType} Id={EntityId}", entry.Entity.GetType().Name, entry.Property("Id").CurrentValue);
            Activity.Current?.AddException(ex);

            throw ex;
        }
    }
}
