using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.Application.Requests.Handlers;

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