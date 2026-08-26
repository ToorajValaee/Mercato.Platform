using Mercato.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task InitializeDatabaseAsync(this WebApplication app, CancellationToken cancellationToken = default)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MercatoDbContext>();
        await DatabaseInitializer.InitializeAsync(context, cancellationToken);
    }
}
