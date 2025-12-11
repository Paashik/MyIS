using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Queries;
using MyIS.Core.Domain.Requests.Entities;
using MyIS.Core.Domain.Requests.ValueObjects;

namespace MyIS.Core.Application.Requests.Handlers;

public class GetRequestCommentsHandler
{
    private readonly IRequestRepository _requestRepository;
    private readonly IRequestCommentRepository _commentRepository;
    private readonly IRequestsAccessChecker _accessChecker;

    public GetRequestCommentsHandler(
        IRequestRepository requestRepository,
        IRequestCommentRepository commentRepository,
        IRequestsAccessChecker accessChecker)
    {
        _requestRepository = requestRepository ?? throw new ArgumentNullException(nameof(requestRepository));
        _commentRepository = commentRepository ?? throw new ArgumentNullException(nameof(commentRepository));
        _accessChecker = accessChecker ?? throw new ArgumentNullException(nameof(accessChecker));
    }

    public async Task<GetRequestCommentsResult> Handle(
        GetRequestCommentsQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null) throw new ArgumentNullException(nameof(query));

        if (query.RequestId == Guid.Empty)
        {
            throw new ArgumentException("RequestId is required.", nameof(query));
        }

        if (query.CurrentUserId == Guid.Empty)
        {
            throw new ArgumentException("CurrentUserId is required.", nameof(query));
        }

        var requestId = new RequestId(query.RequestId);

        // 1. Убеждаемся, что заявка существует
        var request = await _requestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            throw new InvalidOperationException($"Request with id '{query.RequestId}' was not found.");
        }

        // 2. Проверка прав на просмотр комментариев (те же права, что и на просмотр заявки)
        await _accessChecker.EnsureCanViewAsync(query.CurrentUserId, request, cancellationToken);

        // 3. Загрузка комментариев
        var items = await _commentRepository.GetByRequestIdAsync(requestId, cancellationToken);

        // 4. Маппинг в DTO
        var dtos = new List<RequestCommentDto>(items.Count);
        foreach (var c in items)
        {
            var dto = new RequestCommentDto
            {
                Id = c.Id,
                RequestId = c.RequestId.Value,
                AuthorId = c.AuthorId,
                AuthorFullName = null,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            };

            dtos.Add(dto);
        }

        return new GetRequestCommentsResult(dtos);
    }
}