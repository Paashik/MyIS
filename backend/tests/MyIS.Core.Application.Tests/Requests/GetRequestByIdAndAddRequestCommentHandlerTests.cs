using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using Xunit;

namespace MyIS.Core.Application.Tests.Requests;

public class GetRequestByIdHandlerTests
{
    private readonly Mock<IRequestRepository> _requestRepositoryMock = new();
    private readonly Mock<IRequestTypeRepository> _requestTypeRepositoryMock = new();
    private readonly Mock<IRequestStatusRepository> _requestStatusRepositoryMock = new();
    private readonly Mock<IRequestsAccessChecker> _accessCheckerMock = new();

    private GetRequestByIdHandler CreateHandler()
    {
        return new GetRequestByIdHandler(
            _requestRepositoryMock.Object,
            _requestTypeRepositoryMock.Object,
            _requestStatusRepositoryMock.Object,
            _accessCheckerMock.Object);
    }

    private static RequestType CreateRequestType(Guid? id = null, string name = "Type 1")
    {
        return new RequestType(
            new RequestTypeId(id ?? Guid.NewGuid()),
            name,
            RequestDirection.Incoming,
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

    private static Request CreateRequest(RequestType type, RequestStatus status, Guid initiatorId, string title = "Title")
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-5);

        return Request.Create(
            type,
            status,
            initiatorId,
            title,
            description: "Description",
            createdAt: createdAt,
            dueDate: createdAt.AddDays(1));
    }

    [Fact]
    public async Task Handle_ExistingRequest_ReturnsDto()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType(name: "Request");
        var status = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var request = CreateRequest(type, status, initiatorId: currentUserId, title: "My request");

        // Принудительно задаём Id, чтобы он совпадал с query.Id
        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, new RequestId(requestIdGuid));

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var query = new GetRequestByIdQuery
        {
            Id = requestIdGuid,
            CurrentUserId = currentUserId
        };

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Request.Should().NotBeNull();

        var dto = result.Request;
        dto.Id.Should().Be(request.Id.Value);
        dto.Title.Should().Be(request.Title);
        dto.Description.Should().Be(request.Description);
        dto.RequestTypeId.Should().Be(type.Id.Value);
        dto.RequestTypeName.Should().Be(type.Name);
        dto.RequestStatusId.Should().Be(status.Id.Value);
        dto.RequestStatusCode.Should().Be(status.Code.Value);
        dto.RequestStatusName.Should().Be(status.Name);
        dto.InitiatorId.Should().Be(request.InitiatorId);
        dto.RelatedEntityType.Should().Be(request.RelatedEntityType);
        dto.RelatedEntityId.Should().Be(request.RelatedEntityId);
        dto.ExternalReferenceId.Should().Be(request.ExternalReferenceId);
        dto.CreatedAt.Should().Be(request.CreatedAt);
        dto.UpdatedAt.Should().Be(request.UpdatedAt);

        _accessCheckerMock.Verify(
            a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var query = new GetRequestByIdQuery
        {
            Id = Guid.NewGuid(),
            CurrentUserId = Guid.NewGuid()
        };

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Request?)null);

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Request with id '{query.Id}' was not found.");

        _accessCheckerMock.Verify(
            a => a.EnsureCanViewAsync(It.IsAny<Guid>(), It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingRequestType_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var status = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var request = CreateRequest(type, status, initiatorId: currentUserId);

        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, new RequestId(requestIdGuid));

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestType?)null);

        var query = new GetRequestByIdQuery
        {
            Id = requestIdGuid,
            CurrentUserId = currentUserId
        };

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"RequestType with id '{request.RequestTypeId.Value}' was not found.");
    }

    [Fact]
    public async Task Handle_MissingRequestStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var status = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var request = CreateRequest(type, status, initiatorId: currentUserId);

        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, new RequestId(requestIdGuid));

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _accessCheckerMock
            .Setup(a => a.EnsureCanViewAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestStatus?)null);

        var query = new GetRequestByIdQuery
        {
            Id = requestIdGuid,
            CurrentUserId = currentUserId
        };

        // Act
        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"RequestStatus with id '{request.RequestStatusId.Value}' was not found.");
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var query = new GetRequestByIdQuery
        {
            Id = Guid.Empty,
            CurrentUserId = Guid.NewGuid()
        };

        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "query")
            .WithMessage("*Id is required.*");
    }

    [Fact]
    public async Task Handle_EmptyCurrentUserId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var query = new GetRequestByIdQuery
        {
            Id = Guid.NewGuid(),
            CurrentUserId = Guid.Empty
        };

        Func<Task> act = async () => await handler.Handle(query, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "query")
            .WithMessage("*CurrentUserId is required.*");
    }
}

