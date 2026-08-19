using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Services;

public class TaskAssignmentServiceTests
{
    private readonly Mock<IRepository<TaskItem>> _taskRepository = new();
    private readonly Mock<IRepository<User>> _userRepository = new();
    private readonly Mock<IRepository<TaskAssignment>> _assignmentRepository = new();
    private readonly Mock<IRepository<TaskTag>> _taskTagRepository = new();
    private readonly Mock<IRepository<Tag>> _tagRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TaskAssignmentService _service;

    public TaskAssignmentServiceTests()
    {
        _taskTagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync([]);
        _tagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>() ))
            .ReturnsAsync([]);

        _service = new TaskAssignmentService(
            _taskRepository.Object,
            _userRepository.Object,
            _assignmentRepository.Object,
            _taskTagRepository.Object,
            _tagRepository.Object,
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

    [Fact]
    public async Task RemoveAssignmentAsync_ExistingAssignment_DeletesAndSaves()
    {
        var assignment = new TaskAssignment { TaskItemId = 7, AssignedUserId = 12 };
        _taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(new TaskItem { Id = 7 });
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 12)).ReturnsAsync(assignment);
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(1);

        await _service.RemoveAssignmentAsync(7, 12);

        _assignmentRepository.Verify(repository => repository.Delete(assignment), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RemoveAssignmentAsync_MissingAssignment_ThrowsNotFoundWithoutSaving()
    {
        _taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(new TaskItem { Id = 7 });
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 12)).ReturnsAsync((TaskAssignment?)null);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.RemoveAssignmentAsync(7, 12));

        _assignmentRepository.Verify(repository => repository.Delete(It.IsAny<TaskAssignment>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task TransferAssignmentAsync_EligibleUser_ReplacesAssignmentInOneSave()
    {
        var current = new TaskAssignment { TaskItemId = 7, AssignedUserId = 12 };
        _taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(new TaskItem { Id = 7 });
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 12)).ReturnsAsync(current);
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 13)).ReturnsAsync((TaskAssignment?)null);
        _userRepository.Setup(repository => repository.GetByIdAsync(13)).ReturnsAsync(new User { Id = 13 });
        _unitOfWork.Setup(unitOfWork => unitOfWork.SaveChangesAsync()).ReturnsAsync(2);

        await _service.TransferAssignmentAsync(7, new TransferTaskAssignmentRequest
        {
            CurrentAssignedUserId = 12,
            NewAssignedUserId = 13
        });

        _assignmentRepository.Verify(repository => repository.Delete(current), Times.Once);
        _assignmentRepository.Verify(repository => repository.AddAsync(It.Is<TaskAssignment>(assignment =>
            assignment.TaskItemId == 7 && assignment.AssignedUserId == 13)), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task TransferAssignmentAsync_UserLacksRequiredExpertise_ThrowsAndKeepsAssignment()
    {
        var current = new TaskAssignment { TaskItemId = 7, AssignedUserId = 12 };
        _taskRepository.Setup(repository => repository.GetByIdAsync(7)).ReturnsAsync(new TaskItem { Id = 7 });
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 12)).ReturnsAsync(current);
        _assignmentRepository.Setup(repository => repository.GetByIdAsync(7, 13)).ReturnsAsync((TaskAssignment?)null);
        _userRepository.Setup(repository => repository.GetByIdAsync(13)).ReturnsAsync(new User { Id = 13, Expertises = UserExpertise.Frontend });
        _taskTagRepository.Setup(repository => repository.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync([new TaskTag { TaskItemId = 7, TagId = 2 }]);
        _tagRepository.Setup(repository => repository.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>() ))
            .ReturnsAsync([new Tag { Id = 2, RequiredExpertise = UserExpertise.Backend }]);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.TransferAssignmentAsync(7, new TransferTaskAssignmentRequest
        {
            CurrentAssignedUserId = 12,
            NewAssignedUserId = 13
        }));

        _assignmentRepository.Verify(repository => repository.Delete(It.IsAny<TaskAssignment>()), Times.Never);
        _assignmentRepository.Verify(repository => repository.AddAsync(It.IsAny<TaskAssignment>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }

    private void VerifyNoWrite()
    {
        _assignmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<TaskAssignment>()),
            Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(), Times.Never);
    }
}
