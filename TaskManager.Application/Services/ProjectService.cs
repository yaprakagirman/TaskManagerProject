using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IRepository<Project> _projectRepository;
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ProjectService> _logger;

    public ProjectService(
        IRepository<Project> projectRepository,
        IRepository<TaskItem> taskRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ProjectService> logger)
    {
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProjectResponse>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();

        _logger.LogInformation("All projects listed. Count: {ProjectCount}", projects.Count);

        return _mapper.Map<List<ProjectResponse>>(projects);
    }

    public async Task<ProjectResponse> GetByIdAsync(int id)
    {
        var project = await GetProjectOrThrowAsync(id);

        _logger.LogInformation("Project retrieved successfully. ProjectId: {ProjectId}", id);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request)
    {
        var project = _mapper.Map<Project>(request);

        project.CreatedDate = DateTime.UtcNow;

        await _projectRepository.AddAsync(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Project created successfully. ProjectId: {ProjectId}",
            project.Id);

        return _mapper.Map<ProjectResponse>(project);
    }

    public async Task UpdateAsync(int id, UpdateProjectRequest request)
    {
        var project = await GetProjectOrThrowAsync(id);

        _mapper.Map(request, project);

        _projectRepository.Update(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Project updated successfully. ProjectId: {ProjectId}", project.Id);
    }

    public async Task DeleteAsync(int id)
    {
        var project = await GetProjectOrThrowAsync(id);

        var assignedTasks = await _taskRepository.FindAsync(task => task.ProjectId == id);

        if (assignedTasks.Any())
        {
            _logger.LogWarning(
                "Project delete failed. Project has assigned tasks. ProjectId: {ProjectId}",
                id);

            throw new ConflictException(
                "Project cannot be deleted because it has assigned tasks.");
        }

        _projectRepository.Delete(project);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Project deleted successfully. ProjectId: {ProjectId}", id);
    }

    public async Task<IEnumerable<TaskResponse>> GetTasksAsync(int projectId)
    {
        await GetProjectOrThrowAsync(projectId);

        var tasks = await _taskRepository.FindAsync(task => task.ProjectId == projectId);

        _logger.LogInformation(
            "Project tasks listed. ProjectId: {ProjectId}, TaskCount: {TaskCount}",
            projectId,
            tasks.Count);

        return _mapper.Map<List<TaskResponse>>(tasks);
    }

    private async Task<Project> GetProjectOrThrowAsync(int id)
    {
        var project = await _projectRepository.GetByIdAsync(id);

        if (project is null)
        {
            _logger.LogWarning("Project not found. ProjectId: {ProjectId}", id);
            throw new NotFoundException("Project not found.");
        }

        return project;
    }
}
