using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Auth;
using MyIS.Core.Application.Integration.Component2020.Abstractions;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Mdm.Abstractions;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Infrastructure.Auth;
using MyIS.Core.Infrastructure.Integration.Component2020.Repositories;
using MyIS.Core.Infrastructure.Integration.Component2020.Services;
using MyIS.Core.Infrastructure.Mdm.Repositories;
using MyIS.Core.Infrastructure.Mdm.Services;
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

        // MDM
        services.AddScoped<IManufacturerRepository, ManufacturerRepository>();
        services.AddScoped<IBodyTypeRepository, BodyTypeRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<ITechnicalParameterRepository, TechnicalParameterRepository>();
        services.AddScoped<IParameterSetRepository, ParameterSetRepository>();
        services.AddScoped<ISymbolRepository, SymbolRepository>();
        services.AddScoped<IMdmReferencesQueryService, MdmReferencesQueryService>();

        // Integration.Component2020
        services.AddScoped<IComponent2020ConnectionProvider, Component2020ConnectionProvider>();
        services.AddScoped<IComponent2020SnapshotReader, Component2020SnapshotReader>();
        services.AddScoped<IComponent2020DeltaReader, Component2020DeltaReader>();
        services.AddScoped<IComponent2020SyncService, Component2020SyncService>();
        services.AddScoped<IComponent2020SyncRunRepository, Component2020SyncRunRepository>();
        services.AddScoped<IComponent2020SyncCursorRepository, Component2020SyncCursorRepository>();
        services.AddScoped<IComponent2020SyncScheduleRepository, Component2020SyncScheduleRepository>();

        return services;
    }
}
