using Microsoft.Extensions.Logging;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Domain.Entities;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Services;

public class TaskTagService : ITaskTagService
{
    private readonly IRepository<TaskItem> _taskRepository;
    private readonly IRepository<Tag> _tagRepository;
    private readonly IRepository<TaskTag> _taskTagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<TaskTagService> _logger;

    public TaskTagService(
        IRepository<TaskItem> taskRepository,
        IRepository<Tag> tagRepository,
        IRepository<TaskTag> taskTagRepository,
        IUnitOfWork unitOfWork,
        ILogger<TaskTagService> logger)
    {
        _taskRepository = taskRepository;
        _tagRepository = tagRepository;
        _taskTagRepository = taskTagRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IEnumerable<TaskTagResponse>> GetAllAsync(
    int? taskId,
    int? tagId)
    {
        var taskTags = await _taskTagRepository.FindAsync(
            taskTag =>
                (!taskId.HasValue ||
                 taskTag.TaskItemId == taskId.Value) &&
                (!tagId.HasValue ||
                 taskTag.TagId == tagId.Value));

        return taskTags.Select(taskTag => new TaskTagResponse
        {
            Id = taskTag.Id,
            TaskItemId = taskTag.TaskItemId,
            TagId = taskTag.TagId,
            CreatedDate = taskTag.CreatedDate
        });
    }

    public async Task<TaskTagDetailResponse> GetByIdAsync(int id)
    {
        var taskTag = await _taskTagRepository.GetByIdAsync(id);

        if (taskTag is null)
        {
            _logger.LogWarning(
                "Task tag relationship not found. TaskTagId: {TaskTagId}",
                id);

            throw new NotFoundException(
                "Task tag relationship was not found.");
        }

        var task = await _taskRepository.GetByIdAsync(
            taskTag.TaskItemId);

        if (task is null)
        {
            _logger.LogWarning(
                "Task not found for task tag relationship. TaskId: {TaskId}",
                taskTag.TaskItemId);

            throw new NotFoundException(
                "Task was not found.");
        }

        var tag = await _tagRepository.GetByIdAsync(
            taskTag.TagId);

        if (tag is null)
        {
            _logger.LogWarning(
                "Tag not found for task tag relationship. TagId: {TagId}",
                taskTag.TagId);

            throw new NotFoundException(
                "Tag was not found.");
        }

        return new TaskTagDetailResponse
        {
            Id = taskTag.Id,
            TaskItemId = taskTag.TaskItemId,
            TaskTitle = task.Title,
            TagId = taskTag.TagId,
            TagName = tag.Name,
            CreatedDate = taskTag.CreatedDate
        };
    }

    public async Task<TaskTagDetailResponse> UpdateAsync(
        int id,
        UpdateTaskTagRequest request)
    {
        var taskTag = await _taskTagRepository.GetByIdAsync(id);

        if (taskTag is null)
        {
            _logger.LogWarning(
                "Task tag relationship not found. TaskTagId: {TaskTagId}",
                id);

            throw new NotFoundException(
                "Task tag relationship was not found.");
        }

        var newTag = await _tagRepository.GetByIdAsync(
            request.TagId);

        if (newTag is null)
        {
            _logger.LogWarning(
                "Tag not found while updating task tag. TagId: {TagId}",
                request.TagId);

            throw new NotFoundException(
                "Tag was not found.");
        }

        if (taskTag.TagId == request.TagId)
        {
            _logger.LogInformation(
                "Task tag update skipped because the tag did not change. TaskTagId: {TaskTagId}",
                id);

            return await GetByIdAsync(id);
        }

        var duplicateTaskTags =
            await _taskTagRepository.FindAsync(
                existingTaskTag =>
                    existingTaskTag.TaskItemId ==
                        taskTag.TaskItemId &&
                    existingTaskTag.TagId ==
                        request.TagId &&
                    existingTaskTag.Id != id);

        if (duplicateTaskTags.Any())
        {
            _logger.LogWarning(
                "Task already has the requested tag. TaskId: {TaskId}, TagId: {TagId}",
                taskTag.TaskItemId,
                request.TagId);

            throw new ConflictException(
                "This tag is already assigned to the task.");
        }

        var oldTagId = taskTag.TagId;

        taskTag.TagId = request.TagId;

        _taskTagRepository.Update(taskTag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task tag relationship updated. TaskTagId: {TaskTagId}, OldTagId: {OldTagId}, NewTagId: {NewTagId}",
            taskTag.Id,
            oldTagId,
            request.TagId);

        return await GetByIdAsync(id);
    }
    

    public async Task AddTasksToTagAsync(
        int tagId,
        TagTasksRequest request)
    {
        var tag = await GetTagOrThrowAsync(tagId);
        var tasks = new List<TaskItem>();

        foreach (var taskId in request.TaskIds)
        {
            var task = await GetTaskOrThrowAsync(taskId);
            tasks.Add(task);
        }

        var addedTaskCount = 0;

        foreach (var task in tasks)
        {
            var existingTaskTags = await _taskTagRepository.FindAsync(
                taskTag =>
                    taskTag.TaskItemId == task.Id &&
                    taskTag.TagId == tagId);

            if (existingTaskTags.Any())
            {
                continue;
            }

            var taskTag = new TaskTag
            {
                TaskItem = task,
                Tag = tag
            };

            await _taskTagRepository.AddAsync(taskTag);
            addedTaskCount++;
        }

        if (addedTaskCount > 0)
        {
            await _unitOfWork.SaveChangesAsync();
        }

        _logger.LogInformation(
            "Tasks added to tag. TagId: {TagId}, RequestedTaskCount: {RequestedTaskCount}, AddedTaskCount: {AddedTaskCount}",
            tagId,
            request.TaskIds.Count,
            addedTaskCount);
    }

    public async Task RemoveAsync(int taskTagId)
    {
        var taskTag = await _taskTagRepository
            .GetByIdAsync(taskTagId);

        if (taskTag is null)
        {
            _logger.LogWarning(
                "Task tag relationship not found. TaskTagId: {TaskTagId}",
                taskTagId);

            throw new NotFoundException(
                "Task tag relationship was not found.");
        }

        _taskTagRepository.Delete(taskTag);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation(
            "Task tag relationship deleted. TaskTagId: {TaskTagId}",
            taskTagId);
    }

    private async Task<Tag> GetTagOrThrowAsync(int tagId)
    {
        var tag = await _tagRepository.GetByIdAsync(tagId);

        if (tag is not null)
        {
            return tag;
        }

        _logger.LogWarning(
            "Tag not found while adding tasks. TagId: {TagId}",
            tagId);

        throw new NotFoundException(
            "Tag was not found.");
    }

    private async Task<TaskItem> GetTaskOrThrowAsync(int taskId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);

        if (task is not null)
        {
            return task;
        }

        _logger.LogWarning(
            "Task not found while adding tag. TaskId: {TaskId}",
            taskId);

        throw new NotFoundException(
            "Task was not found.");
    }
}
