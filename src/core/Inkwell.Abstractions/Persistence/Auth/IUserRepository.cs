
namespace Inkwell;

/// <summary><see cref="User"/> 具名 Repository。</summary>
public interface IUserRepository
{
    Task<User> AddUser(User user, CancellationToken ct = default);

    Task UpdateUser(User user, CancellationToken ct = default);

    Task<User> GetUser(Guid id, CancellationToken ct = default);

    Task<User> GetUserByUsername(string username, CancellationToken ct = default);

    Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default);

    Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default);
}
