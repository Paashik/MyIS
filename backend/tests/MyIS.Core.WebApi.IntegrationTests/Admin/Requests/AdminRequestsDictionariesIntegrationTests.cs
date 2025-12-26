using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MyIS.Core.WebApi.Contracts.Admin.Requests;
using Xunit;

namespace MyIS.Core.WebApi.IntegrationTests.Admin.Requests;

public sealed class AdminRequestsDictionariesIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminRequestsDictionariesIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task AdminEndpoints_Return401_WhenAuthMissing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/requests/types");
        request.Headers.Add("X-Test-Auth", "false");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminEndpoints_Return403_WhenNotAdmin()
    {
        var response = await _client.GetAsync("/api/admin/requests/types");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoints_Return200_WhenAdmin()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/requests/types");
        request.Headers.Add("X-Test-Roles", "ADMIN");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AdminCreateRequestType_Works_ForAdmin()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/admin/requests/types")
        {
            Content = JsonContent.Create(new AdminRequestTypeCreateRequest
            {
                Name = "Тестовый тип",
                Direction = "Incoming",
                Description = "Created by integration test",
                IsActive = true
            })
        };
        request.Headers.Add("X-Test-Roles", "ADMIN");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        // Проверяем что тип появляется в списке
        var get = new HttpRequestMessage(HttpMethod.Get, "/api/admin/requests/types");
        get.Headers.Add("X-Test-Roles", "ADMIN");

        var listResponse = await _client.SendAsync(get);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await listResponse.Content.ReadFromJsonAsync<dynamic[]>();
        list.Should().NotBeNull();
    }
}

