using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Validators.Tags;
using TaskManager.Application.Validators.Tasks;
using TaskManager.Application.Validators.Users;
using TaskManager.Domain.Enums;

namespace TaskManager.Application.Tests.Validators;

public class ValidatorBoundaryTests
{
    [Fact]
    public void CreateTaskRequestValidator_ValidRequest_HasNoErrors()
    {
        var request = new CreateTaskRequest
        {
            Title = "Valid task",
            Description = "Description",
            Priority = TaskPriority.Medium,
            DueDate = DateTime.UtcNow.Date.AddDays(1),
            ProjectId = 2,
            ParentTaskId = 3
        };

        var result = new CreateTaskRequestValidator().Validate(request);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0, null, "ProjectId")]
    [InlineData(-1, null, "ProjectId")]
    [InlineData(null, 0, "ParentTaskId")]
    [InlineData(null, -1, "ParentTaskId")]
    public void CreateTaskRequestValidator_NonPositiveOptionalId_HasExpectedError(
        int? projectId,
        int? parentTaskId,
        string propertyName)
    {
        var request = new CreateTaskRequest
        {
            Title = "Task",
            ProjectId = projectId,
            ParentTaskId = parentTaskId
        };

        var result = new CreateTaskRequestValidator().Validate(request);

        Assert.Contains(result.Errors, error => error.PropertyName == propertyName);
    }

    [Fact]
    public void CreateTaskRequestValidator_EmptyTitle_HasTitleError()
    {
        var result = new CreateTaskRequestValidator().Validate(
            new CreateTaskRequest { Title = string.Empty });

        Assert.Contains(result.Errors, error => error.PropertyName == "Title");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignTaskRequestValidator_NonPositiveUserId_HasAssignedUserIdError(int userId)
    {
        var result = new AssignTaskRequestValidator().Validate(
            new AssignTaskRequest { AssignedUserId = userId });

        Assert.Contains(result.Errors, error => error.PropertyName == "AssignedUserId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void UpdateTaskTagRequestValidator_NonPositiveTagId_HasTagIdError(int tagId)
    {
        var result = new UpdateTaskTagRequestValidator().Validate(
            new UpdateTaskTagRequest { TagId = tagId });

        Assert.Contains(result.Errors, error => error.PropertyName == "TagId");
    }

    [Fact]
    public void TagTasksRequestValidator_EmptyTaskIds_HasTaskIdsError()
    {
        var result = new TagTasksRequestValidator().Validate(
            new TagTasksRequest { TaskIds = [] });

        Assert.Contains(result.Errors, error => error.PropertyName == "TaskIds");
    }

    [Fact]
    public void TagTasksRequestValidator_DuplicateTaskIds_HasTaskIdsError()
    {
        var result = new TagTasksRequestValidator().Validate(
            new TagTasksRequest { TaskIds = [2, 2] });

        Assert.Contains(result.Errors, error => error.PropertyName == "TaskIds");
    }

    [Fact]
    public void TagTasksRequestValidator_NonPositiveTaskId_HasElementError()
    {
        var result = new TagTasksRequestValidator().Validate(
            new TagTasksRequest { TaskIds = [0] });

        Assert.Contains(result.Errors, error => error.PropertyName == "TaskIds[0]");
    }

    [Fact]
    public void UpdateUserRoleRequestValidator_InvalidRole_HasRoleError()
    {
        var result = new UpdateUserRoleRequestValidator().Validate(
            new UpdateUserRoleRequest { Role = (UserRole)999 });

        Assert.Contains(result.Errors, error => error.PropertyName == "Role");
    }

    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public void TagRequestValidator_InvalidName_HasNameError(string name)
    {
        var result = new TagRequestValidator().Validate(new TagRequest { Name = name });

        Assert.Contains(result.Errors, error => error.PropertyName == "Name");
    }
}
