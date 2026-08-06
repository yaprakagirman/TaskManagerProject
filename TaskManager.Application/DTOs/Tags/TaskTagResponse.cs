namespace TaskManager.Application.DTOs.Tags;

public class TaskTagResponse
{
    public int Id { get; set; }

    public int TaskItemId { get; set; }

    public int TagId { get; set; }

    public DateTime CreatedDate { get; set; }
}