using GuildManagerApi.Application.Auth;

namespace GuildManagerApi.Api.Middleware;

public class ClientIdInjectionMiddleware(RequestDelegate next, IJwtService jwtService)
{
    private readonly RequestDelegate _next = next;
    private readonly IJwtService _jwtService = jwtService;

    public async Task InvokeAsync(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (authHeader?.StartsWith("Bearer ") ?? false)
        {
            var token = authHeader["Bearer ".Length..].Trim();
            var userId = _jwtService.GetUserIdFromToken(token);
            if (userId is not null)
                context.Request.Headers["X-ClientId"] = userId.ToString();
        }

        await _next(context);
    }
}
