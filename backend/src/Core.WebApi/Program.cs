using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.DependencyInjection;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.DependencyInjection;
using MyIS.Core.Infrastructure.Integration.Component2020.BackgroundServices;
using MyIS.Core.WebApi.Middleware;
 
var builder = WebApplication.CreateBuilder(args);

// appsettings.Local.json с приоритетом и hot-reload
builder.Configuration.AddJsonFile(
    "appsettings.Local.json",
    optional: true,
    reloadOnChange: true);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Подсистема работы со строкой подключения и здоровьем БД
builder.Services.AddSingleton<IConnectionStringProvider, DefaultConnectionStringProvider>();
builder.Services.AddScoped<IDbHealthService, DbHealthService>();

// DbContext: не падает при отсутствии строки подключения
builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var provider = sp.GetRequiredService<IConnectionStringProvider>();
    var result = provider.GetDefaultConnection();

    if (!result.IsConfigured || string.IsNullOrWhiteSpace(result.ConnectionString))
    {
        // Строка подключения не настроена — контекст просто не конфигурируется
        return;
    }

    options.UseNpgsql(result.ConnectionString);
});
 
// Application layer (use cases / handlers)
builder.Services.AddApplication();
 
// Инфраструктура (AuthService, BCrypt и т.п.)
builder.Services.AddInfrastructure();

// Background services
builder.Services.AddHostedService<Component2020SchedulerHostedService>();

// Cookie-аутентификация
builder.Services
    .AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.Cookie.Name = ".MyIS.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.Path = "/";

        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;

        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Iteration S1: минимальная модель прав для Settings.
    // Пока маппим permissions -> роль ADMIN.
    options.AddPolicy("Admin.Settings.Access", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Requests.EditTypes", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Requests.EditStatuses", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Requests.EditWorkflow", policy => policy.RequireRole("ADMIN"));

    // Iteration S2: Settings -> Security
    options.AddPolicy("Admin.Security.View", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Security.EditEmployees", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Security.EditUsers", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Security.EditRoles", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Organization.Edit", policy => policy.RequireRole("ADMIN"));

    // Integration.Component2020
    options.AddPolicy("Admin.Integration.View", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Integration.Execute", policy => policy.RequireRole("ADMIN"));

    // MDM (read-only сейчас, CRUD позже)
    options.AddPolicy("Admin.Mdm.EditUnits", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditSuppliers", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditItems", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditManufacturers", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditBodyTypes", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditCurrencies", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditTechnicalParameters", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditParameterSets", policy => policy.RequireRole("ADMIN"));
    options.AddPolicy("Admin.Mdm.EditSymbols", policy => policy.RequireRole("ADMIN"));
});

var app = builder.Build();


// Логируем источник строки подключения на старте, без раскрытия секретов
var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
var connectionStringProvider = app.Services.GetRequiredService<IConnectionStringProvider>();
var connectionInfo = connectionStringProvider.GetDefaultConnection();
var description = connectionInfo.RawSourceDescription ?? "Connection string info is unavailable.";

if (connectionInfo.IsConfigured && !string.IsNullOrWhiteSpace(connectionInfo.ConnectionString))
{
    startupLogger.LogInformation(
        "Connection string source: {Source}. Details: {Description}",
        connectionInfo.Source,
        description);
}
else
{
    startupLogger.LogWarning(
        "Connection string is not configured. Source: {Source}. Details: {Description}",
        connectionInfo.Source,
        description);
}


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ВАЖНО: аутентификация и авторизация
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<UnauthorizedAccessExceptionMiddleware>();

// Маршрутизация контроллеров
app.MapControllers();

app.Run();
