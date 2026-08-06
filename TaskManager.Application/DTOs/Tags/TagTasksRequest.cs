namespace TaskManager.Application.DTOs.Tags;

public class TagTasksRequest
{
    public List<int> TaskIds { get; set; } = new();
}