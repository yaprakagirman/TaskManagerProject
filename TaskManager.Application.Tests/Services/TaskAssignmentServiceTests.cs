using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;

namespace TaskManager.Application.Tests.Services;

public class TaskAssignmentServiceTests
{
    private readonly Mock<IRepository<TaskItem>> _taskRepository = new();
    private readonly Mock<IRepository<User>> _userRepository = new();
    private readonly Mock<IRepository<TaskAssignment>> _assignmentRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TaskAssignmentService _service;

    public TaskAssignmentServiceTests()
    {
        _service = new TaskAssignmentService(
            _taskRepository.Object,
            _userRepository.Object,
            _assignmentRepository.Object,
            _unitOfWork.Object,
            _mapper.Object,
            Mock.Of<ILogger<TaskAssignmentService>>());
    }

    [Fact]
    public async Task AssignTaskAsync_ValidTaskAndUser_CreatesAssignment()
    {
        var before = DateTime.UtcNow;
        var task = new TaskItem { Id = 7, Title = "Task" };
        var request = new AssignTaskRequest { AssignedUserId = 12 };
        TaskAssignment? addedAssignment = null;

        _taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(task);
        _userRepository.Setup(repository => repository.GetByIdAsync(12)).ReturnsAsync(new User { Id = 12 });
        _assignmentRepository
            .Setup(repository => repository.GetByIdAsync(7, 12))
            .ReturnsAsync((TaskAssignment?)null);
        _assignmentRepository
            .Setup(repository => repository.AddAsync(It.IsAny<TaskAssignment>()))
            .Callback<TaskAssignment>(assignment => addedAssignment = assignment)
            .Returns(Task.CompletedTask);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);
        _mapper.Setup(mapper => mapper.Map<TaskResponse>(task)).Returns(new TaskResponse { Id = task.Id });

        var result = await _service.AssignTaskAsync(7, request);
        var after = DateTime.UtcNow;

        Assert.NotNull(result);
        Assert.NotNull(addedAssignment);
        Assert.Equal(7, addedAssignment.TaskItemId);
        Assert.Equal(12, addedAssignment.AssignedUserId);
        Assert.False(addedAssignment.IsCompleted);
        Assert.InRange(addedAssignment.AssignedDate, before, after);
        _assignmentRepository.Verify(repository => repository.AddAsync(addedAssignment), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AssignTaskAsync_TaskDoesNotExist_ReturnsNullWithoutSaving()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync((TaskItem?)null);

        var result = await _service.AssignTaskAsync(
            7,
            new AssignTaskRequest { AssignedUserId = 12 });

        Assert.Null(result);
        VerifyNoWrite();
    }

    [Fact]
    public async Task AssignTaskAsync_UserDoesNotExist_ThrowsNotFoundExceptionWithoutSaving()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(new TaskItem { Id = 7 });
        _userRepository
            .Setup(repository => repository.GetByIdAsync(12))
            .ReturnsAsync((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.AssignTaskAsync(7, new AssignTaskRequest { AssignedUserId = 12 }));

        VerifyNoWrite();
    }

    [Fact]
    public async Task AssignTaskAsync_AssignmentAlreadyExists_ThrowsConflictExceptionWithoutSaving()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(new TaskItem { Id = 7 });
        _userRepository
            .Setup(repository => repository.GetByIdAsync(12))
            .ReturnsAsync(new User { Id = 12 });
        _assignmentRepository
            .Setup(repository => repository.GetByIdAsync(7, 12))
            .ReturnsAsync(new TaskAssignment { TaskItemId = 7, AssignedUserId = 12 });

        await Assert.ThrowsAsync<ConflictException>(() =>
            _service.AssignTaskAsync(7, new AssignTaskRequest { AssignedUserId = 12 }));

        VerifyNoWrite();
    }

    private void VerifyNoWrite()
    {
        _assignmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<TaskAssignment>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }
}
