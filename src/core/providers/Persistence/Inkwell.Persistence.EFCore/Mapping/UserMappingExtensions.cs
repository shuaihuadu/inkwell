// Copyright (c) ShuaiHua Du. All rights reserved.

namespace Inkwell.Persistence.EFCore.Mapping;

internal static class UserMappingExtensions
{
    public static User ToModel(this UserEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new User
        {
            Id = entity.Id,
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            IsAdmin = entity.IsAdmin,
            IsLocked = entity.IsLocked,
            IsDisabled = entity.IsDisabled,
            MustChangePassword = entity.MustChangePassword,
            SessionVersion = entity.SessionVersion,
            FailedUnlockAttempts = entity.FailedUnlockAttempts,
            LastLoginTime = entity.LastLoginTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        };
    }

    public static UserEntity ToEntity(this User model)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new UserEntity
        {
            Id = model.Id,
            Username = model.Username,
            PasswordHash = model.PasswordHash,
            IsAdmin = model.IsAdmin,
            IsLocked = model.IsLocked,
            IsDisabled = model.IsDisabled,
            MustChangePassword = model.MustChangePassword,
            SessionVersion = model.SessionVersion,
            FailedUnlockAttempts = model.FailedUnlockAttempts,
            LastLoginTime = model.LastLoginTime,
            CreatedTime = model.CreatedTime,
            UpdatedTime = model.UpdatedTime,
        };
    }

    public static IQueryable<User> SelectAsModel(this IQueryable<UserEntity> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Select(entity => new User
        {
            Id = entity.Id,
            Username = entity.Username,
            PasswordHash = entity.PasswordHash,
            IsAdmin = entity.IsAdmin,
            IsLocked = entity.IsLocked,
            IsDisabled = entity.IsDisabled,
            MustChangePassword = entity.MustChangePassword,
            SessionVersion = entity.SessionVersion,
            FailedUnlockAttempts = entity.FailedUnlockAttempts,
            LastLoginTime = entity.LastLoginTime,
            CreatedTime = entity.CreatedTime,
            UpdatedTime = entity.UpdatedTime,
        });
    }
}
