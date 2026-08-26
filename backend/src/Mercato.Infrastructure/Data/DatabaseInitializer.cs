using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(MercatoDbContext context)
    {
        var migrations = await context.Database.GetMigrationsAsync();
        if (migrations.Any())
        {
            await context.Database.MigrateAsync();
            return;
        }

        await context.Database.EnsureCreatedAsync();
    }
}
