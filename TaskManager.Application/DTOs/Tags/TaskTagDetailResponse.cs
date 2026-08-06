namespace TaskManager.Application.DTOs.Tags;

public class TaskTagDetailResponse
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public string TaskTitle { get; set; } = string.Empty;

    public int TagId { get; set; }

    public string TagName { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
}