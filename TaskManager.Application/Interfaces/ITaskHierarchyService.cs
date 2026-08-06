using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskHierarchyService
{
    Task<int?> ValidateAndResolveProjectIdAsync(
        int? requestedProjectId,
        int? parentTaskId,
        int? taskId = null);

    Task<TaskTreeResponse> GetTaskTreeAsync(int taskId);
}
