using TaskManager.Domain.Enums;

namespace TaskManager.Application.DTOs.Tasks;

public class UpdateTaskStatusRequest
{
    public TaskItemStatus Status { get; set; }
}