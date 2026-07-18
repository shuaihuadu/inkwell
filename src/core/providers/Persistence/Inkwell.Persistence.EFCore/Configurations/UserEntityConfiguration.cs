// Copyright (c) ShuaiHua Du. All rights reserved.

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> b)
    {
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Username).IsRequired().HasMaxLength(100);
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => x.IsLocked);
        b.HasIndex(x => x.IsDisabled);
    }
}
