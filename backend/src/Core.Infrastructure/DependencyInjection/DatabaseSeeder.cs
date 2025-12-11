using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Auth;
using MyIS.Core.Domain.Users;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.DependencyInjection;

public static class DatabaseSeeder
{
    /// <summary>
    /// НО-ОР реализация сидирования администратора.
    ///
    /// Фактическое создание роли ADMIN и пользователя Admin выполняется в EF Core миграции
    /// SeedAdminUser. Этот метод оставлен только как безопасный хук на будущее и ничего
    /// не изменяет в базе данных.
    /// </summary>
    public static Task SeedAdminUserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        try
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseSeeder");

            logger?.LogInformation(
                "DatabaseSeeder.SeedAdminUserAsync: admin user seeding is handled by EF Core migration 'SeedAdminUser'. Runtime seeding is disabled.");
        }
        catch (Exception ex)
        {
            // Best-effort logging: не ломаем запуск приложения, даже если логгер недоступен.
            var loggerFactory = services.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseSeeder");
            logger?.LogError(ex, "Error while executing no-op DatabaseSeeder.SeedAdminUserAsync.");
        }

        return Task.CompletedTask;
    }
}