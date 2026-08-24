using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Infrastructure.Data;

public static class DatabaseStartupExtensions
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MercatoDbContext>();

        await DatabaseInitializer.InitializeAsync(context);
        await SeedData.InitializeAsync(context);
    }
}
