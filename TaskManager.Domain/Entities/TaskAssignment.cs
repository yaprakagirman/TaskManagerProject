namespace TaskManager.Domain.Entities;

public class TaskAssignment
{
    public int TaskItemId { get; set; }

    public TaskItem TaskItem { get; set; } = null!;

    public int AssignedUserId { get; set; }

    public User AssignedUser { get; set; } = null!;

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public bool IsCompleted { get; set; } = false;

    public DateTime? CompletedDate { get; set; }
}