using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MercatoDbContext>(options =>
            options.UseNpgsql(connectionString));

        return services;
    }
}
