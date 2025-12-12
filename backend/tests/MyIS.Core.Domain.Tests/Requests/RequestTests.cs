using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;
using Xunit;

namespace MyIS.Core.Domain.Tests.Requests;

public class RequestTests
{
    #region Helpers

    private static RequestType CreateRequestType(string code = "TYPE1", string name = "Type 1")
    {
        return new RequestType(
            RequestTypeId.New(),
            code,
            name,
            description: "Test type");
    }

    private static RequestStatus CreateStatus(RequestStatusCode code, string name, bool isFinal)
    {
        return new RequestStatus(
            RequestStatusId.New(),
            code,
            name,
            isFinal,
            description: "Test status");
    }

    private static void SetStatusNavigation(Request request, RequestStatus status)
    {
        var statusProperty = typeof(Request)
            .GetProperty(nameof(Request.Status), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        statusProperty.Should().NotBeNull("Status navigation property must exist on Request entity");

        statusProperty!.SetValue(request, status);
    }

    #endregion

    #region Create

    [Fact]
    public void Create_ValidData_CreatesRequestWithExpectedProperties()
    {
        // Arrange
        var type = CreateRequestType();
        var initialStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var initiatorId = Guid.NewGuid();

        var createdAt = DateTimeOffset.UtcNow;
        var dueDate = createdAt.AddDays(7);
        const string title = "  Test request  ";
        const string description = "  Description  ";
        const string relatedEntityType = "  Order  ";
        var relatedEntityId = Guid.NewGuid();
        const string externalReferenceId = "  EXT-123  ";

        // Act
        var request = Request.Create(
            type,
            initialStatus,
            initiatorId,
            title,
            description,
            createdAt,
            dueDate,
            relatedEntityType,
            relatedEntityId,
            externalReferenceId);

        // Assert
        request.Id.Value.Should().NotBeEmpty();
        request.Title.Should().Be("Test request");
        request.Description.Should().Be("Description");
        request.DueDate.Should().Be(dueDate);

        request.RequestTypeId.Value.Should().Be(type.Id.Value);
        request.RequestStatusId.Value.Should().Be(initialStatus.Id.Value);
        request.InitiatorId.Should().Be(initiatorId);

        request.RelatedEntityType.Should().Be("Order");
        request.RelatedEntityId.Should().Be(relatedEntityId);
        request.ExternalReferenceId.Should().Be("EXT-123");

        request.CreatedAt.Should().Be(createdAt);
        request.UpdatedAt.Should().Be(createdAt);

        request.History.Should().NotBeNull();
        request.History.Should().BeEmpty();
        request.Comments.Should().NotBeNull();
        request.Comments.Should().BeEmpty();
        request.Attachments.Should().NotBeNull();
        request.Attachments.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_EmptyTitle_Throws(string title)
    {
        // Arrange
        var type = CreateRequestType();
        var initialStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var initiatorId = Guid.NewGuid();

        // Act
        Action act = () => Request.Create(
            type,
            initialStatus,
            initiatorId,
            title,
            description: null,
            createdAt: DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "title");
    }

    [Fact]
    public void Create_EmptyInitiator_Throws()
    {
        // Arrange
        var type = CreateRequestType();
        var initialStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);

        // Act
        Action act = () => Request.Create(
            type,
            initialStatus,
            Guid.Empty,
            "Title",
            description: null,
            createdAt: DateTimeOffset.UtcNow);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "initiatorId");
    }

    #endregion

    #region UpdateDetails

