using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManager.Domain.Entities;

namespace TaskManager.Infrastructure.Persistence.Configurations;

public sealed class TaskAssignmentConfiguration : IEntityTypeConfiguration<TaskAssignment>
{
    public void Configure(EntityTypeBuilder<TaskAssignment> entity)
    {
        entity.ToTable("TaskAssignments");

        entity.HasKey(taskAssignment => new
        {
            taskAssignment.TaskItemId,
            taskAssignment.AssignedUserId
        });

        entity.Property(taskAssignment => taskAssignment.AssignedDate)
            .IsRequired();

        entity.Property(taskAssignment => taskAssignment.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(taskAssignment => taskAssignment.CompletedDate)
            .IsRequired(false);

        entity.HasQueryFilter(taskAssignment =>
            !taskAssignment.TaskItem.IsDeleted &&
            !taskAssignment.AssignedUser.IsDeleted);

        entity.HasOne(taskAssignment => taskAssignment.TaskItem)
            .WithMany(task => task.TaskAssignments)
            .HasForeignKey(taskAssignment => taskAssignment.TaskItemId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(taskAssignment => taskAssignment.AssignedUser)
            .WithMany(user => user.TaskAssignments)
            .HasForeignKey(taskAssignment => taskAssignment.AssignedUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
