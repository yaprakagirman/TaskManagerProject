using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class TaskTagServiceTests
{
    private readonly Mock<IRepository<TaskItem>> _taskRepository = new();
    private readonly Mock<IRepository<Tag>> _tagRepository = new();
    private readonly Mock<IRepository<TaskTag>> _taskTagRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly TaskTagService _service;

    public TaskTagServiceTests()
    {
        _service = new TaskTagService(
            _taskRepository.Object,
            _tagRepository.Object,
            _taskTagRepository.Object,
            _unitOfWork.Object,
            Mock.Of<ILogger<TaskTagService>>());
    }

    [Fact]
    public async Task AddTasksToTagAsync_ValidTaskAndTag_AddsRelationshipAndSaves()
    {
        var task = new TaskItem { Id = 5, Title = "Task" };
        var tag = new Tag { Id = 3, Name = "backend" };
        TaskTag? added = null;
        _tagRepository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(tag);
        _taskRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync(task);
        _taskTagRepository
            .Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync(new List<TaskTag>());
        _taskTagRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TaskTag>()))
            .Callback<TaskTag>(taskTag => added = taskTag)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);

        await _service.AddTasksToTagAsync(3, new TagTasksRequest { TaskIds = [5] });

        Assert.NotNull(added);
        Assert.Same(task, added.TaskItem);
        Assert.Same(tag, added.Tag);
        _taskTagRepository.Verify(repository => repository.AddAsync(added), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AddTasksToTagAsync_TagDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
    {
        _tagRepository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync((Tag?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AddTasksToTagAsync(3, new TagTasksRequest { TaskIds = [5] }));

        VerifyNoAddOrSave();
    }

    [Fact]
    public async Task AddTasksToTagAsync_TaskDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
    {
        _tagRepository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(new Tag { Id = 3 });
        _taskRepository.Setup(repository => repository.GetByIdAsync(5)).ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AddTasksToTagAsync(3, new TagTasksRequest { TaskIds = [5] }));

        VerifyNoAddOrSave();
    }

    [Fact]
    public async Task GetAllAsync_TaskAndTagFilters_ReturnsMatchingRelationships()
    {
        var data = new List<TaskTag>
        {
            new() { Id = 1, TaskItemId = 5, TagId = 3 },
            new() { Id = 2, TaskItemId = 5, TagId = 4 },
            new() { Id = 3, TaskItemId = 6, TagId = 3 }
        };
        _taskTagRepository
            .Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync((Expression<Func<TaskTag, bool>> predicate) =>
                data.Where(predicate.Compile()).ToList());

        var result = (await _service.GetAllAsync(5, 3)).ToList();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_RelationshipExists_ReturnsDetail()
    {
        _taskTagRepository.Setup(repository => repository.GetByIdAsync(9))
            .ReturnsAsync(new TaskTag { Id = 9, TaskItemId = 5, TagId = 3 });
        _taskRepository.Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(new TaskItem { Id = 5, Title = "Task" });
        _tagRepository.Setup(repository => repository.GetByIdAsync(3))
            .ReturnsAsync(new Tag { Id = 3, Name = "backend" });

        var result = await _service.GetByIdAsync(9);

        Assert.Equal(9, result.Id);
        Assert.Equal("Task", result.TaskTitle);
        Assert.Equal("backend", result.TagName);
    }

    [Fact]
    public async Task UpdateAsync_DuplicateRelationship_ThrowsConflictExceptionWithoutSaving()
    {
        var taskTag = new TaskTag { Id = 9, TaskItemId = 5, TagId = 2 };
        _taskTagRepository.Setup(repository => repository.GetByIdAsync(9)).ReturnsAsync(taskTag);
        _tagRepository.Setup(repository => repository.GetByIdAsync(3)).ReturnsAsync(new Tag { Id = 3 });
        _taskTagRepository
            .Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync([new TaskTag { Id = 10, TaskItemId = 5, TagId = 3 }]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateAsync(9, new UpdateTaskTagRequest { TagId = 3 }));

        _taskTagRepository.Verify(repository => repository.Update(It.IsAny<TaskTag>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_ValidNewTag_UpdatesRelationshipAndSaves()
    {
        var taskTag = new TaskTag { Id = 9, TaskItemId = 5, TagId = 2 };
        _taskTagRepository.Setup(repository => repository.GetByIdAsync(9)).ReturnsAsync(taskTag);
        _tagRepository.Setup(repository => repository.GetByIdAsync(3))
            .ReturnsAsync(new Tag { Id = 3, Name = "new" });
        _taskRepository.Setup(repository => repository.GetByIdAsync(5))
            .ReturnsAsync(new TaskItem { Id = 5, Title = "Task" });
        _taskTagRepository
            .Setup(repository => repository.FindAsync(It.IsAny<Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync(new List<TaskTag>());
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _service.UpdateAsync(9, new UpdateTaskTagRequest { TagId = 3 });

        Assert.Equal(3, taskTag.TagId);
        Assert.Equal(3, result.TagId);
        _taskTagRepository.Verify(repository => repository.Update(taskTag), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveAsync_RelationshipExists_DeletesAndSaves()
    {
        var taskTag = new TaskTag { Id = 9, TaskItemId = 5, TagId = 3 };
        _taskTagRepository.Setup(repository => repository.GetByIdAsync(9)).ReturnsAsync(taskTag);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);

        await _service.RemoveAsync(9);

        _taskTagRepository.Verify(repository => repository.Delete(taskTag), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    private void VerifyNoAddOrSave()
    {
        _taskTagRepository.Verify(repository => repository.AddAsync(It.IsAny<TaskTag>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }
}