    [Fact]
    public void UpdateDetails_InNonFinalStatus_UpdatesFields()
    {
        // Arrange
        var type = CreateRequestType();
        var initialStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var initiatorId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        var request = Request.Create(
            type,
            initialStatus,
            initiatorId,
            "Old title",
            "Old description",
            createdAt,
            createdAt.AddDays(1),
            "Order",
            Guid.NewGuid(),
            "EXT-1");

        var newDueDate = createdAt.AddDays(2);
        var updatedAt = DateTimeOffset.UtcNow;

        // Act
        request.UpdateDetails(
            "  New title  ",
            "  New description  ",
            newDueDate,
            "  Contract  ",
            relatedEntityId: null,
            externalReferenceId: "  EXT-2  ",
            updatedAt: updatedAt,
            isCurrentStatusFinal: false);

        // Assert
        request.Title.Should().Be("New title");
        request.Description.Should().Be("New description");
        request.DueDate.Should().Be(newDueDate);
        request.RelatedEntityType.Should().Be("Contract");
        request.RelatedEntityId.Should().BeNull();
        request.ExternalReferenceId.Should().Be("EXT-2");
        request.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void UpdateDetails_InFinalStatus_Throws()
    {
        // Arrange
        var type = CreateRequestType();
        var finalStatus = CreateStatus(RequestStatusCode.Done, "Done", isFinal: true);
        var initiatorId = Guid.NewGuid();

        var request = Request.Create(
            type,
            finalStatus,
            initiatorId,
            "Title",
            description: null,
            createdAt: DateTimeOffset.UtcNow);

        // Act
        Action act = () => request.UpdateDetails(
            "New title",
            description: null,
            dueDate: null,
            relatedEntityType: null,
            relatedEntityId: null,
            externalReferenceId: null,
            updatedAt: DateTimeOffset.UtcNow,
            isCurrentStatusFinal: true);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot update request details when it is in a final status.");
    }

    #endregion

    #region ChangeStatus

    [Fact]
    public void ChangeStatus_FromNonFinalToNonFinal_AddsHistoryAndUpdatesStatus()
    {
        // Arrange
        var type = CreateRequestType();
        var initialStatus = CreateStatus(RequestStatusCode.Draft, "Draft", isFinal: false);
        var initiatorId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-30);

        var request = Request.Create(
            type,
            initialStatus,
            initiatorId,
            "Title",
            description: null,
            createdAt: createdAt);

        var targetStatus = CreateStatus(RequestStatusCode.InReview, "In review", isFinal: false);
        var performedBy = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;
        const string action = "Status changed";
        const string comment = "Moved to review";

        // Act
        var historyItem = request.ChangeStatus(
            initialStatus,
            targetStatus,
            performedBy,
            timestamp,
            action,
            comment);

        // Assert: статус и UpdatedAt обновлены
        request.RequestStatusId.Value.Should().Be(targetStatus.Id.Value);
        request.UpdatedAt.Should().Be(timestamp);

        // Assert: история
        request.History.Should().HaveCount(1);
        request.History.Single().Should().BeSameAs(historyItem);

        historyItem.Action.Should().Be(action);
        historyItem.PerformedBy.Should().Be(performedBy);
        historyItem.Timestamp.Should().Be(timestamp);
        historyItem.Comment.Should().Be("Moved to review");
        historyItem.OldValue.Should().Be(initialStatus.Code.Value);
        historyItem.NewValue.Should().Be(targetStatus.Code.Value);
    }

    [Fact]
    public void ChangeStatus_FromFinalStatus_Throws()
    {
        // Arrange
        var type = CreateRequestType();
        var finalStatus = CreateStatus(RequestStatusCode.Done, "Done", isFinal: true);
        var initiatorId = Guid.NewGuid();

        var request = Request.Create(
            type,
            finalStatus,
            initiatorId,
            "Title",
            description: null,
            createdAt: DateTimeOffset.UtcNow);

        var targetStatus = CreateStatus(RequestStatusCode.Closed, "Closed", isFinal: true);

        // Act
        Action act = () => request.ChangeStatus(
            finalStatus,
            targetStatus,
            performedBy: Guid.NewGuid(),
            timestamp: DateTimeOffset.UtcNow,
            action: "Close",
            comment: null);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot change status when current status is final.");
    }

    #endregion

    #region Value Objects

    [Fact]
    public void RequestId_New_CreatesNonEmptyGuid()
    {
        var id = RequestId.New();

        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void RequestId_From_EmptyGuid_Throws()
    {
        Action act = () => RequestId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "value");
    }

    [Fact]
    public void RequestTypeId_New_CreatesNonEmptyGuid()
    {
        var id = RequestTypeId.New();

        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void RequestTypeId_From_EmptyGuid_Throws()
    {
        Action act = () => RequestTypeId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "value");
    }

    [Fact]
    public void RequestStatusId_New_CreatesNonEmptyGuid()
    {
        var id = RequestStatusId.New();

        id.Value.Should().NotBeEmpty();
    }

    [Fact]
    public void RequestStatusId_From_EmptyGuid_Throws()
    {
        Action act = () => RequestStatusId.From(Guid.Empty);

        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "value");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void RequestStatusCode_EmptyOrWhitespace_Throws(string value)
    {
        Action act = () => new RequestStatusCode(value);

        act.Should().Throw<ArgumentException>()
            .Where(e => e.ParamName == "value");
    }

    [Fact]
    public void RequestStatusCode_ToString_ReturnsTrimmedValue()
    {
        var code = new RequestStatusCode("  Draft  ");

        code.Value.Should().Be("Draft");
        code.ToString().Should().Be("Draft");
    }

    #endregion
}
