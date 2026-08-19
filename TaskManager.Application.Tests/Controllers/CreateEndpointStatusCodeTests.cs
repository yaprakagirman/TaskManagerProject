using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TaskManager.API.Controllers;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.DTOs.Tags;
using TaskManager.Application.DTOs.Users;
using TaskManager.Application.Interfaces;
using TaskManager.Application.Validators.Auth;
using TaskManager.Application.Validators.Projects;
using TaskManager.Application.Validators.Tags;
using TaskManager.Application.Validators.Users;

namespace TaskManager.Application.Tests.Controllers;

public class CreateEndpointStatusCodeTests
{
    [Fact]
    public async Task Register_Success_ReturnsOkWithResponseBody()
    {
        var request = new RegisterRequest
        {
            FirstName = "Ada",
            LastName = "Lovelace",
            Email = "ada@example.test",
            Password = "secret123"
        };
        var response = new AuthResponse { UserId = 1, Email = request.Email };
        var service = new Mock<IAuthService>();
        service.Setup(current => current.RegisterAsync(request)).ReturnsAsync(response);
        var controller = new AuthController(
            service.Object,
            new RegisterRequestValidator(),
            new LoginRequestValidator());

        var result = await controller.Register(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task UserCreate_Success_ReturnsOkWithResponseBody()
    {
        var request = new CreateUserRequest
        {
            FirstName = "Grace",
            LastName = "Hopper",
            Email = "grace@example.test"
        };
        var response = new UserResponse { Id = 2, Email = request.Email };
        var service = new Mock<IUserService>();
        service.Setup(current => current.CreateUserAsync(request)).ReturnsAsync(response);
        var controller = new UsersController(
            service.Object,
            Mock.Of<ICurrentUserService>(),
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            new UpdateUserRoleRequestValidator(),
            new UpdateUserExpertisesRequestValidator());

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task GetMyProfile_AuthenticatedUser_ReturnsOkWithUserResponse()
    {
        var userService = new Mock<IUserService>();
        var currentUserService = new Mock<ICurrentUserService>();
        currentUserService.Setup(c => c.UserId).Returns(42);
        var expectedResponse = new UserResponse { Id = 42, Email = "me@example.test" };
        userService.Setup(s => s.GetUserByIdAsync(42)).ReturnsAsync(expectedResponse);

        var controller = new UsersController(
            userService.Object,
            currentUserService.Object,
            new CreateUserRequestValidator(),
            new UpdateUserRequestValidator(),
            new UpdateUserRoleRequestValidator(),
            new UpdateUserExpertisesRequestValidator());

        var result = await controller.GetMyProfile();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedResponse, ok.Value);
    }

    [Fact]
    public async Task ProjectCreate_Success_ReturnsOkWithResponseBody()
    {
        var request = new CreateProjectRequest { Name = "Internship" };
        var response = new ProjectResponse { Id = 3, Name = request.Name };
        var service = new Mock<IProjectService>();
        service.Setup(current => current.CreateAsync(request)).ReturnsAsync(response);
        var controller = new ProjectsController(
            service.Object,
            new CreateProjectRequestValidator(),
            new UpdateProjectRequestValidator());

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task TagCreate_Success_ReturnsOkWithResponseBody()
    {
        var request = new TagRequest { Name = "backend" };
        var response = new TagResponse { Id = 4, Name = request.Name };
        var service = new Mock<ITagService>();
        service.Setup(current => current.CreateAsync(request)).ReturnsAsync(response);
        var controller = new TagsController(service.Object, new TagRequestValidator());

        var result = await controller.Create(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(response, ok.Value);
    }

    [Fact]
    public async Task TagDelete_UsesTagIdAndReturnsNoContent()
    {
        var service = new Mock<ITagService>();
        var controller = new TagsController(service.Object, new TagRequestValidator());

        var result = await controller.Delete(10);

        Assert.IsType<NoContentResult>(result);
        service.Verify(current => current.DeleteAsync(10), Times.Once);
    }

    [Theory]
    [InlineData(typeof(AuthController), nameof(AuthController.Register))]
    [InlineData(typeof(UsersController), nameof(UsersController.Create))]
    [InlineData(typeof(ProjectsController), nameof(ProjectsController.Create))]
    [InlineData(typeof(TasksController), nameof(TasksController.Create))]
    [InlineData(typeof(TagsController), nameof(TagsController.Create))]
    public void StandardizedCreateAction_Documents200AndNot201(
        Type controllerType,
        string actionName)
    {
        var action = controllerType.GetMethod(actionName);

        Assert.NotNull(action);
        var responseTypes = action.GetCustomAttributes<ProducesResponseTypeAttribute>();
        Assert.Contains(responseTypes, attribute =>
            attribute.StatusCode == StatusCodes.Status200OK);
        Assert.DoesNotContain(responseTypes, attribute =>
            attribute.StatusCode == StatusCodes.Status201Created);
    }
}
