using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Services;

public sealed class TaskAssignmentService : ITaskAssignmentService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<TaskAssignment> _taskAssignmentRepository;
    private readonly IRepository<TaskTag> _taskTagRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<TaskAssignmentService> _logger;

    public TaskAssignmentService(
        IRepository<TaskItem> taskRepository,
        IRepository<User> userRepository,
        IRepository<TaskAssignment> taskAssignmentRepository,
        IRepository<TaskTag> taskTagRepository,
        IRepository<Tag> tagRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<TaskAssignmentService> logger)
    {
        _taskRepository = taskRepository;
        _userRepository = userRepository;
        _taskAssignmentRepository = taskAssignmentRepository;
        _taskTagRepository = taskTagRepository;
        _tagRepository = tagRepository;
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

        var requiredExpertises = await GetRequiredExpertisesAsync(taskId);

        EnsureUserHasRequiredExpertises(
            user,
            requiredExpertises,
            taskId);

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

    public async Task<List<EligibleUserResponse>> GetEligibleUsersAsync(
        int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task is null)
        {
            _logger.LogWarning(
                "Eligible user listing failed. Task not found. TaskId: {TaskId}",
                taskId);

            throw new NotFoundException("Task was not found.");
        }

        var requiredExpertises = await GetRequiredExpertisesAsync(taskId);
        var users = await _userRepository.GetAllAsync();
        var eligibleUsers = users
            .Where(user =>
                !user.IsDeleted &&
                HasAllRequiredExpertises(
                    user.Expertises,
                    requiredExpertises))
            .ToList();

        _logger.LogInformation(
            "Eligible users listed. TaskId: {TaskId}, RequiredExpertises: {RequiredExpertises}, EligibleUserCount: {EligibleUserCount}",
            taskId,
            requiredExpertises,
            eligibleUsers.Count);

        return _mapper.Map<List<EligibleUserResponse>>(eligibleUsers);
    }

    public async Task RemoveAssignmentAsync(int taskId, int userId)
    {
        if (await _taskRepository.GetByIdAsync(taskId) is null)
        {
            throw new NotFoundException("Task was not found.");
        }

        var assignment = await _taskAssignmentRepository.GetByIdAsync(taskId, userId);
        if (assignment is null)
        {
            throw new NotFoundException("Task assignment was not found.");
        }

        _taskAssignmentRepository.Delete(assignment);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation(
            "Task assignment removed. TaskId: {TaskId}, AssignedUserId: {AssignedUserId}",
            taskId,
            userId);
    }

    public async Task TransferAssignmentAsync(
        int taskId,
        TransferTaskAssignmentRequest request)
    {
        if (request.CurrentAssignedUserId == request.NewAssignedUserId)
        {
            throw new BadRequestException("Current and new assigned users must be different.");
        }

        if (await _taskRepository.GetByIdAsync(taskId) is null)
        {
            throw new NotFoundException("Task was not found.");
        }

        var currentAssignment = await _taskAssignmentRepository.GetByIdAsync(taskId, request.CurrentAssignedUserId);
        if (currentAssignment is null)
        {
            throw new NotFoundException("Current task assignment was not found.");
        }

        var newUser = await _userRepository.GetByIdAsync(request.NewAssignedUserId)
            ?? throw new NotFoundException("New assigned user was not found.");

        if (await _taskAssignmentRepository.GetByIdAsync(taskId, request.NewAssignedUserId) is not null)
        {
            throw new ConflictException("This task is already assigned to the new user.");
        }

        EnsureUserHasRequiredExpertises(newUser, await GetRequiredExpertisesAsync(taskId), taskId);

        _taskAssignmentRepository.Delete(currentAssignment);
        await _taskAssignmentRepository.AddAsync(new TaskAssignment
        {
            TaskItemId = taskId,
            AssignedUserId = request.NewAssignedUserId,
            AssignedDate = DateTime.UtcNow
        });
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task assignment transferred. TaskId: {TaskId}, PreviousAssignedUserId: {PreviousAssignedUserId}, NewAssignedUserId: {NewAssignedUserId}",
            taskId,
            request.CurrentAssignedUserId,
            request.NewAssignedUserId);
    }

    private async Task<UserExpertise> GetRequiredExpertisesAsync(int taskId)
    {
        var taskTags = await _taskTagRepository.FindAsync(
            taskTag => taskTag.TaskItemId == taskId);

        if (taskTags.Count == 0)
        {
            return UserExpertise.None;
        }

        var tagIds = taskTags
            .Select(taskTag => taskTag.TagId)
            .Distinct()
            .ToList();
        var tags = await _tagRepository.FindAsync(
            tag => tagIds.Contains(tag.Id));

        return tags
            .Where(tag => tag.RequiredExpertise.HasValue)
            .Aggregate(
                UserExpertise.None,
                (combined, tag) =>
                    combined | tag.RequiredExpertise!.Value);
    }

    private void EnsureUserHasRequiredExpertises(
        User user,
        UserExpertise requiredExpertises,
        int taskId)
    {
        if (HasAllRequiredExpertises(
            user.Expertises,
            requiredExpertises))
        {
            return;
        }

        var missingExpertises = GetIndividualExpertises(
            requiredExpertises & ~user.Expertises);

        _logger.LogWarning(
            "Task assignment failed. User is missing required expertises. TaskId: {TaskId}, AssignedUserId: {AssignedUserId}, MissingExpertises: {MissingExpertises}",
            taskId,
            user.Id,
            missingExpertises);

        throw new BadRequestException(
            $"The user does not have the required task expertises: {string.Join(", ", missingExpertises)}.");
    }

    private static bool HasAllRequiredExpertises(
        UserExpertise userExpertises,
        UserExpertise requiredExpertises)
    {
        return (userExpertises & requiredExpertises) ==
            requiredExpertises;
    }

    private static List<UserExpertise> GetIndividualExpertises(
        UserExpertise expertises)
    {
        UserExpertise[] supportedExpertises =
        [
            UserExpertise.Backend,
            UserExpertise.Frontend,
            UserExpertise.QA,
            UserExpertise.DevOps
        ];

        return supportedExpertises
            .Where(expertise => expertises.HasFlag(expertise))
            .ToList();
    }
}
