using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.Username).IsUnique();
        b.Property(x => x.Username).IsRequired().HasMaxLength(100);
        b.Property(x => x.PasswordHash).IsRequired();
        b.HasIndex(x => x.IsLocked);
    }
}
