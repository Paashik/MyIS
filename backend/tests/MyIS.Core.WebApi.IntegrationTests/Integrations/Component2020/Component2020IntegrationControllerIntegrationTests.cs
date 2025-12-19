using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MyIS.Core.WebApi.IntegrationTests;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;
using Xunit;

namespace MyIS.Core.WebApi.IntegrationTests.Integrations.Component2020;

public sealed class Component2020IntegrationControllerIntegrationTests
    : IClassFixture<MyIS.Core.WebApi.IntegrationTests.CustomWebApplicationFactory>
{
    private readonly MyIS.Core.WebApi.IntegrationTests.CustomWebApplicationFactory _factory;

    public Component2020IntegrationControllerIntegrationTests(MyIS.Core.WebApi.IntegrationTests.CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private sealed class GetRunErrorsResponse
    {
        public ErrorDto[] Errors { get; set; } = Array.Empty<ErrorDto>();
    }

    private sealed class ErrorDto
    {
        public Guid Id { get; set; }
        public string EntityType { get; set; } = null!;
        public string? ExternalKey { get; set; }
        public string Message { get; set; } = null!;
    }

    private sealed class RunSyncResponse
    {
        public Guid RunId { get; set; }
        public string Status { get; set; } = null!;
        public int ProcessedCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    private sealed class FakeComponent2020SyncService : IComponent2020SyncService
    {
        public RunComponent2020SyncCommand? LastCommand { get; private set; }

        public Task<RunComponent2020SyncResponse> RunSyncAsync(RunComponent2020SyncCommand command, System.Threading.CancellationToken cancellationToken)
        {
            LastCommand = command;
            return Task.FromResult(new RunComponent2020SyncResponse
            {
                RunId = Guid.NewGuid(),
                Status = "Success",
                ProcessedCount = 123
            });
        }
    }

    [Fact]
    public async Task GetRunErrors_Returns200_AndErrors()
    {
        Guid runId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var run = new Component2020SyncRun("Counterparties", "Commit", TestAuthHandler.TestUserId);
            run.Complete("Failed", 0, 1, "{}", "Test summary");
            runId = run.Id;

            db.Component2020SyncRuns.Add(run);
            db.Component2020SyncErrors.Add(new Component2020SyncError(runId, "Supplier", null, "1", "Boom", null));

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient(new() { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/integrations/component2020/runs/{runId}/errors");
        request.Headers.Add("X-Test-Roles", "ADMIN");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<GetRunErrorsResponse>();
        body.Should().NotBeNull();
        body!.Errors.Should().HaveCount(1);
        body.Errors[0].EntityType.Should().Be("Supplier");
        body.Errors[0].Message.Should().Be("Boom");
        body.Errors[0].ExternalKey.Should().Be("1");
    }

    [Fact]
    public async Task RunSync_UsesAuthenticatedUserId_AndReturnsOk()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(s => s.ServiceType == typeof(IComponent2020SyncService));
                if (existing != null)
                {
                    services.Remove(existing);
                }

                services.AddSingleton<FakeComponent2020SyncService>();
                services.AddSingleton<IComponent2020SyncService>(sp => sp.GetRequiredService<FakeComponent2020SyncService>());
            });
        });

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/integrations/component2020/run")
        {
            Content = JsonContent.Create(new
            {
                connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                scope = "Counterparties",
                dryRun = true
            })
        };
        request.Headers.Add("X-Test-Roles", "ADMIN");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<RunSyncResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("Success");
        body.ProcessedCount.Should().Be(123);

        var fake = factory.Services.GetRequiredService<FakeComponent2020SyncService>();
        fake.LastCommand.Should().NotBeNull();
        fake.LastCommand!.StartedByUserId.Should().Be(TestAuthHandler.TestUserId);
        fake.LastCommand.Scope.ToString().Should().Be("Counterparties");
    }

    [Fact]
    public async Task RunSync_Returns400_OnInvalidScope()
    {
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var existing = services.SingleOrDefault(s => s.ServiceType == typeof(IComponent2020SyncService));
                if (existing != null)
                {
                    services.Remove(existing);
                }

                services.AddSingleton<FakeComponent2020SyncService>();
                services.AddSingleton<IComponent2020SyncService>(sp => sp.GetRequiredService<FakeComponent2020SyncService>());
            });
        });

        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/integrations/component2020/run")
        {
            Content = JsonContent.Create(new
            {
                connectionId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                scope = "NOT_A_SCOPE",
                dryRun = true
            })
        };
        request.Headers.Add("X-Test-Roles", "ADMIN");

        var response = await client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var fake = factory.Services.GetRequiredService<FakeComponent2020SyncService>();
        fake.LastCommand.Should().BeNull();
    }
}
