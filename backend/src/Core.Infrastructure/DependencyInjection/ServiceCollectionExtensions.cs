using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Auth;
using MyIS.Core.Infrastructure.Auth;

namespace MyIS.Core.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}