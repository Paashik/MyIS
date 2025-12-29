using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Application.Requests.Dto;
using MyIS.Core.Domain.Requests.ValueObjects;
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

            var deduped = prioritizedItems
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToArray();

            return Ok(deduped);
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
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToArray();

        return Ok(items);
    }

    [HttpGet("org-units")]
    public async Task<ActionResult<RequestOrgUnitLookupDto[]>> GetOrgUnits(
        [FromQuery] string? q,
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        take = Math.Clamp(take, 1, 500);

        var query = _dbContext.OrgUnits.AsNoTracking().Where(x => x.IsActive);
        if (q != null)
        {
            query = query.Where(x =>
                x.Name.Contains(q) ||
                (x.Code != null && x.Code.Contains(q)));
        }

        var items = await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Take(take)
            .Select(x => new RequestOrgUnitLookupDto
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                ParentId = x.ParentId,
                Phone = x.Phone,
                Email = x.Email
            })
            .ToArrayAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("incoming-requests")]
    public async Task<ActionResult<RequestBasisIncomingRequestLookupDto[]>> GetIncomingRequests(
        [FromQuery] string? q,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        take = Math.Clamp(take, 1, 200);

        var query =
            from request in _dbContext.Requests.AsNoTracking()
            join type in _dbContext.RequestTypes.AsNoTracking()
                on request.RequestTypeId equals type.Id
            where type.Direction == RequestDirection.Incoming
            select new { request, type };

        if (q != null)
        {
            query = query.Where(x =>
                x.request.Title.Contains(q) ||
                (x.request.Description != null && x.request.Description.Contains(q)));
        }

        var items = await query
            .OrderByDescending(x => x.request.CreatedAt)
            .ThenBy(x => x.request.Title)
            .Take(take)
            .Select(x => new RequestBasisIncomingRequestLookupDto
            {
                Id = x.request.Id.Value,
                Title = x.request.Title,
                RequestTypeName = x.type.Name
            })
            .ToArrayAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("customer-orders")]
    public async Task<ActionResult<RequestBasisCustomerOrderLookupDto[]>> GetCustomerOrders(
        [FromQuery] string? q,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        q = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        take = Math.Clamp(take, 1, 200);

        var query =
            from order in _dbContext.CustomerOrders.AsNoTracking()
            join customer in _dbContext.Counterparties.AsNoTracking()
                on order.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            select new { order, customer };

        if (q != null)
        {
            query = query.Where(x =>
                (x.order.Number != null && x.order.Number.Contains(q)) ||
                (x.customer != null && x.customer.Name.Contains(q)) ||
                (x.customer != null && x.customer.FullName != null && x.customer.FullName.Contains(q)));
        }

        var items = await query
            .OrderByDescending(x => x.order.OrderDate)
            .ThenByDescending(x => x.order.CreatedAt)
            .Take(take)
            .Select(x => new RequestBasisCustomerOrderLookupDto
            {
                Id = x.order.Id,
                Number = x.order.Number,
                CustomerName = x.customer != null ? x.customer.Name : null
            })
            .ToArrayAsync(cancellationToken);

        return Ok(items);
    }
}
