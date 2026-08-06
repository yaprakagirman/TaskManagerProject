namespace TaskManager.API.Middlewares;

public class ClientContextMiddleware
{
    private const string ClientIdHeaderName = "X-Client-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<ClientContextMiddleware> _logger;

    public ClientContextMiddleware(
        RequestDelegate next,
        ILogger<ClientContextMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var hasClientId = context.Request.Headers.TryGetValue(
            ClientIdHeaderName,
            out var clientIdHeaderValue);

        if (!hasClientId && !ShouldSkipValidation(context))
        {
            context.Response.StatusCode =
                StatusCodes.Status400BadRequest;

            await context.Response.WriteAsJsonAsync(new
            {
                message = $"{ClientIdHeaderName} header is required."
            });

            return;
        }

        if (!hasClientId)
        {
            await _next(context);
            return;
        }

        var clientId = clientIdHeaderValue.ToString();

        context.Items["ClientId"] = clientId;

        _logger.LogInformation(
            "Client request entered the middleware chain. ClientId: {ClientId}",
            clientId);

        await _next(context);

        _logger.LogInformation(
            "Client request completed. ClientId: {ClientId}, StatusCode: {StatusCode}",
            clientId,
            context.Response.StatusCode);
    }

    private static bool ShouldSkipValidation(HttpContext context)
    {
        var path = context.Request.Path;

        return path.StartsWithSegments("/api/auth")
            || path.StartsWithSegments("/swagger");
    }
}
