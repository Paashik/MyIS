using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Workflow;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Workflow;
using MyIS.Core.Application.Security.Abstractions;

namespace MyIS.Core.Application.Requests.Handlers.Workflow;

public sealed class StartReviewRequestHandler : RequestWorkflowActionHandlerBase
{
    public StartReviewRequestHandler(
        IRequestRepository requestRepository,
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository,
        IRequestsAccessChecker accessChecker,
        IUserRepository userRepository)
        : base(requestRepository, requestTypeRepository, requestStatusRepository, transitionRepository, accessChecker, userRepository)
    {
    }

    public Task<RequestDto> Handle(StartReviewRequestCommand command, CancellationToken cancellationToken)
        => ExecuteAsync(command.RequestId, command.CurrentUserId, RequestActionCodes.StartReview, command.Comment, cancellationToken);
}

