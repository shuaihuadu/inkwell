// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence;

/// <summary><see cref="User"/> 具名 Repository。</summary>
public interface IUserRepository
{
    /// <summary>新增用户。</summary>
    /// <param name="user">待新增的用户。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>已新增的用户。</returns>
    Task<User> AddUser(User user, CancellationToken ct = default);

    /// <summary>更新用户。</summary>
    /// <param name="user">待更新的用户。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>表示异步操作的任务。</returns>
    Task UpdateUser(User user, CancellationToken ct = default);

    /// <summary>获取指定用户。</summary>
    /// <param name="id">用户标识。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>用户。</returns>
    Task<User> GetUser(Guid id, CancellationToken ct = default);

    /// <summary>按用户名获取用户。</summary>
    /// <param name="username">用户名。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>用户。</returns>
    Task<User> GetUserByUsername(string username, CancellationToken ct = default);

    /// <summary>分页获取用户。</summary>
    /// <param name="pagination">分页参数。</param>
    /// <param name="sort">排序条件。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>用户分页结果。</returns>
    Task<PagedResult<User>> ListUsers(Pagination pagination, SortOrder sort, CancellationToken ct = default);

    /// <summary>按锁定状态查找用户。</summary>
    /// <param name="isLocked">是否锁定。</param>
    /// <param name="ct">取消令牌。</param>
    /// <returns>符合锁定状态的用户列表。</returns>
    Task<IReadOnlyList<User>> FindUsersByLockedStatus(bool isLocked, CancellationToken ct = default);
}
