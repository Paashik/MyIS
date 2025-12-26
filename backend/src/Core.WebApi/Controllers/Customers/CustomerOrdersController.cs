using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Customers.Dto;
using MyIS.Core.Application.Customers.Queries;

namespace MyIS.Core.WebApi.Controllers.Customers;

[ApiController]
[Route("api/customers/orders")]
[Authorize]
public sealed class CustomerOrdersController : ControllerBase
{
    private readonly ICustomerOrdersQueryService _queryService;

    public CustomerOrdersController(ICustomerOrdersQueryService queryService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    }

    /// <summary>
    /// Список заказов клиентов (импорт из Access).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<CustomerOrderListItemDto>>> GetCustomerOrders(
        [FromQuery] string? q = null,
        [FromQuery] Guid? customerId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _queryService.GetCustomerOrdersAsync(q, customerId, pageNumber, pageSize, cancellationToken);
        return Ok(result);
    }
}
