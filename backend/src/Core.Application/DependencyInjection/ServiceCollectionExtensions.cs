using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Handlers.Workflow;

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

        return services;
    }
}
