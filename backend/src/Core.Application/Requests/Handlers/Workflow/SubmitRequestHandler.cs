using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Requests.Abstractions;
using MyIS.Core.Application.Requests.Commands.Workflow;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Application.Requests.Workflow;

namespace MyIS.Core.Application.Requests.Handlers.Workflow;

public sealed class SubmitRequestHandler : RequestWorkflowActionHandlerBase
{
    public SubmitRequestHandler(
        IRequestRepository requestRepository,
        IRequestTypeRepository requestTypeRepository,
        IRequestStatusRepository requestStatusRepository,
        IRequestTransitionRepository transitionRepository,
        IRequestsAccessChecker accessChecker)
        : base(requestRepository, requestTypeRepository, requestStatusRepository, transitionRepository, accessChecker)
    {
    }

    public Task<RequestDto> Handle(SubmitRequestCommand command, CancellationToken cancellationToken)
        => ExecuteAsync(command.RequestId, command.CurrentUserId, RequestActionCodes.Submit, command.Comment, cancellationToken);
}

