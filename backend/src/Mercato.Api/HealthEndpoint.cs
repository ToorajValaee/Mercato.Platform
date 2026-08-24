namespace Mercato.Api;

public static class HealthEndpoint
{
    public static void MapHealth(this WebApplication app)
    {
        app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Mercato.Api" }));
    }
}
