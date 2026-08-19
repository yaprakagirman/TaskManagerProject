using System.Linq.Expressions;
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

public class ExpertiseAssignmentServiceTests
{
    private readonly Mock<IRepository<TaskItem>> _taskRepository = new();
    private readonly Mock<IRepository<User>> _userRepository = new();
    private readonly Mock<IRepository<TaskAssignment>> _assignmentRepository = new();
    private readonly Mock<IRepository<TaskTag>> _taskTagRepository = new();
    private readonly Mock<IRepository<Tag>> _tagRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly TaskAssignmentService _service;

    public ExpertiseAssignmentServiceTests()
    {
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

    [Theory]
    [InlineData(UserExpertise.Backend, UserExpertise.Backend)]
    [InlineData(
        UserExpertise.Backend,
        UserExpertise.Backend | UserExpertise.QA)]
    [InlineData(
        UserExpertise.Backend | UserExpertise.QA,
        UserExpertise.Backend | UserExpertise.QA)]
    public async Task AssignTaskAsync_UserHasAllRequirements_AllowsAssignment(
        UserExpertise required,
        UserExpertise userExpertises)
    {
        SetupAssignment(userExpertises);
        SetupRequirements(required);

        var result = await _service.AssignTaskAsync(
            7,
            new AssignTaskRequest { AssignedUserId = 12 });

        Assert.NotNull(result);
        _assignmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<TaskAssignment>()),
            Times.Once);
        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Once);
    }

    [Theory]
    [InlineData(
        UserExpertise.Backend,
        UserExpertise.Frontend,
        "Backend")]
    [InlineData(
        UserExpertise.Backend | UserExpertise.QA,
        UserExpertise.Backend,
        "QA")]
    public async Task AssignTaskAsync_UserMissingRequirement_RejectsWithoutPersisting(
        UserExpertise required,
        UserExpertise userExpertises,
        string missingExpertise)
    {
        SetupAssignment(userExpertises);
        SetupRequirements(required);

        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.AssignTaskAsync(
                7,
                new AssignTaskRequest { AssignedUserId = 12 }));

        Assert.Contains(missingExpertise, exception.Message);
        VerifyNoAssignmentWasPersisted();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AssignTaskAsync_NoExpertiseRequirement_AllowsAssignment(
        bool includeOrdinaryTag)
    {
        SetupAssignment(UserExpertise.None);
        SetupRequirements(
            UserExpertise.None,
            includeOrdinaryTag);

        var result = await _service.AssignTaskAsync(
            7,
            new AssignTaskRequest { AssignedUserId = 12 });

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetEligibleUsersAsync_ReturnsOnlyActiveUsersWithAllRequirements()
    {
        SetupTask();
        SetupRequirements(UserExpertise.Backend | UserExpertise.QA);
        SetupUsers(
            new User
            {
                Id = 1,
                Expertises = UserExpertise.Backend | UserExpertise.QA
            },
            new User { Id = 2, Expertises = UserExpertise.Backend },
            new User
            {
                Id = 3,
                Expertises = UserExpertise.Backend |
                    UserExpertise.QA |
                    UserExpertise.DevOps
            },
            new User
            {
                Id = 4,
                Expertises = UserExpertise.Backend | UserExpertise.QA,
                IsDeleted = true
            });

        var result = await _service.GetEligibleUsersAsync(7);

        Assert.Equal([1, 3], result.Select(user => user.Id));
    }

    [Fact]
    public async Task GetEligibleUsersAsync_NoRequirements_ReturnsAllActiveUsers()
    {
        SetupTask();
        SetupRequirements(UserExpertise.None);
        SetupUsers(
            new User { Id = 1 },
            new User { Id = 2, Expertises = UserExpertise.Frontend },
            new User { Id = 3, IsDeleted = true });

        var result = await _service.GetEligibleUsersAsync(7);

        Assert.Equal([1, 2], result.Select(user => user.Id));
    }

    [Fact]
    public async Task GetEligibleUsersAsync_TaskDoesNotExist_ThrowsNotFound()
    {
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(99))
            .ReturnsAsync((TaskItem?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetEligibleUsersAsync(99));

        _userRepository.Verify(
            repository => repository.GetAllAsync(),
            Times.Never);
    }

    private void SetupAssignment(UserExpertise userExpertises)
    {
        var task = SetupTask();
        _userRepository
            .Setup(repository => repository.GetByIdAsync(12))
            .ReturnsAsync(new User
            {
                Id = 12,
                Expertises = userExpertises
            });
        _assignmentRepository
            .Setup(repository => repository.GetByIdAsync(7, 12))
            .ReturnsAsync((TaskAssignment?)null);
        _assignmentRepository
            .Setup(repository => repository.AddAsync(
                It.IsAny<TaskAssignment>()))
            .Returns(Task.CompletedTask);
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.SaveChangesAsync())
            .ReturnsAsync(1);
        _mapper
            .Setup(mapper => mapper.Map<TaskResponse>(task))
            .Returns(new TaskResponse { Id = task.Id });
    }

    private TaskItem SetupTask()
    {
        var task = new TaskItem { Id = 7, Title = "Task" };
        _taskRepository
            .Setup(repository => repository.GetByIdAsync(7))
            .ReturnsAsync(task);

        return task;
    }

    private void SetupRequirements(
        UserExpertise required,
        bool includeOrdinaryTag = false)
    {
        UserExpertise[] supported =
        [
            UserExpertise.Backend,
            UserExpertise.Frontend,
            UserExpertise.QA,
            UserExpertise.DevOps
        ];
        var requirements = supported
            .Where(expertise => required.HasFlag(expertise))
            .Select(expertise => (UserExpertise?)expertise)
            .ToList();

        if (includeOrdinaryTag)
        {
            requirements.Add(null);
        }

        var taskTags = requirements
            .Select((_, index) => new TaskTag
            {
                Id = index + 1,
                TaskItemId = 7,
                TagId = index + 10
            })
            .ToList();
        var tags = requirements
            .Select((expertise, index) => new Tag
            {
                Id = index + 10,
                Name = $"tag-{index}",
                RequiredExpertise = expertise
            })
            .ToList();

        _taskTagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<TaskTag, bool>>>() ))
            .ReturnsAsync(taskTags);
        _tagRepository
            .Setup(repository => repository.FindAsync(
                It.IsAny<Expression<Func<Tag, bool>>>() ))
            .ReturnsAsync(tags);
    }

    private void SetupUsers(params User[] users)
    {
        _userRepository
            .Setup(repository => repository.GetAllAsync())
            .ReturnsAsync(users.ToList());
        _mapper
            .Setup(mapper => mapper.Map<List<EligibleUserResponse>>(
                It.IsAny<List<User>>()))
            .Returns((List<User> source) => source
                .Select(user => new EligibleUserResponse
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Expertises = user.Expertises
                })
                .ToList());
    }

    private void VerifyNoAssignmentWasPersisted()
    {
        _assignmentRepository.Verify(
            repository => repository.AddAsync(It.IsAny<TaskAssignment>()),
            Times.Never);
        _unitOfWork.Verify(
            unitOfWork => unitOfWork.SaveChangesAsync(),
            Times.Never);
    }
}
