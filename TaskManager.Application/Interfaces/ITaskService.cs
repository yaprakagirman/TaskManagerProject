using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskService
{
    Task<List<TaskResponse>> GetAllTasksAsync();

    Task<TaskDetailResponse?> GetTaskDetailAsync(int id);

    Task<TaskResponse> CreateTaskAsync(
        CreateTaskRequest request,
        int createdByUserId);

    Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request);

    Task<bool> DeleteTaskAsync(int id);

    Task<TaskResponse?> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request);
}
