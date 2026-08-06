using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Services;

public sealed class TaskAssignmentService : ITaskAssignmentService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<TaskAssignment> _taskAssignmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskAssignmentService> _logger;

    public TaskAssignmentService(
        IRepository<TaskItem> taskRepository,
        IRepository<User> userRepository,
        IRepository<TaskAssignment> taskAssignmentRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TaskAssignmentService> logger)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TaskResponse?> AssignTaskAsync(
        int taskId,
        AssignTaskRequest request)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task is null)
        {
            _logger.LogWarning("Task assignment failed. Task not found. TaskId: {TaskId}", taskId);
            return null;
        }

        var user = await _userRepository.GetByIdAsync(request.AssignedUserId);

        if (user is null)
        {
            _logger.LogWarning(
                "Task assignment failed. Assigned user not found. TaskId: {TaskId}, AssignedUserId: {AssignedUserId}",
                taskId,
                request.AssignedUserId);

            throw new NotFoundException("Assigned user was not found.");
        }

        var existingAssignment = await _taskAssignmentRepository.GetByIdAsync(
            taskId,
            request.AssignedUserId);

        if (existingAssignment is not null)
        {
            _logger.LogWarning(
                "Task assignment failed. Task is already assigned to this user. TaskId: {TaskId}, AssignedUserId: {AssignedUserId}",
                taskId,
                request.AssignedUserId);

            throw new ConflictException("This task is already assigned to this user.");
        }

        var taskAssignment = new TaskAssignment
        {
            TaskItemId = taskId,
            AssignedUserId = request.AssignedUserId,
            AssignedDate = DateTime.UtcNow,
            IsCompleted = false
        };

        await _taskAssignmentRepository.AddAsync(taskAssignment);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task assigned successfully. TaskId: {TaskId}, AssignedUserId: {AssignedUserId}",
            taskId,
            request.AssignedUserId);

        return _mapper.Map<TaskResponse>(task);
    }
}
