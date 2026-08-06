using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Services;

public class TaskServiceTests
{
    [Fact]
    public void CreateTaskRequest_DoesNotExposeCreatedByUserId()
    {
        var property = typeof(CreateTaskRequest)
            .GetProperty(nameof(TaskItem.CreatedByUserId));

        Assert.Null(property);
    }

    [Fact]
    public async Task CreateTaskAsync_UsesAuthenticatedUserIdAsCreator()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var userRepository = new Mock<IRepository<User>>();
        var taskDetailRepository = new Mock<ITaskDetailRepository>();
        var taskHierarchyService = new Mock<ITaskHierarchyService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<TaskService>>();

        const int authenticatedUserId = 42;
        var request = new CreateTaskRequest
        {
            Title = "Secure task"
        };

        userRepository
            .Setup(repository => repository.GetByIdAsync(authenticatedUserId))
            .ReturnsAsync(new User { Id = authenticatedUserId });

        taskHierarchyService
            .Setup(service => service.ValidateAndResolveProjectIdAsync(
                null,
                null,
                null))
            .ReturnsAsync((int?)null);

        var mappedTask = new TaskItem
        {
            Title = request.Title
        };

        mapper
            .Setup(currentMapper => currentMapper.Map<TaskItem>(request))
            .Returns(mappedTask);

        mapper
            .Setup(currentMapper => currentMapper.Map<TaskResponse>(mappedTask))
            .Returns(() => new TaskResponse
            {
                Id = mappedTask.Id,
                Title = mappedTask.Title,
                CreatedByUserId = mappedTask.CreatedByUserId
            });

        unitOfWork
            .Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new TaskService(
            taskRepository.Object,
            userRepository.Object,
            taskDetailRepository.Object,
            taskHierarchyService.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        var response = await service.CreateTaskAsync(
            request,
            authenticatedUserId);

        Assert.Equal(authenticatedUserId, mappedTask.CreatedByUserId);
        Assert.Equal(authenticatedUserId, response.CreatedByUserId);
        Assert.Equal(TaskItemStatus.Pending, mappedTask.Status);

        userRepository.Verify(
            repository => repository.GetByIdAsync(authenticatedUserId),
            Times.Once);

        taskRepository.Verify(
            repository => repository.AddAsync(mappedTask),
            Times.Once);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task UpdateTaskAsync_PreservesExistingCreatedByUserId()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var userRepository = new Mock<IRepository<User>>();
        var taskDetailRepository = new Mock<ITaskDetailRepository>();
        var taskHierarchyService = new Mock<ITaskHierarchyService>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var mapper = new Mock<IMapper>();
        var logger = new Mock<ILogger<TaskService>>();

        var task = new TaskItem
        {
            Id = 7,
            Title = "Old title",
            CreatedByUserId = 42
        };

        var request = new UpdateTaskRequest
        {
            Title = "Updated title"
        };

        taskRepository
            .Setup(repository => repository.GetByIdAsync(task.Id))
            .ReturnsAsync(task);

        mapper
            .Setup(currentMapper => currentMapper.Map(request, task))
            .Callback(() =>
            {
                task.Title = request.Title;
                task.CreatedByUserId = 999;
            })
            .Returns(task);

        mapper
            .Setup(currentMapper => currentMapper.Map<TaskResponse>(task))
            .Returns(() => new TaskResponse
            {
                Id = task.Id,
                Title = task.Title,
                CreatedByUserId = task.CreatedByUserId
            });

        unitOfWork
            .Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);

        var service = new TaskService(
            taskRepository.Object,
            userRepository.Object,
            taskDetailRepository.Object,
            taskHierarchyService.Object,
            unitOfWork.Object,
            mapper.Object,
            logger.Object);

        var response = await service.UpdateTaskAsync(task.Id, request);

        Assert.NotNull(response);
        Assert.Equal(42, task.CreatedByUserId);
        Assert.Equal(42, response.CreatedByUserId);
        Assert.Equal("Updated title", task.Title);

        taskRepository.Verify(
            repository => repository.Update(task),
            Times.Once);

        unitOfWork.Verify(
            currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(),
            Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatorDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var userRepository = new Mock<IRepository<User>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        userRepository
            .Setup(repository => repository.GetByIdAsync(42))
            .ReturnsAsync((User?)null);

        var service = CreateService(taskRepository, userRepository, unitOfWork);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CreateTaskAsync(new CreateTaskRequest { Title = "Task" }, 42));

        taskRepository.Verify(repository => repository.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task UpdateTaskAsync_TaskDoesNotExist_ReturnsNullWithoutSaving()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService(taskRepository, unitOfWork: unitOfWork);

        var result = await service.UpdateTaskAsync(7, new UpdateTaskRequest { Title = "Updated" });

        Assert.Null(result);
        taskRepository.Verify(repository => repository.Update(It.IsAny<TaskItem>()), Times.Never);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task DeleteTaskAsync_TaskExists_DeletesAndSaves()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var task = new TaskItem { Id = 7, Title = "Task" };
        taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(task);
        unitOfWork.Setup(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync()).ReturnsAsync(1);

        var service = CreateService(taskRepository, unitOfWork: unitOfWork);

        var result = await service.DeleteTaskAsync(7);

        Assert.True(result);
        taskRepository.Verify(repository => repository.Delete(task), Times.Once);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteTaskAsync_TaskDoesNotExist_ReturnsFalseWithoutSaving()
    {
        var taskRepository = new Mock<IRepository<TaskItem>>();
        var unitOfWork = new Mock<IUnitOfWork>();
        taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync((TaskItem?)null);

        var service = CreateService(taskRepository, unitOfWork: unitOfWork);

        var result = await service.DeleteTaskAsync(7);

        Assert.False(result);
        taskRepository.Verify(repository => repository.Delete(It.IsAny<TaskItem>()), Times.Never);
        unitOfWork.Verify(currentUnitOfWork => currentUnitOfWork.SaveChangesAsync(), Times.Never);
    }

    private static TaskService CreateService(
        Mock<IRepository<TaskItem>> taskRepository,
        Mock<IRepository<User>>? userRepository = null,
        Mock<IUnitOfWork>? unitOfWork = null)
    {
        return new TaskService(
            taskRepository.Object,
            (userRepository ?? new Mock<IRepository<User>>()).Object,
            Mock.Of<ITaskDetailRepository>(),
            Mock.Of<ITaskHierarchyService>(),
            (unitOfWork ?? new Mock<IUnitOfWork>()).Object,
            Mock.Of<IMapper>(),
            Mock.Of<ILogger<TaskService>>());
    }
}
