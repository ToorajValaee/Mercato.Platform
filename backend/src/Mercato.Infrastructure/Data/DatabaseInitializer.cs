using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(MercatoDbContext context, CancellationToken cancellationToken = default)
    {
        var migrations = context.Database.GetMigrations();
        if (migrations.Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
            return;
        }

        await context.Database.EnsureCreatedAsync(cancellationToken);
    }
}
