using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Customers.Dto;

namespace MyIS.Core.Application.Customers.Queries;

public interface ICustomerOrdersQueryService
{
    Task<PagedResultDto<CustomerOrderListItemDto>> GetCustomerOrdersAsync(
        string? q,
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
