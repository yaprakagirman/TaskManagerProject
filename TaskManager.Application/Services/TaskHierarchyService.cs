using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public sealed class TaskHierarchyService : ITaskHierarchyService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<Project> _projectRepository;
    private readonly ILogger<TaskHierarchyService> _logger;

    public TaskHierarchyService(
        IRepository<TaskItem> taskRepository,
        IRepository<Project> projectRepository,
        ILogger<TaskHierarchyService> logger)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
        _logger = logger;
    }

    public async Task<int?> ValidateAndResolveProjectIdAsync(
        int? requestedProjectId,
        int? parentTaskId,
        int? taskId = null)
    {
        await EnsureProjectExistsIfProvidedAsync(requestedProjectId);

        if (taskId.HasValue && parentTaskId == taskId)
        {
            throw new BadRequestException("A task cannot be its own parent.");
        }

        var parentTask = await GetParentTaskIfProvidedAsync(parentTaskId);

        EnsureTaskProjectMatchesParent(requestedProjectId, parentTask);

        return requestedProjectId ?? parentTask?.ProjectId;
    }

    public async Task<TaskTreeResponse> GetTaskTreeAsync(int taskId)
    {
        var tasks = await _taskRepository.GetAllAsync();
        var task = tasks.FirstOrDefault(task => task.Id == taskId);

        if (task is null)
        {
            _logger.LogWarning("Task tree retrieval failed. Task not found. TaskId: {TaskId}", taskId);
            throw new NotFoundException("Task not found.");
        }

        var tasksByParentId = tasks.ToLookup(task => task.ParentTaskId);
        var taskTree = BuildTaskTree(task, tasksByParentId, new HashSet<int>());

        _logger.LogInformation("Task tree retrieved successfully. TaskId: {TaskId}", taskId);

        return taskTree;
    }

    private async Task EnsureProjectExistsIfProvidedAsync(int? projectId)
    {
        if (!projectId.HasValue)
        {
            return;
        }

        var project = await _projectRepository.GetByIdAsync(projectId.Value);

        if (project is not null)
        {
            return;
        }

        _logger.LogWarning(
            "Task creation failed. Project not found. ProjectId: {ProjectId}",
            projectId.Value);

        throw new NotFoundException("Project not found.");
    }

    private async Task<TaskItem?> GetParentTaskIfProvidedAsync(int? parentTaskId)
    {
        if (!parentTaskId.HasValue)
        {
            return null;
        }

        var parentTask = await _taskRepository.GetByIdAsync(parentTaskId.Value);

        if (parentTask is not null)
        {
            return parentTask;
        }

        _logger.LogWarning(
            "Task creation failed. Parent task not found. ParentTaskId: {ParentTaskId}",
            parentTaskId.Value);

        throw new NotFoundException("Parent task not found.");
    }

    private void EnsureTaskProjectMatchesParent(
        int? requestedProjectId,
        TaskItem? parentTask)
    {
        if (parentTask is null ||
            !requestedProjectId.HasValue ||
            requestedProjectId.Value == parentTask.ProjectId)
        {
            return;
        }

        _logger.LogWarning(
            "Task creation failed. Task project does not match parent task project. ProjectId: {ProjectId}, ParentTaskProjectId: {ParentTaskProjectId}",
            requestedProjectId.Value,
            parentTask.ProjectId);

        throw new BadRequestException("Task project must match parent task project.");
    }

    private static TaskTreeResponse BuildTaskTree(
        TaskItem task,
        ILookup<int?, TaskItem> tasksByParentId,
        HashSet<int> currentPath)
    {
        if (!currentPath.Add(task.Id))
        {
            throw new ConflictException("A cycle was detected in the task hierarchy.");
        }

        var response = new TaskTreeResponse
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            CreatedDate = task.CreatedDate,
            DueDate = task.DueDate,
            CreatedByUserId = task.CreatedByUserId,
            ProjectId = task.ProjectId,
            ParentTaskId = task.ParentTaskId
        };

        foreach (var subtask in tasksByParentId[task.Id])
        {
            response.Subtasks.Add(BuildTaskTree(
                subtask,
                tasksByParentId,
                currentPath));
        }

        currentPath.Remove(task.Id);

        return response;
    }
}
