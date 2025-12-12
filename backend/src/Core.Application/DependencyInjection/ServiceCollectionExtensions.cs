using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Handlers.Admin;
using MyIS.Core.Application.Requests.Handlers.Workflow;
using MyIS.Core.Application.Security.Handlers.Admin;

namespace MyIS.Core.Application.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрация Application-слоя (Use Case / Handlers).
    /// На текущей итерации подключаем только модуль Requests.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Requests — команды
        services.AddScoped<CreateRequestHandler>();
        services.AddScoped<UpdateRequestHandler>();
        services.AddScoped<AddRequestCommentHandler>();

        // Requests — workflow actions
        services.AddScoped<GetRequestActionsHandler>();
        services.AddScoped<SubmitRequestHandler>();
        services.AddScoped<StartReviewRequestHandler>();
        services.AddScoped<ApproveRequestHandler>();
        services.AddScoped<RejectRequestHandler>();
        services.AddScoped<StartWorkOnRequestHandler>();
        services.AddScoped<CompleteRequestHandler>();
        services.AddScoped<CloseRequestHandler>();

        // Requests — запросы
        services.AddScoped<SearchRequestsHandler>();
        services.AddScoped<GetRequestByIdHandler>();
        services.AddScoped<GetRequestHistoryHandler>();
        services.AddScoped<GetRequestCommentsHandler>();
        services.AddScoped<GetRequestTypesHandler>();
        services.AddScoped<GetRequestStatusesHandler>();

        // Requests — admin/settings
        services.AddScoped<GetAdminRequestTypesHandler>();
        services.AddScoped<CreateAdminRequestTypeHandler>();
        services.AddScoped<UpdateAdminRequestTypeHandler>();
        services.AddScoped<ArchiveAdminRequestTypeHandler>();

        services.AddScoped<GetAdminRequestStatusesHandler>();
        services.AddScoped<CreateAdminRequestStatusHandler>();
        services.AddScoped<UpdateAdminRequestStatusHandler>();
        services.AddScoped<ArchiveAdminRequestStatusHandler>();

        services.AddScoped<GetAdminRequestWorkflowTransitionsHandler>();
        services.AddScoped<ReplaceAdminRequestWorkflowTransitionsHandler>();

        // Security — admin/settings
        services.AddScoped<GetAdminEmployeesHandler>();
        services.AddScoped<GetAdminEmployeeByIdHandler>();
        services.AddScoped<CreateAdminEmployeeHandler>();
        services.AddScoped<UpdateAdminEmployeeHandler>();
        services.AddScoped<ActivateAdminEmployeeHandler>();
        services.AddScoped<DeactivateAdminEmployeeHandler>();

        services.AddScoped<GetAdminUsersHandler>();
        services.AddScoped<GetAdminUserByIdHandler>();
        services.AddScoped<CreateAdminUserHandler>();
        services.AddScoped<UpdateAdminUserHandler>();
        services.AddScoped<ActivateAdminUserHandler>();
        services.AddScoped<DeactivateAdminUserHandler>();
        services.AddScoped<ResetAdminUserPasswordHandler>();

        services.AddScoped<GetAdminRolesHandler>();
        services.AddScoped<CreateAdminRoleHandler>();
        services.AddScoped<UpdateAdminRoleHandler>();

        services.AddScoped<GetAdminUserRolesHandler>();
        services.AddScoped<ReplaceAdminUserRolesHandler>();

        return services;
    }
}
