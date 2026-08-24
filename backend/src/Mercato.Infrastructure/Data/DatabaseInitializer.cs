using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(MercatoDbContext context)
    {
        await context.Database.MigrateAsync();
    }
}
