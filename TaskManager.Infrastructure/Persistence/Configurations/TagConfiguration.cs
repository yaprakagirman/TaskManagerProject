using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> entity)
    {
        entity.ToTable("Tags");

        entity.HasKey(tag => tag.Id);

        entity.Property(tag => tag.Name)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(tag => tag.CreatedDate)
            .IsRequired();

        entity.Property(tag => tag.RequiredExpertise)
            .HasConversion<int?>()
            .IsRequired(false);

        entity.HasIndex(tag => tag.Name)
            .IsUnique()
            .HasFilter("\"IsDeleted\" = FALSE");

        entity.HasQueryFilter(tag => !tag.IsDeleted);
    }
}
