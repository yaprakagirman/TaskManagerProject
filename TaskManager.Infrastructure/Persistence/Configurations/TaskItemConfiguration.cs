using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> entity)
    {
        entity.ToTable("Tasks");

        entity.HasKey(task => task.Id);

        entity.Property(task => task.Title)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(task => task.Description)
            .HasMaxLength(1000);

        entity.Property(task => task.Status)
            .IsRequired()
            .HasConversion<int>();

        entity.Property(task => task.Priority)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(TaskPriority.Medium);

        entity.Property(task => task.CreatedDate)
            .IsRequired();

        entity.Property(task => task.DueDate)
            .IsRequired(false);

        entity.HasOne(task => task.ParentTask)
            .WithMany(task => task.Subtasks)
            .HasForeignKey(task => task.ParentTaskId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
