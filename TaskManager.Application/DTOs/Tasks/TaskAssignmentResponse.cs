namespace TaskManager.Application.DTOs.Tasks;

public class TaskAssignmentResponse
{
    public int AssignedUserId { get; set; }

    public string AssignedUserFullName { get; set; }
        = string.Empty;

    public DateTime AssignedDate { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedDate { get; set; }
}