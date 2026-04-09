using System.Net;
using System.Text.Json;

namespace GuildManagerApi.Api.Middleware;

public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger = logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var (status, title) = ex switch
        {
            HttpRequestException http => ((int)(http.StatusCode ?? HttpStatusCode.BadGateway),
                "WarcraftLogs API error"),
            KeyNotFoundException => (StatusCodes.Status404NotFound,
                "Resource not found"),
            InvalidOperationException => (StatusCodes.Status422UnprocessableEntity,
                "Processing error"),
            OperationCanceledException => (StatusCodes.Status408RequestTimeout,
                "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new
        {
            type = $"https://httpstatuses.com/{status}",
            title,
            status,
            detail = ex.Message,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
    }

}
