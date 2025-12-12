using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace MyIS.Core.WebApi.IntegrationTests.Admin.Security;

public sealed class AdminSecurityIntegrationTests : IClassFixture<MyIS.Core.WebApi.IntegrationTests.CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminSecurityIntegrationTests(MyIS.Core.WebApi.IntegrationTests.CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
    }

    private sealed class EmployeeDto
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = null!;
        public bool IsActive { get; set; }
    }

    private sealed class RoleDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    private sealed class UserDto
    {
        public Guid Id { get; set; }
        public string Login { get; set; } = null!;
        public bool IsActive { get; set; }
        public Guid? EmployeeId { get; set; }
    }

    private sealed class UserRolesDto
    {
        public Guid UserId { get; set; }
        public Guid[] RoleIds { get; set; } = Array.Empty<Guid>();
    }

    [Fact]
    public async Task AdminSecurityEndpoints_Return401_WhenAuthMissing()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/security/employees");
        request.Headers.Add("X-Test-Auth", "false");

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminSecurityEndpoints_Return403_WhenNotAdmin()
    {
        var response = await _client.GetAsync("/api/admin/security/employees");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminSecurity_HappyPath_Works_ForAdmin()
    {
        // Create role
        var createRole = new HttpRequestMessage(HttpMethod.Post, "/api/admin/security/roles")
        {
            Content = JsonContent.Create(new { code = "TEST_ROLE", name = "Test role" })
        };
        createRole.Headers.Add("X-Test-Roles", "ADMIN");

        var roleResponse = await _client.SendAsync(createRole);
        roleResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var role = await roleResponse.Content.ReadFromJsonAsync<RoleDto>();
        role.Should().NotBeNull();
        role!.Code.Should().Be("TEST_ROLE");

        // Create employee
        var createEmployee = new HttpRequestMessage(HttpMethod.Post, "/api/admin/security/employees")
        {
            Content = JsonContent.Create(new { fullName = "Иванов Иван", email = "ivanov@example.com" })
        };
        createEmployee.Headers.Add("X-Test-Roles", "ADMIN");

        var employeeResponse = await _client.SendAsync(createEmployee);
        employeeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var employee = await employeeResponse.Content.ReadFromJsonAsync<EmployeeDto>();
        employee.Should().NotBeNull();
        employee!.FullName.Should().Be("Иванов Иван");

        // Create user linked to employee
        var createUser = new HttpRequestMessage(HttpMethod.Post, "/api/admin/security/users")
        {
            Content = JsonContent.Create(new
            {
                login = "test.user",
                password = "P@ssw0rd!",
                isActive = true,
                employeeId = employee.Id
            })
        };
        createUser.Headers.Add("X-Test-Roles", "ADMIN");

        var userResponse = await _client.SendAsync(createUser);
        userResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var user = await userResponse.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.EmployeeId.Should().Be(employee.Id);

        // Unique employeeId: second user with same employeeId should fail
        var createUser2 = new HttpRequestMessage(HttpMethod.Post, "/api/admin/security/users")
        {
            Content = JsonContent.Create(new
            {
                login = "test.user2",
                password = "P@ssw0rd!",
                isActive = true,
                employeeId = employee.Id
            })
        };
        createUser2.Headers.Add("X-Test-Roles", "ADMIN");

        var user2Response = await _client.SendAsync(createUser2);
        user2Response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Assign roles to user (replace)
        var replaceRoles = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/security/users/{user.Id}/roles")
        {
            Content = JsonContent.Create(new { roleIds = new[] { role.Id } })
        };
        replaceRoles.Headers.Add("X-Test-Roles", "ADMIN");

        var replaceRolesResponse = await _client.SendAsync(replaceRoles);
        replaceRolesResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getRoles = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/security/users/{user.Id}/roles");
        getRoles.Headers.Add("X-Test-Roles", "ADMIN");

        var getRolesResponse = await _client.SendAsync(getRoles);
        getRolesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var userRoles = await getRolesResponse.Content.ReadFromJsonAsync<UserRolesDto>();
        userRoles.Should().NotBeNull();
        userRoles!.RoleIds.Should().Contain(role.Id);

        // Reset password
        var resetPassword = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/security/users/{user.Id}/reset-password")
        {
            Content = JsonContent.Create(new { newPassword = "N3wP@ss" })
        };
        resetPassword.Headers.Add("X-Test-Roles", "ADMIN");

        var resetResponse = await _client.SendAsync(resetPassword);
        resetResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}

