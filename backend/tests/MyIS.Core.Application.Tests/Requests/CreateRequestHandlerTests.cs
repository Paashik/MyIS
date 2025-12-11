using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using Xunit;

namespace MyIS.Core.Application.Tests.Requests;

public class CreateRequestHandlerTests
{
    private readonly Mock<IRequestRepository> _requestRepositoryMock = new();
    private readonly Mock<IRequestTypeRepository> _requestTypeRepositoryMock = new();
    private readonly Mock<IRequestStatusRepository> _requestStatusRepositoryMock = new();
    private readonly Mock<IRequestsAccessChecker> _accessCheckerMock = new();

    private CreateRequestHandler CreateHandler()
    {
        return new CreateRequestHandler(
            _requestRepositoryMock.Object,
            _requestTypeRepositoryMock.Object,
            _requestStatusRepositoryMock.Object,
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

    [Fact]
    public async Task Handle_ValidCommand_CreatesRequestAndReturnsDto()
    {
        // Arrange
        var handler = CreateHandler();

        var requestTypeId = Guid.NewGuid();
        var initiatorId = Guid.NewGuid();

        var type = CreateRequestType(requestTypeId, code: "REQ", name: "Request");
        var draftStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestTypeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestTypeId id, CancellationToken _) =>
                id.Value == type.Id.Value ? type : null);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByCodeAsync(RequestStatusCode.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftStatus);

        _accessCheckerMock
            .Setup(a => a.EnsureCanCreateAsync(initiatorId, type, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        Request? savedRequest = null;
        _requestRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .Callback((Request req, CancellationToken _) => savedRequest = req)
            .Returns(Task.CompletedTask);

        var command = new CreateRequestCommand
        {
            InitiatorId = initiatorId,
            RequestTypeId = requestTypeId,
            Title = "Test title",
            Description = "Test description",
            DueDate = DateTimeOffset.UtcNow.AddDays(1),
            RelatedEntityType = "Order",
            RelatedEntityId = Guid.NewGuid(),
            ExternalReferenceId = "EXT-1"
        };

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert: репозиторий вызван
        _requestRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Once);

        savedRequest.Should().NotBeNull();
        savedRequest!.Title.Should().Be(command.Title);
        savedRequest.Description.Should().Be(command.Description);
        savedRequest.RequestTypeId.Value.Should().Be(requestTypeId);
        savedRequest.RequestStatusId.Value.Should().Be(draftStatus.Id.Value);
        savedRequest.InitiatorId.Should().Be(initiatorId);

        // Assert: DTO маппится корректно
        dto.Should().NotBeNull();
        dto.Id.Should().Be(savedRequest.Id.Value);
        dto.Title.Should().Be(command.Title);
        dto.Description.Should().Be(command.Description);
        dto.RequestTypeId.Should().Be(type.Id.Value);
        dto.RequestTypeCode.Should().Be(type.Code);
        dto.RequestTypeName.Should().Be(type.Name);
        dto.RequestStatusId.Should().Be(draftStatus.Id.Value);
        dto.RequestStatusCode.Should().Be(draftStatus.Code.Value);
        dto.RequestStatusName.Should().Be(draftStatus.Name);
        dto.InitiatorId.Should().Be(initiatorId);
        dto.RelatedEntityType.Should().Be(command.RelatedEntityType);
        dto.RelatedEntityId.Should().Be(command.RelatedEntityId);
        dto.ExternalReferenceId.Should().Be(command.ExternalReferenceId);
        dto.CreatedAt.Should().Be(dto.UpdatedAt); // при создании равны
    }

    [Fact]
    public async Task Handle_AccessCheckerThrows_PropagatesException_AndDoesNotCallRepository()
    {
        // Arrange
        var handler = CreateHandler();

        var requestTypeId = Guid.NewGuid();
        var initiatorId = Guid.NewGuid();

        var type = CreateRequestType(requestTypeId);
        var draftStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestTypeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByCodeAsync(RequestStatusCode.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draftStatus);

        _accessCheckerMock
            .Setup(a => a.EnsureCanCreateAsync(initiatorId, type, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException("no access"));

        var command = new CreateRequestCommand
        {
            InitiatorId = initiatorId,
            RequestTypeId = requestTypeId,
            Title = "Test title"
        };

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("no access");

        _requestRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingRequestType_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new CreateRequestCommand
        {
            InitiatorId = Guid.NewGuid(),
            RequestTypeId = Guid.NewGuid(),
            Title = "Test"
        };

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestTypeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestType?)null);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"RequestType with id '{command.RequestTypeId}' was not found.");

        _requestRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_MissingDraftStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var requestTypeId = Guid.NewGuid();
        var initiatorId = Guid.NewGuid();
        var type = CreateRequestType(requestTypeId);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestTypeId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByCodeAsync(RequestStatusCode.Draft, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RequestStatus?)null);

        var command = new CreateRequestCommand
        {
            InitiatorId = initiatorId,
            RequestTypeId = requestTypeId,
            Title = "Test"
        };

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Initial RequestStatus 'Draft' is not configured.");

        _requestRepositoryMock.Verify(
            r => r.AddAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyInitiatorId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new CreateRequestCommand
        {
            InitiatorId = Guid.Empty,
            RequestTypeId = Guid.NewGuid(),
            Title = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*InitiatorId is required.*");
    }

    [Fact]
    public async Task Handle_EmptyRequestTypeId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new CreateRequestCommand
        {
            InitiatorId = Guid.NewGuid(),
            RequestTypeId = Guid.Empty,
            Title = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*RequestTypeId is required.*");
    }
}