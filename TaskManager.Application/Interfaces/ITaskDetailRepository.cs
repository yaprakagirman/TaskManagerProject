using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskDetailRepository
{
    Task<TaskDetailResponse?> GetByIdAsync(int taskId);
}