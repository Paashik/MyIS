using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Infrastructure.Auth;
using MyIS.Core.Infrastructure.Requests.Access;
using MyIS.Core.Infrastructure.Requests.Repositories;
using MyIS.Core.Infrastructure.Security.Repositories;
 
namespace MyIS.Core.Infrastructure.DependencyInjection;
 
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Auth
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IAuthService, AuthService>();
 
        // Requests module
        services.AddScoped<IRequestRepository, RequestRepository>();
        services.AddScoped<IRequestTypeRepository, RequestTypeRepository>();
        services.AddScoped<IRequestStatusRepository, RequestStatusRepository>();
        services.AddScoped<IRequestTransitionRepository, RequestTransitionRepository>();
        services.AddScoped<IRequestHistoryRepository, RequestHistoryRepository>();
        services.AddScoped<IRequestCommentRepository, RequestCommentRepository>();
        services.AddScoped<IRequestAttachmentRepository, RequestAttachmentRepository>();
        services.AddScoped<IRequestsAccessChecker, RequestsAccessChecker>();

        // Security (Settings -> Security)
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
 
        return services;
    }
}