public class AddRequestCommentHandlerTests
{
    private readonly Mock<IRequestRepository> _requestRepositoryMock = new();
    private readonly Mock<IRequestsAccessChecker> _accessCheckerMock = new();

    private AddRequestCommentHandler CreateHandler()
    {
        return new AddRequestCommentHandler(
            _requestRepositoryMock.Object,
            _accessCheckerMock.Object);
    }

    private static RequestType CreateRequestType()
    {
        return new RequestType(
            RequestTypeId.New(),
            "Type 1",
            RequestDirection.Incoming,
            description: "Test type");
    }

    private static RequestStatus CreateStatus()
    {
        return new RequestStatus(
            RequestStatusId.New(),
            RequestStatusCode.Draft,
            "Draft",
            isFinal: false,
            description: "Test status");
    }

    private static Request CreateRequest(Guid requestId, Guid initiatorId)
    {
        var type = CreateRequestType();
        var status = CreateStatus();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var request = Request.Create(
            type,
            status,
            initiatorId,
            "Title",
            description: null,
            createdAt: createdAt);

        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, new RequestId(requestId));

        return request;
    }

    [Fact]
    public async Task Handle_ValidCommand_AddsCommentAndReturnsDto()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var request = CreateRequest(requestIdGuid, initiatorId: authorId);

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _accessCheckerMock
            .Setup(a => a.EnsureCanAddCommentAsync(authorId, It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Request? savedAggregate = null;

        _requestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .Callback((Request r, CancellationToken _) => savedAggregate = r)
            .Returns(Task.CompletedTask);

        var command = new AddRequestCommentCommand
        {
            RequestId = requestIdGuid,
            AuthorId = authorId,
            Text = "My comment",
            CreatedAt = createdAt
        };

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert
        _requestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Once);

        savedAggregate.Should().NotBeNull();
        savedAggregate!.Comments.Should().HaveCount(1);
        var savedComment = savedAggregate.Comments.Should().ContainSingle().Which;
        savedComment.RequestId.Value.Should().Be(requestIdGuid);
        savedComment.AuthorId.Should().Be(authorId);
        savedComment.Text.Should().Be("My comment");
        savedComment.CreatedAt.Should().Be(createdAt);

        dto.Should().NotBeNull();
        dto.Id.Should().Be(savedComment.Id);
        dto.RequestId.Should().Be(requestIdGuid);
        dto.AuthorId.Should().Be(authorId);
        dto.Text.Should().Be(savedComment.Text);
        dto.CreatedAt.Should().Be(savedComment.CreatedAt);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new AddRequestCommentCommand
        {
            RequestId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Text = "Test"
        };

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Request?)null);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Request with id '{command.RequestId}' was not found.");

        _requestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyRequestId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new AddRequestCommentCommand
        {
            RequestId = Guid.Empty,
            AuthorId = Guid.NewGuid(),
            Text = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*RequestId is required.*");
    }

    [Fact]
    public async Task Handle_EmptyAuthorId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new AddRequestCommentCommand
        {
            RequestId = Guid.NewGuid(),
            AuthorId = Guid.Empty,
            Text = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*AuthorId is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Handle_EmptyText_ThrowsArgumentException(string text)
    {
        var handler = CreateHandler();

        var command = new AddRequestCommentCommand
        {
            RequestId = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Text = text
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*Text is required.*");
    }
}
