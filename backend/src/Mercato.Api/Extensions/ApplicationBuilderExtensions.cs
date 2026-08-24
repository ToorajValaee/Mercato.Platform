namespace Mercato.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseMercato(this WebApplication app)
    {
        app.MapGet("/api/status", () => new { service = "Mercato", status = "running" });
        return app;
    }
}
