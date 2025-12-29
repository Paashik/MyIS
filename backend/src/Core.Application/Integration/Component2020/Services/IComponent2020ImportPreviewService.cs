using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Dto;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020ImportPreviewService
{
    Task<Component2020ImportPreviewResponseDto> PreviewAsync(
        Component2020ImportPreviewRequestDto request,
        CancellationToken cancellationToken);
}
