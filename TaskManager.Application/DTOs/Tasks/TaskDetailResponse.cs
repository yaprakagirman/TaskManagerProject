using TaskManager.Application.DTOs.Tags;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Tasks;

public class TaskDetailResponse
{
    public int Id { get; set; }

    public string Title { get; set; }
        = string.Empty;

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; }

    public TaskPriority Priority { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? DueDate { get; set; }

    public int CreatedByUserId { get; set; }

    public string CreatedByUserFullName { get; set; }
        = string.Empty;

    public int? ProjectId { get; set; }

    public string? ProjectName { get; set; }

    public int? ParentTaskId { get; set; }

    public List<TagResponse> Tags { get; set; }
        = [];

    public List<TaskAssignmentResponse> Assignments { get; set; }
        = [];
}