using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskAssignmentService
{
    Task<TaskResponse?> AssignTaskAsync(int taskId, AssignTaskRequest request);
}
