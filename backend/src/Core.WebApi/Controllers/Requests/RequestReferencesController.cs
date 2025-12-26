using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.WebApi.Controllers.Requests;

[ApiController]
[Route("api/requests/references")]
[Authorize]
public sealed class RequestReferencesController : ControllerBase
{
    private readonly IMdmReferencesQueryService _service;
    private readonly AppDbContext _dbContext;

    public RequestReferencesController(IMdmReferencesQueryService service, AppDbContext dbContext)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    [HttpGet("counterparties")]
    public async Task<ActionResult<RequestCounterpartyLookupDto[]>> GetCounterparties(
        [FromQuery] string? q,
        [FromQuery] bool prioritizeByOrders = false,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (prioritizeByOrders)
        {
            q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            take = Math.Clamp(take, 1, 200);

            var query =
                from c in _dbContext.Counterparties.AsNoTracking()
                join o in _dbContext.CustomerOrders.AsNoTracking() on c.Id equals o.CustomerId into orders
                select new
                {
                    Counterparty = c,
                    OrdersCount = orders.Count()
                };

            if (q != null)
            {
                query = query.Where(x =>
                    x.Counterparty.Name.Contains(q) ||
                    (x.Counterparty.FullName != null && x.Counterparty.FullName.Contains(q)) ||
                    (x.Counterparty.Inn != null && x.Counterparty.Inn.Contains(q)) ||
                    (x.Counterparty.Kpp != null && x.Counterparty.Kpp.Contains(q)));
            }

            query = query.Where(x => x.Counterparty.IsActive);

            var prioritizedItems = await query
                .OrderByDescending(x => x.OrdersCount)
                .ThenBy(x => x.Counterparty.Name)
                .ThenBy(x => x.Counterparty.FullName ?? string.Empty)
                .Skip(Math.Max(0, skip))
                .Take(take)
                .Select(x => new RequestCounterpartyLookupDto
                {
                    Id = x.Counterparty.Id,
                    Name = x.Counterparty.Name,
                    FullName = x.Counterparty.FullName
                })
                .ToListAsync(cancellationToken);

            return Ok(prioritizedItems);
        }

        var result = await _service.GetCounterpartiesAsync(
            q: q,
            isActive: true,
            roleType: null,
            skip: skip,
            take: take,
            cancellationToken: cancellationToken);

        var items = result.Items
            .Select(x => new RequestCounterpartyLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                FullName = x.FullName
            })
            .ToArray();

        return Ok(items);
    }
}
