using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi;

namespace MyIS.Core.WebApi.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<WebApiAssemblyMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbName = $"MyIS_TestDb_{Guid.NewGuid()}";

            // Заменяем AppDbContext на in-memory БД для интеграционных тестов
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
            });

            // Переопределяем аутентификацию на тестовую схему
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "Test",
                    options => { });

            // Строим провайдер и сидируем Requests-справочники
            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<AppDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<CustomWebApplicationFactory>>();

            db.Database.EnsureCreated();

            try
            {
                SeedDatabase(db);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred seeding the test database.");
                throw;
            }
        });
    }

    private static void SeedDatabase(AppDbContext db)
    {
        if (!db.RequestTypes.Any())
        {
            db.RequestTypes.AddRange(
                new RequestType(
                    RequestTypeIds.CustomerDevelopment,
                    "Заявка заказчика",
                    RequestDirection.Incoming,
                    description: null),
                new RequestType(
                    RequestTypeIds.InternalProduction,
                    "Внутренняя производственная заявка",
                    RequestDirection.Incoming,
                    description: null),
                new RequestType(
                    RequestTypeIds.ChangeRequest,
                    "Заявка на изменение (ECR/ECO-light)",
                    RequestDirection.Incoming,
                    description: null),
                new RequestType(
                    RequestTypeIds.SupplyRequest,
                    "Заявка на обеспечение/закупку",
                    RequestDirection.Outgoing,
                    description: null),
                new RequestType(
                    RequestTypeIds.ExternalTechStage,
                    "Заявка на внешний технологический этап",
                    RequestDirection.Outgoing,
                    description: null));
        }

        if (!db.RequestStatuses.Any())
        {
            var draft = new RequestStatus(
                RequestStatusId.New(),
                RequestStatusCode.Draft,
                "Draft",
                isFinal: false,
                description: "Draft status");
            db.RequestStatuses.Add(draft);
        }

        db.SaveChanges();
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        System.Text.Encodings.Web.UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Позволяем тестам явно выключать аутентификацию для проверки 401.
        if (Request.Headers.TryGetValue("X-Test-Auth", out var authFlag)
            && string.Equals(authFlag.ToString(), "false", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.Fail("Test auth disabled"));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, TestAuthHandler.TestUserId.ToString()),
            new Claim(ClaimTypes.Name, "Test User")
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);

        // Позволяем тестам подмешивать роли через заголовок.
        // Формат: X-Test-Roles: ADMIN,USER
        if (Request.Headers.TryGetValue("X-Test-Roles", out var rolesHeader)
            && !string.IsNullOrWhiteSpace(rolesHeader))
        {
            var roles = rolesHeader
                .ToString()
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }
        }
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

