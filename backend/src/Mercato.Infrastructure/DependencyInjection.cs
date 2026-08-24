using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mercato.Infrastructure.Data;
using Mercato.Application.Repositories;
using Mercato.Infrastructure.Repositories;

namespace Mercato.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Mercato")
            ?? configuration["MERCATO_CONNECTION_STRING"]
            ?? throw new InvalidOperationException("Mercato database connection string is missing.");

        services.AddDbContext<MercatoDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}
