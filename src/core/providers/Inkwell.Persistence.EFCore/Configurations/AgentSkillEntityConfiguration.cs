using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Inkwell.Persistence.EFCore.Entities;

namespace Inkwell.Persistence.EFCore.Configurations;

internal sealed class AgentSkillEntityConfiguration : IEntityTypeConfiguration<AgentSkillEntity>
{
    public void Configure(EntityTypeBuilder<AgentSkillEntity> b)
    {
        b.ToTable("skills");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.ContentMarkdown).IsRequired();
    }
}
