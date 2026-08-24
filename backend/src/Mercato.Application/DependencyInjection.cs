using Microsoft.Extensions.DependencyInjection;

namespace Mercato.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Services.IProductService, Services.ProductServiceImplementation>();
        services.AddScoped<Interfaces.IAuthService, Services.AuthService>();
        services.AddScoped<Services.PasswordService>();

        return services;
    }
}
