using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercato.Infrastructure.Data;

namespace Mercato.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddMercatoInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<MercatoDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        return services;
    }
}
