namespace TaskManager.Application.DTOs.Tasks;

public sealed class TransferTaskAssignmentRequest
{
    public int CurrentAssignedUserId { get; set; }
    public int NewAssignedUserId { get; set; }
}
