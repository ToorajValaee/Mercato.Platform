using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Services.IProductService, Services.ProductServiceImplementation>();

        return services;
    }
}
