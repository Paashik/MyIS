using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Commands;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик команды удаления строки BOM
/// </summary>
public class DeleteEbomLineHandler : IRequestHandler<DeleteEbomLineCommand, DeleteEbomLineResponse>
{
    private readonly IBomLineRepository _bomLineRepository;

    public DeleteEbomLineHandler(IBomLineRepository bomLineRepository)
    {
        _bomLineRepository = bomLineRepository;
    }

    public async Task<DeleteEbomLineResponse> Handle(DeleteEbomLineCommand request, CancellationToken cancellationToken)
    {
        var bomLine = await _bomLineRepository.GetByIdAsync(request.LineId, cancellationToken);
        if (bomLine == null)
        {
            throw new KeyNotFoundException($"BOM line with ID {request.LineId} not found");
        }

        await _bomLineRepository.DeleteAsync(request.LineId, cancellationToken);

        return new DeleteEbomLineResponse(true);
    }
}