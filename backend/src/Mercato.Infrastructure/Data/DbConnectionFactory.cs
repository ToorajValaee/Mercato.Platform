using Microsoft.EntityFrameworkCore;

namespace Mercato.Infrastructure.Data;

public static class DbConnectionFactory
{
    public static void ConfigureDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MercatoDbContext>(options =>
            options.UseNpgsql(connectionString));
    }
}
