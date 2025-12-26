using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Security.Abstractions;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class AddRequestCommentHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestsAccessChecker _accessChecker;
    private readonly IUserRepository _userRepository;

    public AddRequestCommentHandler(
        IRequestRepository requestRepository,
        IRequestsAccessChecker accessChecker,
        IUserRepository userRepository)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<RequestCommentDto> Handle(
        AddRequestCommentCommand command,
        CancellationToken cancellationToken)
    {
        if (command is null) throw new ArgumentNullException(nameof(command));

        if (command.RequestId == Guid.Empty)
        {
            throw new ArgumentException("RequestId is required.", nameof(command));
        }

        if (command.AuthorId == Guid.Empty)
        {
            throw new ArgumentException("AuthorId is required.", nameof(command));
        }

        if (string.IsNullOrWhiteSpace(command.Text))
        {
            throw new ArgumentException("Text is required.", nameof(command));
        }

        var requestId = new RequestId(command.RequestId);

        // 1. Убеждаемся, что заявка существует
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{command.RequestId}' was not found.");
        }

        // 2. Проверка прав на добавление комментария
        await _accessChecker.EnsureCanAddCommentAsync(command.AuthorId, requestId, cancellationToken);

        // 3. Создание доменной сущности комментария
        var createdAt = command.CreatedAt == default
            ? DateTimeOffset.UtcNow
            : command.CreatedAt;

        var comment = request.AddComment(
            authorId: command.AuthorId,
            text: command.Text,
            createdAt: createdAt);

        // 4. Сохранение агрегата через корень
        await _requestRepository.UpdateAsync(request, cancellationToken);

        // 5. Маппинг в DTO
        var author = await _userRepository.GetByIdAsync(comment.AuthorId, cancellationToken);
        var authorBaseName = author?.Employee?.ShortName ?? author?.Employee?.FullName ?? author?.FullName ?? author?.Login;
        var authorFullName = PersonNameFormatter.ToShortName(authorBaseName) ?? authorBaseName;

        var dto = new RequestCommentDto
        {
            Id = comment.Id,
            RequestId = comment.RequestId.Value,
            AuthorId = comment.AuthorId,
            AuthorFullName = authorFullName,
            Text = comment.Text,
            CreatedAt = comment.CreatedAt
        };

        return dto;
    }
}
