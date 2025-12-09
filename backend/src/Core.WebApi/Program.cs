using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.DependencyInjection;

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

// Инфраструктура (AuthService, BCrypt и т.п.)
builder.Services.AddInfrastructure();

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

builder.Services.AddAuthorization();

var app = builder.Build();

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

// Маршрутизация контроллеров
app.MapControllers();

app.Run();
