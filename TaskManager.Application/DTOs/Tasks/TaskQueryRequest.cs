using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Tasks;

public sealed class TaskQueryRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public TaskItemStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public int? ProjectId { get; set; }
    public int? AssignedUserId { get; set; }
    public int? TagId { get; set; }
    public string SortBy { get; set; } = "createdDate";
    public string SortDirection { get; set; } = "desc";
}
