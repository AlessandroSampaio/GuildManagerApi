using GuildManagerApi.Domain.Interfaces;

namespace GuildManagerApi.Api.Middleware;

/// <summary>
/// Bloqueia requisições mutáveis enquanto um restore de banco está em andamento,
/// exceto as próprias rotas de backup/restore (para o admin continuar monitorando/gerenciando).
/// Puramente baseado em verbo/path — não depende de HttpContext.User — por isso roda
/// antes de UseAuthentication, evitando gastar validação de JWT em requisições que serão rejeitadas.
/// </summary>
public class MaintenanceModeMiddleware(RequestDelegate next, IMaintenanceModeService maintenance)
{
    private static readonly HashSet<string> MutatingMethods =
        new(StringComparer.OrdinalIgnoreCase) { "POST", "PUT", "PATCH", "DELETE" };

    private static readonly string[] ExemptPrefixes = ["/api/admin/backups", "/api/admin/restores"];

    private readonly RequestDelegate _next = next;
    private readonly IMaintenanceModeService _maintenance = maintenance;

    public async Task InvokeAsync(HttpContext context)
    {
        if (_maintenance.IsActive
            && MutatingMethods.Contains(context.Request.Method)
            && !ExemptPrefixes.Any(p => context.Request.Path.StartsWithSegments(p)))
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://httpstatuses.com/503",
                title = "Maintenance mode",
                status = 503,
                detail = "A database restore is currently in progress. Mutating requests are temporarily disabled."
            });
            return;
        }

        await _next(context);
    }
}
