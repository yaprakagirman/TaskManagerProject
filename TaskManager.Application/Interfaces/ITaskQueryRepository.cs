using TaskManager.Application.DTOs.Common;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface ITaskQueryRepository
{
    Task<PagedResponse<TaskResponse>> GetPagedAsync(
        TaskQueryRequest request,
        int? assignedUserId = null,
        bool deletedOnly = false,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(int taskId, CancellationToken cancellationToken = default);
}
