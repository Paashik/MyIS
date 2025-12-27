using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FluentAssertions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.WebApi.Contracts.Requests;
using Xunit;

namespace MyIS.Core.WebApi.IntegrationTests.Requests;

public class RequestsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private static readonly Guid SupplyRequestTypeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CustomerDevelopmentTypeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid InternalProductionTypeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid ChangeRequestTypeId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid ExternalTechStageTypeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public RequestsControllerIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task CreateAndGetRequest_Works()
    {
        // Arrange: получаем тип заявки и убеждаемся, что справочники инициализированы
        var typesResponse = await _client.GetAsync("/api/request-types");
        typesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var types = await typesResponse.Content.ReadFromJsonAsync<List<RequestTypeDto>>();
        types.Should().NotBeNull();
        types!.Should().HaveCount(5);

        var requestType = types!.Single(t => t.Id == SupplyRequestTypeId);

        var createPayload = new CreateRequestRequest
        {
            RequestTypeId = requestType.Id,
            Title = "Integration test request",
            Description = "Created from integration test",
            DueDate = DateTimeOffset.UtcNow.AddDays(3),
            RelatedEntityType = "TestEntity",
            RelatedEntityId = Guid.NewGuid(),
            ExternalReferenceId = "INT-REQ-1"
        };

        // Act: создаём заявку
        var createResponse = await _client.PostAsJsonAsync("/api/requests", createPayload);

        // Assert: проверяем результат создания
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<RequestDto>();
        created.Should().NotBeNull();

        created!.Id.Should().NotBe(Guid.Empty);
        created.Title.Should().NotBeNullOrWhiteSpace();
        created.Description.Should().Be(createPayload.Description);
        created.RequestTypeId.Should().Be(requestType.Id);
        created.RequestTypeName.Should().Be(requestType.Name);

        // Стартовый статус должен быть Draft
        created.RequestStatusCode.Should().Be("Draft");
        created.RequestStatusName.Should().Be("Draft");
        created.RequestStatusId.Should().NotBe(Guid.Empty);

        // Инициатор должен совпадать с тестовым пользователем из TestAuthHandler
        created.InitiatorId.Should().Be(TestAuthHandler.TestUserId);

        created.RelatedEntityType.Should().Be(createPayload.RelatedEntityType);
        created.RelatedEntityId.Should().Be(createPayload.RelatedEntityId);
        created.ExternalReferenceId.Should().Be(createPayload.ExternalReferenceId);

        created.CreatedAt.Should().NotBe(default);
        created.UpdatedAt.Should().NotBe(default);

        // Act 2: запрашиваем по Id
        var getResponse = await _client.GetAsync($"/api/requests/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<RequestDto>();
        fetched.Should().NotBeNull();

        // Assert 2: поля совпадают с созданной заявкой (минимально важные)
        fetched!.Id.Should().Be(created.Id);
        fetched.Title.Should().Be(created.Title);
        fetched.Description.Should().Be(created.Description);
        fetched.RequestTypeId.Should().Be(created.RequestTypeId);
        fetched.RequestStatusId.Should().Be(created.RequestStatusId);
        fetched.InitiatorId.Should().Be(created.InitiatorId);
    }

    [Fact]
    public async Task GetRequestTypes_ReturnsCanonicalList()
    {
        // Act: типы заявок
        var typesResponse = await _client.GetAsync("/api/request-types");
        typesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var types = await typesResponse.Content.ReadFromJsonAsync<List<RequestTypeDto>>();
        types.Should().NotBeNull();
        var nonNullTypes = types!;

        nonNullTypes.Should().HaveCount(5, "справочник типов заявок должен быть канонизирован до exact-set из 5 элементов");

        nonNullTypes.Select(t => t.Id).Should().OnlyHaveUniqueItems("в справочнике типов не должно быть дубликатов по Id");

        var expectedIds = new[]
        {
            CustomerDevelopmentTypeId,
            InternalProductionTypeId,
            ChangeRequestTypeId,
            SupplyRequestTypeId,
            ExternalTechStageTypeId,
        };

        nonNullTypes.Select(t => t.Id)
            .Should()
            .BeEquivalentTo(expectedIds, options => options.WithoutStrictOrdering());

        // Exact-set по (Code, Name, Direction): ловит любые рассинхроны имен/направлений и появление лишних типов.
        nonNullTypes.Should().BeEquivalentTo(
            new[]
            {
                new { Id = CustomerDevelopmentTypeId, Name = "Заявка заказчика", Direction = "Incoming" },
                new { Id = InternalProductionTypeId, Name = "Внутренняя производственная заявка", Direction = "Incoming" },
                new { Id = ChangeRequestTypeId, Name = "Заявка на изменение (ECR/ECO-light)", Direction = "Incoming" },
                new { Id = SupplyRequestTypeId, Name = "Заявка на обеспечение/закупку", Direction = "Outgoing" },
                new { Id = ExternalTechStageTypeId, Name = "Заявка на внешний технологический этап", Direction = "Outgoing" },
            },
            options => options.WithoutStrictOrdering().ExcludingMissingMembers());
    }

    [Fact]
    public async Task GetRequestStatuses_ReturnsAtLeastDraft()
    {
        // Act: статусы заявок
        var statusesResponse = await _client.GetAsync("/api/request-statuses");
        statusesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statuses = await statusesResponse.Content.ReadFromJsonAsync<List<RequestStatusDto>>();
        statuses.Should().NotBeNull();
        statuses!.Count.Should().BeGreaterThan(0);

        // Убеждаемся, что как минимум статус Draft присутствует
        statuses.Should().Contain(s => s.Code == "Draft");
    }
}
