using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskTagConfiguration : IEntityTypeConfiguration<TaskTag>
{
    public void Configure(EntityTypeBuilder<TaskTag> entity)
    {
        entity.ToTable("TaskTags");

        entity.HasKey(taskTag => taskTag.Id);

        entity.HasIndex(taskTag => new
        {
            taskTag.TaskItemId,
            taskTag.TagId
        })
        .IsUnique();

        entity.HasOne(taskTag => taskTag.TaskItem)
            .WithMany(task => task.TaskTags)
            .HasForeignKey(taskTag => taskTag.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(taskTag => taskTag.Tag)
            .WithMany(tag => tag.TaskTags)
            .HasForeignKey(taskTag => taskTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
