using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> entity)
    {
        entity.ToTable("Projects");

        entity.HasKey(project => project.Id);

        entity.Property(project => project.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(project => project.Description)
            .HasMaxLength(1000);

        entity.Property(project => project.CreatedDate)
            .IsRequired();

        entity.HasQueryFilter(project => !project.IsDeleted);

        entity.HasMany(project => project.Tasks)
            .WithOne(task => task.Project)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
