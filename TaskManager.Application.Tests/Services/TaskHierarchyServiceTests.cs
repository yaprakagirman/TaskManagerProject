using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class TaskHierarchyServiceTests
{
    private readonly Mock<IRepository<TaskItem>> _taskRepository = new();
    private readonly Mock<IRepository<Project>> _projectRepository = new();
    private readonly TaskHierarchyService _service;

    public TaskHierarchyServiceTests()
    {
        _service = new TaskHierarchyService(
            _taskRepository.Object,
            _projectRepository.Object,
            Mock.Of<ILogger<TaskHierarchyService>>());
    }

    [Fact]
    public async Task ValidateAndResolveProjectIdAsync_TaskIsOwnParent_ThrowsBadRequestException()
    {
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ValidateAndResolveProjectIdAsync(null, 5, 5));
    }

    [Fact]
    public async Task ValidateAndResolveProjectIdAsync_ParentDoesNotExist_ThrowsNotFoundException()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(8))
            .ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ValidateAndResolveProjectIdAsync(null, 8));
    }

    [Fact]
    public async Task ValidateAndResolveProjectIdAsync_ProjectDoesNotExist_ThrowsNotFoundException()
    {
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(3))
            .ReturnsAsync((Project?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.ValidateAndResolveProjectIdAsync(3, null));

        _taskRepository.Verify(
            repository => repository.GetByIdAsync(It.IsAny<object[]>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidateAndResolveProjectIdAsync_ProjectDoesNotMatchParent_ThrowsBadRequestException()
    {
        _projectRepository
            .Setup(repository => repository.GetByIdAsync(1))
            .ReturnsAsync(new Project { Id = 1 });

        _taskRepository
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(new TaskItem { Id = 10, ProjectId = 2 });

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ValidateAndResolveProjectIdAsync(1, 10));
    }

    [Fact]
    public async Task ValidateAndResolveProjectIdAsync_ProjectIsOmitted_ReturnsParentProjectId()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(10))
            .ReturnsAsync(new TaskItem { Id = 10, ProjectId = 2 });

        var result = await _service.ValidateAndResolveProjectIdAsync(null, 10);

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetTaskTreeAsync_CyclicHierarchy_ThrowsConflictException()
    {
        _taskRepository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<TaskItem>
            {
                new() { Id = 1, Title = "First", ParentTaskId = 2 },
                new() { Id = 2, Title = "Second", ParentTaskId = 1 }
            });

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.GetTaskTreeAsync(1));
    }

    [Fact]
    public async Task GetTaskTreeAsync_ValidHierarchy_ReturnsRequestedBranch()
    {
        _taskRepository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(new List<TaskItem>
            {
                new() { Id = 1, Title = "Root" },
                new() { Id = 2, Title = "Child", ParentTaskId = 1 },
                new() { Id = 3, Title = "Grandchild", ParentTaskId = 2 },
                new() { Id = 4, Title = "Other root" },
                new() { Id = 5, Title = "Other child", ParentTaskId = 4 }
            });

        var result = await _service.GetTaskTreeAsync(1);

        Assert.Equal(1, result.Id);
        var child = Assert.Single(result.Subtasks);
        Assert.Equal(2, child.Id);
        Assert.Equal(3, Assert.Single(child.Subtasks).Id);
        Assert.DoesNotContain(result.Subtasks, task => task.Id == 5);
    }
}
