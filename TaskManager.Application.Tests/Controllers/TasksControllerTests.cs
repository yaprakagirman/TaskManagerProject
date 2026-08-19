using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Validators.Tasks;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Controllers;

public class TasksControllerTests
{
    [Fact]
    public async Task Create_InvalidRequest_ReturnsBadRequestWithoutCallingService()
    {
        var taskService = new Mock<ITaskService>();
        var controller = CreateController(taskService, "42");
        var request = new CreateTaskRequest { Title = string.Empty };

        var result = await controller.Create(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        taskService.Verify(
            service => service.CreateTaskAsync(
                It.IsAny<CreateTaskRequest>(),
                It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Create_ValidNameIdentifierClaim_PassesAuthenticatedUserIdToService()
    {
        var taskService = new Mock<ITaskService>();
        var request = new CreateTaskRequest { Title = "Secure task" };
        taskService
            .Setup(service => service.CreateTaskAsync(request, 42))
            .ReturnsAsync(new TaskResponse { Id = 7, Title = request.Title, CreatedByUserId = 42 });
        var controller = CreateController(taskService, "42");

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<TaskResponse>(ok.Value);
        Assert.Equal(42, response.CreatedByUserId);
        taskService.Verify(service => service.CreateTaskAsync(request, 42), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-an-integer")]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task Create_InvalidNameIdentifierClaim_ThrowsUnauthorizedExceptionWithoutCallingService(
        string? claimValue)
    {
        var taskService = new Mock<ITaskService>();
        var controller = CreateController(taskService, claimValue);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            controller.Create(new CreateTaskRequest { Title = "Secure task" }));

        taskService.Verify(
            service => service.CreateTaskAsync(It.IsAny<CreateTaskRequest>(), It.IsAny<int>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_TaskDoesNotExist_ReturnsNotFound()
    {
        var taskService = new Mock<ITaskService>();
        var request = new UpdateTaskRequest
        {
            Title = "Updated task",
            Priority = TaskPriority.Medium
        };
        taskService
            .Setup(service => service.UpdateTaskAsync(17, request))
            .ReturnsAsync((TaskResponse?)null);
        var controller = CreateController(taskService, "42");

        var result = await controller.Update(17, request);

        Assert.IsType<NotFoundResult>(result.Result);
        taskService.Verify(
            service => service.UpdateTaskAsync(17, request),
            Times.Once);
    }

    [Fact]
    public async Task Assign_InvalidAssignedUserId_ReturnsBadRequestWithoutCallingService()
    {
        var taskService = new Mock<ITaskService>();
        var assignmentService = new Mock<ITaskAssignmentService>();
        var controller = CreateController(
            taskService,
            "42",
            assignmentService);
        var request = new AssignTaskRequest { AssignedUserId = 0 };

        var result = await controller.Assign(9, request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        assignmentService.Verify(
            service => service.AssignTaskAsync(
                It.IsAny<int>(),
                It.IsAny<AssignTaskRequest>()),
            Times.Never);
    }

    private static TasksController CreateController(
        Mock<ITaskService> taskService,
        string? claimValue,
        Mock<ITaskAssignmentService>? assignmentService = null)
    {
        var controller = new TasksController(
            taskService.Object,
            CreateCurrentUserService(claimValue),
            assignmentService?.Object ?? Mock.Of<ITaskAssignmentService>(),
            Mock.Of<ITaskHierarchyService>(),
            new CreateTaskRequestValidator(),
            new UpdateTaskRequestValidator(),
            new AssignTaskRequestValidator(),
            new UpdateTaskStatusRequestValidator(),
            new TaskQueryRequestValidator(),
            new TransferTaskAssignmentRequestValidator());

        return controller;
    }

    private static ICurrentUserService CreateCurrentUserService(
        string? claimValue)
    {
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService
            .Setup(service => service.UserId)
            .Returns(int.TryParse(claimValue, out var userId) && userId > 0
                ? userId
                : null);

        return currentUserService.Object;
    }
}
