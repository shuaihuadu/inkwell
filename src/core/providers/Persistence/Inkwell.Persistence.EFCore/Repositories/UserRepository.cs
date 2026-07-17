// Copyright (c) ShuaiHua Du. All rights reserved.

using Inkwell.Persistence.EFCore.Mapping;

namespace Inkwell.Persistence.EFCore.Repositories;

internal sealed class UserRepository(InkwellDbContext db) : IUserRepository
{
    public async Task<User> AddUser(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        try
        {
            UserEntity entity = user.ToEntity();

            db.Set<UserEntity>().Add(entity);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return entity.ToModel();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Duplicate key: Username={user.Username}", ex);
        }
    }

    public async Task UpdateUser(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        db.Set<UserEntity>().Update(user.ToEntity());
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<User> GetUser(Guid id, CancellationToken ct = default)
    {
        // AsNoTracking：同 AgentRepository.GetAgent 的说明，避免与 UpdateUser 产生重复追踪冲突。
        UserEntity? entity = await db.Set<UserEntity>().AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"User not found: id={id}");
    }

    public async Task<User> GetUserByUsername(string username, CancellationToken ct = default)
    {
        UserEntity? entity = await db.Set<UserEntity>().AsNoTracking().FirstOrDefaultAsync(x => x.Username == username, ct).ConfigureAwait(false);

        return entity?.ToModel() ?? throw new KeyNotFoundException($"User not found: username={username}");
    }

    public async Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default)
    {
        IOrderedQueryable<UserEntity> query = db.Set<UserEntity>().AsNoTracking().ApplySort(sort, FieldSelector);
        long total = await query.LongCountAsync(ct).ConfigureAwait(false);
        List<User> items = await query.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<User>(items, total, pagination);
    }

    public async Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default) =>
        await db.Set<UserEntity>().AsNoTracking().Where(x => x.IsLocked == isLocked).SelectAsModel().ToListAsync(ct).ConfigureAwait(false);

    private static System.Linq.Expressions.Expression<Func<UserEntity, object?>> FieldSelector(string field) => field switch
    {
        nameof(UserEntity.Username) => x => x.Username,
        nameof(UserEntity.UpdatedTime) => x => x.UpdatedTime,
        _ => x => x.CreatedTime,
    };
}
