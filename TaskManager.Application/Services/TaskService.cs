using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Common;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public class TaskService : ITaskService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<User> _userRepository;
    private readonly ITaskDetailRepository _taskDetailRepository;
    private readonly ITaskQueryRepository _taskQueryRepository;
    private readonly ITaskHierarchyService _taskHierarchyService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskService> _logger;

    public TaskService(
        IRepository<TaskItem> taskRepository,
        IRepository<User> userRepository,
        ITaskDetailRepository taskDetailRepository,
        ITaskQueryRepository taskQueryRepository,
        ITaskHierarchyService taskHierarchyService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TaskService> logger)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _taskDetailRepository = taskDetailRepository;
        _taskQueryRepository = taskQueryRepository;
        _taskHierarchyService = taskHierarchyService;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public Task<PagedResponse<TaskResponse>> GetTasksAsync(
        TaskQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _taskQueryRepository.GetPagedAsync(request, cancellationToken: cancellationToken);
    }

    public Task<PagedResponse<TaskResponse>> GetMyTasksAsync(
        TaskQueryRequest request,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return _taskQueryRepository.GetPagedAsync(request, userId, cancellationToken: cancellationToken);
    }

    public Task<PagedResponse<TaskResponse>> GetDeletedTasksAsync(
        TaskQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        return _taskQueryRepository.GetPagedAsync(request, deletedOnly: true, cancellationToken: cancellationToken);
    }

    public async Task RestoreTaskAsync(int id, CancellationToken cancellationToken = default)
    {
        await _taskQueryRepository.RestoreAsync(id, cancellationToken);
        _logger.LogInformation("Task restored successfully. TaskId: {TaskId}", id);
    }

    public async Task<TaskDetailResponse?> GetTaskDetailAsync(int id)
    {
        var taskDetail = await _taskDetailRepository.GetByIdAsync(id);

        if (taskDetail is null)
        {
            _logger.LogWarning(
                "Task detail not found. TaskId: {TaskId}",
                id);

            return null;
        }

        _logger.LogInformation(
            "Task detail retrieved successfully. TaskId: {TaskId}",
            id);

        return taskDetail;
    }

    public async Task<TaskResponse> CreateTaskAsync(
        CreateTaskRequest request,
        int createdByUserId)
    {
        await EnsureCreatorUserExistsAsync(createdByUserId);

        var projectId = await _taskHierarchyService.ValidateAndResolveProjectIdAsync(
            request.ProjectId,
            request.ParentTaskId);

        var task = _mapper.Map<TaskItem>(request);

        task.CreatedByUserId = createdByUserId;
        task.ProjectId = projectId;
        task.Status = TaskItemStatus.Pending;

        await _taskRepository.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task created successfully. TaskId: {TaskId}, CreatedByUserId: {CreatedByUserId}",
            task.Id,
            task.CreatedByUserId);

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<TaskResponse?> UpdateTaskAsync(
        int id,
        UpdateTaskRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task update failed. Task not found. TaskId: {TaskId}", id);
            return null;
        }

        var createdByUserId = task.CreatedByUserId;

        _mapper.Map(request, task);

        task.CreatedByUserId = createdByUserId;

        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task updated successfully. TaskId: {TaskId}", task.Id);

        return _mapper.Map<TaskResponse>(task);
    }

    public async Task<bool> DeleteTaskAsync(int id)
    {
        var task = await _taskRepository.GetByIdAsync(id);

        if (task is null)
        {
            _logger.LogWarning("Task delete failed. Task not found. TaskId: {TaskId}", id);
            return false;
        }

        _taskRepository.Delete(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Task deleted successfully. TaskId: {TaskId}", id);

        return true;
    }

    public async Task<TaskResponse?> UpdateTaskStatusAsync(
        int taskId,
        UpdateTaskStatusRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task is null)
        {
            _logger.LogWarning("Task status update failed. Task not found. TaskId: {TaskId}", taskId);
            return null;
        }

        task.Status = request.Status;

        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task status updated successfully. TaskId: {TaskId}, Status: {Status}",
            task.Id,
            task.Status);

        return _mapper.Map<TaskResponse>(task);
    }

    private async Task EnsureCreatorUserExistsAsync(int createdByUserId)
    {
        var user = await _userRepository.GetByIdAsync(createdByUserId);

        if (user is not null)
        {
            return;
        }

        _logger.LogWarning(
            "Task creation failed. Creator user not found. CreatedByUserId: {CreatedByUserId}",
            createdByUserId);

        throw new NotFoundException("Creator user was not found.");
    }
}
