using TaskManager.Domain.Common;
using TaskManager.Domain.Enums;

namespace TaskManager.Domain.Entities;

public class TaskItem : FullAuditedEntityBase
{
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.Pending;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public DateTime? DueDate { get; set; }

    public int? ProjectId { get; set; }

    public Project? Project { get; set; }

    public int? ParentTaskId { get; set; }

    public TaskItem? ParentTask { get; set; }

    public ICollection<TaskItem> Subtasks { get; set; }
        = new List<TaskItem>();

    public int CreatedByUserId { get; set; }

    public User CreatedByUser { get; set; } = null!;

    public ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
    public ICollection<TaskTag> TaskTags { get; set; }
    = new List<TaskTag>();
}
