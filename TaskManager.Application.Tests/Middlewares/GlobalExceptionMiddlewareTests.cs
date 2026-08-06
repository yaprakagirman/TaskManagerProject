using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManager.API.Middlewares;
using TaskManager.Application.Common.Exceptions;

namespace TaskManager.Application.Tests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    [Theory]
    [InlineData("not-found", StatusCodes.Status404NotFound)]
    [InlineData("conflict", StatusCodes.Status409Conflict)]
    [InlineData("bad-request", StatusCodes.Status400BadRequest)]
    [InlineData("unauthorized", StatusCodes.Status401Unauthorized)]
    public async Task InvokeAsync_KnownAppException_ReturnsContractStatusAndMessage(
        string exceptionType,
        int expectedStatusCode)
    {
        var exception = CreateAppException(exceptionType);
        var context = CreateContext();
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.FromException(exception),
            Mock.Of<ILogger<GlobalExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        using var body = await ReadBodyAsync(context);
        Assert.Equal(expectedStatusCode, context.Response.StatusCode);
        Assert.StartsWith("application/json", context.Response.ContentType);
        Assert.Equal(expectedStatusCode, body.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal(exception.Message, body.RootElement.GetProperty("message").GetString());
        Assert.Equal("/api/test", body.RootElement.GetProperty("path").GetString());
        Assert.Equal("trace-123", body.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_UnexpectedException_ReturnsSafeInternalServerError()
    {
        var context = CreateContext();
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.FromException(new InvalidOperationException("secret database detail")),
            Mock.Of<ILogger<GlobalExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        using var body = await ReadBodyAsync(context);
        var responseText = body.RootElement.GetRawText();
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", body.RootElement.GetProperty("message").GetString());
        Assert.DoesNotContain("secret database detail", responseText);
        Assert.DoesNotContain(nameof(InvalidOperationException), responseText);
        Assert.DoesNotContain("stack", responseText, StringComparison.OrdinalIgnoreCase);
    }

    private static AppException CreateAppException(string exceptionType)
    {
        return exceptionType switch
        {
            "not-found" => new NotFoundException("Missing resource."),
            "conflict" => new ConflictException("Conflicting resource."),
            "bad-request" => new BadRequestException("Invalid request."),
            "unauthorized" => new UnauthorizedException("Authentication required."),
            _ => throw new ArgumentOutOfRangeException(nameof(exceptionType))
        };
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/test";
        context.TraceIdentifier = "trace-123";
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}
