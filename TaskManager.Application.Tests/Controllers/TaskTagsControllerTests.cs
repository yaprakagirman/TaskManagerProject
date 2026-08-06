using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Validators.Tags;

namespace TaskManager.Application.Tests.Controllers;

public class TaskTagsControllerTests
{
    [Fact]
    public async Task Update_InvalidTagId_ReturnsBadRequestWithoutCallingService()
    {
        var taskTagService = new Mock<ITaskTagService>();
        var controller = CreateController(taskTagService);
        var request = new UpdateTaskTagRequest { TagId = 0 };

        var result = await controller.Update(12, request);

        Assert.IsType<BadRequestObjectResult>(result);
        taskTagService.Verify(
            service => service.UpdateAsync(
                It.IsAny<int>(),
                It.IsAny<UpdateTaskTagRequest>()),
            Times.Never);
    }

    [Fact]
    public async Task Update_ValidRequest_PassesParametersToServiceAndReturnsOk()
    {
        var taskTagService = new Mock<ITaskTagService>();
        var request = new UpdateTaskTagRequest { TagId = 6 };
        var response = new TaskTagDetailResponse
        {
            Id = 12,
            TaskItemId = 4,
            TaskTitle = "Task",
            TagId = request.TagId,
            TagName = "Backend"
        };
        taskTagService
            .Setup(service => service.UpdateAsync(12, request))
            .ReturnsAsync(response);
        var controller = CreateController(taskTagService);

        var result = await controller.Update(12, request);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        taskTagService.Verify(
            service => service.UpdateAsync(12, request),
            Times.Once);
    }

    private static TaskTagsController CreateController(
        Mock<ITaskTagService> taskTagService)
    {
        return new TaskTagsController(
            taskTagService.Object,
            new TagTasksRequestValidator(),
            new UpdateTaskTagRequestValidator());
    }
}
