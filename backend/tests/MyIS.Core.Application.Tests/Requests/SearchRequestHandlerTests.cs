using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using Xunit;

namespace MyIS.Core.Application.Tests.Requests;

public class SearchRequestsHandlerTests
{
    private readonly Mock<IRequestRepository> _requestRepositoryMock = new();
    private readonly Mock<IRequestsAccessChecker> _accessCheckerMock = new();

    private SearchRequestsHandler CreateHandler()
    {
        return new SearchRequestsHandler(
            _requestRepositoryMock.Object,
            _accessCheckerMock.Object);
    }

    private static RequestType CreateRequestType(Guid? id = null, string code = "TYPE1", string name = "Type 1")
    {
        return new RequestType(
            new RequestTypeId(id ?? Guid.NewGuid()),
            code,
            name,
            description: "Test type");
    }

    private static RequestStatus CreateStatus(RequestStatusCode code, string name, bool isFinal = false)
    {
        return new RequestStatus(
            new RequestStatusId(Guid.NewGuid()),
            code,
            name,
            isFinal,
            description: "Test status");
    }

    private static Request CreateRequest(RequestType type, RequestStatus status, Guid initiatorId, string title)
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var request = Request.Create(
            type,
            status,
            initiatorId,
            title,
            description: null,
            createdAt: createdAt,
            dueDate: createdAt.AddDays(1));

        // Подготовим навигационные свойства, как если бы их подгрузил репозиторий/EF
        typeof(Request)
            .GetProperty(nameof(Request.Type))!
            .SetValue(request, type);

        typeof(Request)
            .GetProperty(nameof(Request.Status))!
            .SetValue(request, status);

        return request;
    }

    [Fact]
    public async Task Handle_ReturnsPagedResultAndMapsItems()
    {
        // Arrange
        var handler = CreateHandler();

        var currentUserId = Guid.NewGuid();
        var requestTypeId = Guid.NewGuid();
        var requestStatusId = Guid.NewGuid();

        var type = CreateRequestType(requestTypeId, code: "REQ", name: "Request");
        var status = CreateStatus(new RequestStatusCode("InReview"), "In review", isFinal: false);

        var request1 = CreateRequest(type, status, currentUserId, "Request 1");
        var request2 = CreateRequest(type, status, currentUserId, "Request 2");

        var items = new List<Request> { request1, request2 };
        const int totalCount = 10;

        _requestRepositoryMock
            .Setup(r => r.SearchAsync(
                requestTypeId,
                requestStatusId,
                currentUserId,
                true,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items as IReadOnlyList<Request>, totalCount));

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var query = new SearchRequestsQuery
        {
            RequestTypeId = requestTypeId,
            RequestStatusId = requestStatusId,
            OnlyMine = true,
            CurrentUserId = currentUserId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Page.Should().NotBeNull();
        result.Page.TotalCount.Should().Be(totalCount);
        result.Page.Items.Should().HaveCount(2);

        var first = result.Page.Items[0];
        first.Id.Should().Be(request1.Id.Value);
        first.Title.Should().Be(request1.Title);
        first.RequestTypeId.Should().Be(type.Id.Value);
        first.RequestTypeCode.Should().Be(type.Code);
        first.RequestTypeName.Should().Be(type.Name);
        first.RequestStatusId.Should().Be(status.Id.Value);
        first.RequestStatusCode.Should().Be(status.Code.Value);
        first.RequestStatusName.Should().Be(status.Name);
        first.InitiatorId.Should().Be(request1.InitiatorId);
        first.DueDate.Should().Be(request1.DueDate);

        // Параметры пагинации
        result.Page.PageNumber.Should().Be(1);
        result.Page.PageSize.Should().Be(20);

        // Проверка вызовов EnsureCanViewAsync для каждого запроса
        _accessCheckerMock.Verify(
            a => a.EnsureCanViewAsync(currentUserId, request1, It.IsAny<CancellationToken>()),
            Times.Once);

        _accessCheckerMock.Verify(
            a => a.EnsureCanViewAsync(currentUserId, request2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_AccessCheckerDeniesView_PropagatesException()
    {
        // Arrange
        var handler = CreateHandler();

        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var status = CreateStatus(RequestStatusCode.Draft, "Draft");
        var request = CreateRequest(type, status, currentUserId, "Request 1");

        var items = new List<Request> { request };

        _requestRepositoryMock
            .Setup(r => r.SearchAsync(
                null,
                null,
                null,
                false,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items as IReadOnlyList<Request>, 1));

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("no view"));

        var query = new SearchRequestsQuery
        {
            RequestTypeId = null,
            RequestStatusId = null,
            OnlyMine = false,
            CurrentUserId = currentUserId,
            PageNumber = 1,
            PageSize = 20
        };

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("no view");
    }

    [Fact]
    public async Task Handle_EmptyCurrentUserId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var query = new SearchRequestsQuery
        {
            CurrentUserId = Guid.Empty,
            PageNumber = 1,
            PageSize = 20
        };

        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "query")
            .WithMessage("*CurrentUserId is required.*");
    }

    [Fact]
    public async Task Handle_InvalidPageParameters_NormalizesToDefaults()
    {
        // Arrange
        var handler = CreateHandler();

        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var status = CreateStatus(RequestStatusCode.Draft, "Draft");
        var request = CreateRequest(type, status, currentUserId, "Request 1");
        var items = new List<Request> { request };

        // Ожидаем, что handler исправит pageNumber <= 0 и pageSize <= 0 на 1 и 20
        _requestRepositoryMock
            .Setup(r => r.SearchAsync(
                null,
                null,
                null,
                false,
                1,
                20,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((items as IReadOnlyList<Request>, 1));

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var query = new SearchRequestsQuery
        {
            CurrentUserId = currentUserId,
            PageNumber = 0,
            PageSize = 0
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Page.PageNumber.Should().Be(1);
        result.Page.PageSize.Should().Be(20);
    }
}