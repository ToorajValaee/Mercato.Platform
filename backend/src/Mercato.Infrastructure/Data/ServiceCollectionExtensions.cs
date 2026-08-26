using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Infrastructure.Data;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMercatoDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<MercatoDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }
}
