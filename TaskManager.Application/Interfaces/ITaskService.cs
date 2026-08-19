using TaskManager.Application.DTOs.Common;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskService
{
    Task<PagedResponse<TaskResponse>> GetTasksAsync(TaskQueryRequest request, CancellationToken cancellationToken = default);
    Task<PagedResponse<TaskResponse>> GetMyTasksAsync(TaskQueryRequest request, int userId, CancellationToken cancellationToken = default);
    Task<PagedResponse<TaskResponse>> GetDeletedTasksAsync(TaskQueryRequest request, CancellationToken cancellationToken = default);
    Task RestoreTaskAsync(int id, CancellationToken cancellationToken = default);
    Task<TaskDetailResponse?> GetTaskDetailAsync(int id);
    Task<TaskResponse> CreateTaskAsync(CreateTaskRequest request, int createdByUserId);
    Task<TaskResponse?> UpdateTaskAsync(int id, UpdateTaskRequest request);
    Task<bool> DeleteTaskAsync(int id);
    Task<TaskResponse?> UpdateTaskStatusAsync(int taskId, UpdateTaskStatusRequest request);
}
