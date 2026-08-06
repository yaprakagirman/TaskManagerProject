using TaskManager.API.Models;

using TaskManager.Application.Common.Exceptions;

namespace TaskManager.API.Middlewares;

public class GlobalExceptionMiddleware
{
    private const string UnexpectedErrorMessage = "An unexpected error occurred.";

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);

        LogException(exception, statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;

        var response = CreateErrorResponse(context, exception, statusCode);

        await context.Response.WriteAsJsonAsync(response);
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception is AppException appException
            ? (int)appException.StatusCode
            : StatusCodes.Status500InternalServerError;
    }

    private void LogException(Exception exception, int statusCode)
    {
        if (statusCode != StatusCodes.Status500InternalServerError)
        {
            return;
        }

        _logger.LogError(exception, UnexpectedErrorMessage);
    }

    private static ErrorResponse CreateErrorResponse(
        HttpContext context,
        Exception exception,
        int statusCode)
    {
        return new ErrorResponse
        {
            StatusCode = statusCode,
            Message = GetClientMessage(exception, statusCode),
            Path = context.Request.Path.ToString(),
            TraceId = context.TraceIdentifier
        };
    }

    private static string GetClientMessage(Exception exception, int statusCode)
    {
        return statusCode == StatusCodes.Status500InternalServerError
            ? UnexpectedErrorMessage
            : exception.Message;
    }
}
