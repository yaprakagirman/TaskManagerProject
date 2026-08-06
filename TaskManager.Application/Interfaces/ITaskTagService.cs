using TaskManager.Application.DTOs.Tags;

namespace TaskManager.Application.Interfaces;

public interface ITaskTagService
{
    Task<IEnumerable<TaskTagResponse>> GetAllAsync(
        int? taskId,
        int? tagId);

    Task AddTasksToTagAsync(
        int tagId,
        TagTasksRequest request);

    Task RemoveAsync(int taskTagId);

    Task<TaskTagDetailResponse> GetByIdAsync(int id);

    Task<TaskTagDetailResponse> UpdateAsync(
        int id,
        UpdateTaskTagRequest request);

}