using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Tasks;

public class TaskResponse
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? DueDate { get; set; }

    public int CreatedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public int? ParentTaskId { get; set; }
}
