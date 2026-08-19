using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;

namespace TaskManager.Application.Interfaces;

public interface ITaskAssignmentService
{
    Task<TaskResponse?> AssignTaskAsync(int taskId, AssignTaskRequest request);
    Task<List<EligibleUserResponse>> GetEligibleUsersAsync(int taskId);
    Task RemoveAssignmentAsync(int taskId, int userId);
    Task TransferAssignmentAsync(int taskId, TransferTaskAssignmentRequest request);
}
