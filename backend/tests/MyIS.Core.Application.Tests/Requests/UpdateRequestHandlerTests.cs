using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Handlers;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using MyIS.Core.Domain.Users;
using Xunit;

namespace MyIS.Core.Application.Tests.Requests;

public class UpdateRequestHandlerTests
{
    private readonly Mock<IRequestRepository> _requestRepositoryMock = new();
    private readonly Mock<IRequestTypeRepository> _requestTypeRepositoryMock = new();
    private readonly Mock<IRequestStatusRepository> _requestStatusRepositoryMock = new();
    private readonly Mock<IRequestsAccessChecker> _accessCheckerMock = new();
    private readonly Mock<IUserRepository> _userRepositoryMock = new();

    private UpdateRequestHandler CreateHandler()
    {
        return new UpdateRequestHandler(
            _requestRepositoryMock.Object,
            _requestTypeRepositoryMock.Object,
            _requestStatusRepositoryMock.Object,
            _accessCheckerMock.Object,
            _userRepositoryMock.Object);
    }

    private static RequestType CreateRequestType(Guid? id = null, string name = "Type 1")
    {
        return new RequestType(
            new RequestTypeId(id ?? Guid.NewGuid()),
            name,
            RequestDirection.Incoming,
            description: "Test type");
    }

    private static RequestStatus CreateStatus(RequestStatusCode code, string name, bool isFinal)
    {
        return new RequestStatus(
            new RequestStatusId(Guid.NewGuid()),
            code,
            name,
            isFinal,
            description: "Test status");
    }

    private static User CreateUser(Guid id, string fullName = "Test User")
    {
        return User.Create(
            id: id,
            login: $"user-{id:N}",
            passwordHash: "hash",
            isActive: true,
            employeeId: null,
            now: DateTimeOffset.UtcNow,
            fullName: fullName);
    }

    private static Request CreateRequest(RequestType type, RequestStatus status, Guid initiatorId, string title = "Original title")
    {
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-30);

        return Request.Create(
            type,
            status,
            initiatorId,
            title,
            description: "Original description",
            createdAt: createdAt,
            dueDate: createdAt.AddDays(1),
            relatedEntityType: "Order",
            relatedEntityId: Guid.NewGuid(),
            externalReferenceId: "EXT-1");
    }

    [Fact]
    public async Task Handle_ValidCommand_UpdatesRequestAndReturnsDto()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var requestId = new RequestId(requestIdGuid);
        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var status = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var request = CreateRequest(type, status, initiatorId: currentUserId);

        // Ensure ids match what handler expects
        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, requestId);

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        _accessCheckerMock
            .Setup(a => a.EnsureCanUpdateAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _userRepositoryMock
            .Setup(r => r.GetByIdAsync(request.InitiatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(request.InitiatorId));

        Request? savedRequest = null;
        _requestRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()))
            .Callback((Request req, CancellationToken _) => savedRequest = req)
            .Returns(Task.CompletedTask);

        var newDueDate = DateTimeOffset.UtcNow.AddDays(2);

        var command = new UpdateRequestCommand
        {
            Id = requestIdGuid,
            CurrentUserId = currentUserId,
            Title = "New title",
            Description = "New description",
            DueDate = newDueDate,
            RelatedEntityType = "Contract",
            RelatedEntityId = null,
            ExternalReferenceId = "EXT-2"
        };

        // Act
        var dto = await handler.Handle(command, CancellationToken.None);

        // Assert: доступ проверен
        _accessCheckerMock.Verify(
            a => a.EnsureCanUpdateAsync(currentUserId, request, It.IsAny<CancellationToken>()),
            Times.Once);

        // Assert: репозиторий обновления вызван
        _requestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Once);

        savedRequest.Should().NotBeNull();
        savedRequest!.Title.Should().Be(request.Title);
        savedRequest.Description.Should().Be(command.Description);
        savedRequest.DueDate.Should().Be(newDueDate);
        savedRequest.RelatedEntityType.Should().Be(command.RelatedEntityType);
        savedRequest.RelatedEntityId.Should().BeNull();
        savedRequest.ExternalReferenceId.Should().Be(command.ExternalReferenceId);
        savedRequest.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(5));

        // Assert: DTO маппится корректно
        dto.Should().NotBeNull();
        dto.Id.Should().Be(savedRequest.Id.Value);
        dto.Title.Should().Be(request.Title);
        dto.Description.Should().Be(savedRequest.Description);
        dto.RequestTypeId.Should().Be(type.Id.Value);
        dto.RequestTypeName.Should().Be(type.Name);
        dto.RequestStatusId.Should().Be(status.Id.Value);
        dto.RequestStatusCode.Should().Be(status.Code.Value);
        dto.RequestStatusName.Should().Be(status.Name);
        dto.InitiatorId.Should().Be(savedRequest.InitiatorId);
        dto.RelatedEntityType.Should().Be(savedRequest.RelatedEntityType);
        dto.RelatedEntityId.Should().Be(savedRequest.RelatedEntityId);
        dto.ExternalReferenceId.Should().Be(savedRequest.ExternalReferenceId);
        dto.UpdatedAt.Should().Be(savedRequest.UpdatedAt);
    }

    [Fact]
    public async Task Handle_RequestNotFound_ThrowsInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var command = new UpdateRequestCommand
        {
            Id = Guid.NewGuid(),
            CurrentUserId = Guid.NewGuid(),
            Title = "Title"
        };

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<RequestId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Request?)null);

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"Request with id '{command.Id}' was not found.");

        _requestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_FinalStatus_UpdateFails_WithDomainInvalidOperationException()
    {
        // Arrange
        var handler = CreateHandler();

        var requestIdGuid = Guid.NewGuid();
        var requestId = new RequestId(requestIdGuid);
        var currentUserId = Guid.NewGuid();

        var type = CreateRequestType();
        var finalStatus = CreateStatus(RequestStatusCode.Done, "Done", isFinal: true);
        var request = CreateRequest(type, finalStatus, initiatorId: currentUserId);

        typeof(Request)
            .GetProperty(nameof(Request.Id))!
            .SetValue(request, requestId);

        _requestRepositoryMock
            .Setup(r => r.GetByIdAsync(requestId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(request);

        _requestTypeRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(type);

        _requestStatusRepositoryMock
            .Setup(r => r.GetByIdAsync(request.RequestStatusId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(finalStatus);

        _accessCheckerMock
            .Setup(a => a.EnsureCanUpdateAsync(currentUserId, request, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var command = new UpdateRequestCommand
        {
            Id = requestIdGuid,
            CurrentUserId = currentUserId,
            Title = "New title"
        };

        // Act
        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        // Assert: ожидаем проброс доменного InvalidOperationException
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot update request details when it is in a final status.");

        _requestRepositoryMock.Verify(
            r => r.UpdateAsync(It.IsAny<Request>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new UpdateRequestCommand
        {
            Id = Guid.Empty,
            CurrentUserId = Guid.NewGuid(),
            Title = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*Id is required.*");
    }

    [Fact]
    public async Task Handle_EmptyCurrentUserId_ThrowsArgumentException()
    {
        var handler = CreateHandler();

        var command = new UpdateRequestCommand
        {
            Id = Guid.NewGuid(),
            CurrentUserId = Guid.Empty,
            Title = "Test"
        };

        Func<Task> act = async () => await handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .Where(e => e.ParamName == "command")
            .WithMessage("*CurrentUserId is required.*");
    }
}
