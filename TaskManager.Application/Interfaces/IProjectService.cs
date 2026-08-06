using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.DTOs.Tasks;

namespace TaskManager.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponse>> GetAllAsync();

    Task<ProjectResponse> GetByIdAsync(int id);

    Task<ProjectResponse> CreateAsync(
        CreateProjectRequest request);

    Task UpdateAsync(
        int id,
        UpdateProjectRequest request);

    Task DeleteAsync(int id);

    Task<IEnumerable<TaskResponse>> GetTasksAsync(
        int projectId);
}
