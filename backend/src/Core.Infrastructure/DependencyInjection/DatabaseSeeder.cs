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
    public static async Task SeedAdminUserAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        try
        {
            using var scope = services.CreateScope();
            var serviceProvider = scope.ServiceProvider;

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseSeeder");

            // Ensure that database connection is configured before touching DbContext
            var connectionStringProvider = serviceProvider.GetService<IConnectionStringProvider>();
            if (connectionStringProvider is null)
            {
                logger?.LogWarning("IConnectionStringProvider is not registered. Skipping admin user seeding.");
                return;
            }

            var connection = connectionStringProvider.GetDefaultConnection();
            if (!connection.IsConfigured || string.IsNullOrWhiteSpace(connection.ConnectionString))
            {
                logger?.LogInformation("Database connection is not configured. Skipping admin user seeding.");
                return;
            }

            var db = serviceProvider.GetService<AppDbContext>();
            if (db is null)
            {
                logger?.LogWarning("AppDbContext is not registered. Skipping admin user seeding.");
                return;
            }

            // Check if an Admin user already exists (strict 'Admin' login)
            var existingAdmin = await db.Users
                .FirstOrDefaultAsync(u => u.Login == "Admin", cancellationToken);

            if (existingAdmin is not null)
            {
                logger?.LogInformation("Admin user already exists. Skipping seeding.");
                return;
            }

            var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher>();

            var now = DateTimeOffset.UtcNow;

            var adminRole = await db.Roles
                .FirstOrDefaultAsync(r => r.Code == "ADMIN", cancellationToken);

            if (adminRole is null)
            {
                adminRole = new Role
                {
                    Id = Guid.NewGuid(),
                    Code = "ADMIN",
                    Name = "Administrator",
                    CreatedAt = now
                };

                await db.Roles.AddAsync(adminRole, cancellationToken);
            }

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                Login = "Admin",
                FullName = "Administrator",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                PasswordHash = passwordHasher.HashPassword("admin")
            };

            await db.Users.AddAsync(adminUser, cancellationToken);

            var adminUserRole = new UserRole
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = now,
                CreatedAt = now
            };

            await db.UserRoles.AddAsync(adminUserRole, cancellationToken);

            await db.SaveChangesAsync(cancellationToken);

            logger?.LogInformation("Admin user has been seeded successfully.");
        }
        catch (Exception ex)
        {
            // Best-effort seeding: log and swallow exceptions so that startup is not broken
            var loggerFactory = services.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseSeeder");
            logger?.LogError(ex, "Error while seeding admin user.");
        }
    }
}