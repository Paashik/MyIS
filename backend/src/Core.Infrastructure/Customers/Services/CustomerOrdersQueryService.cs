using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Common.Dto;
using MyIS.Core.Application.Customers.Dto;
using MyIS.Core.Application.Customers.Queries;
using MyIS.Core.Domain.Statuses.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Customers.Services;

public sealed class CustomerOrdersQueryService : ICustomerOrdersQueryService
{
    private readonly AppDbContext _dbContext;

    public CustomerOrdersQueryService(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<PagedResultDto<CustomerOrderListItemDto>> GetCustomerOrdersAsync(
        string? q,
        Guid? customerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        if (pageSize < 1)
        {
            pageSize = 20;
        }

        if (pageSize > 200)
        {
            pageSize = 200;
        }

        var search = string.IsNullOrWhiteSpace(q) ? null : q.Trim();

        var customerOrderGroupIds = _dbContext.ExternalEntityLinks.AsNoTracking()
            .Where(link =>
                link.EntityType == nameof(Status) &&
                link.ExternalSystem == "Component2020" &&
                link.ExternalEntity == "StatusKind" &&
                link.ExternalId == "2")
            .Select(link => link.EntityId);

        var statusLookup =
            from status in _dbContext.Statuses.AsNoTracking()
            join statusGroup in _dbContext.Statuses.AsNoTracking()
                on status.GroupId equals statusGroup.Id
            where status.GroupId != null && customerOrderGroupIds.Contains(statusGroup.Id)
            select new { status, statusGroup };

        var statusLinks = _dbContext.ExternalEntityLinks.AsNoTracking()
            .Where(link =>
                link.EntityType == nameof(Status) &&
                link.ExternalSystem == "Component2020" &&
                link.ExternalEntity == "StatusCode");

        var query =
            from order in _dbContext.CustomerOrders.AsNoTracking()
            let orderStateCode = order.State.HasValue
                ? order.State.Value.ToString()
                : null
            join customer in _dbContext.Counterparties.AsNoTracking()
                on order.CustomerId equals customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            join employee in _dbContext.Employees.AsNoTracking()
                on order.PersonId equals employee.Id into employees
            from employee in employees.DefaultIfEmpty()
            join link in statusLinks
                on orderStateCode equals link.ExternalId into statusLinkSet
            from statusLink in statusLinkSet.DefaultIfEmpty()
            join statusByLink in statusLookup
                on statusLink.EntityId equals statusByLink.status.Id into statusesByLink
            from statusByLink in statusesByLink.DefaultIfEmpty()
            select new { order, customer, employee, statusByLink };

        if (customerId.HasValue)
        {
            query = query.Where(x => x.order.CustomerId == customerId.Value);
        }

        if (search != null)
        {
            var like = $"%{search}%";
            query = query.Where(x =>
                (x.order.Number != null && EF.Functions.Like(x.order.Number, like)) ||
                (x.customer != null && EF.Functions.Like(x.customer.Name, like)) ||
                (x.customer != null && x.customer.FullName != null && EF.Functions.Like(x.customer.FullName, like)) ||
                (x.employee != null && EF.Functions.Like(x.employee.FullName, like)) ||
                (x.employee != null && EF.Functions.Like(x.employee.ShortName, like)));
        }

        var total = await query.CountAsync(cancellationToken);

        var skip = (pageNumber - 1) * pageSize;

        var items = await query
            .OrderByDescending(x => x.order.OrderDate)
            .ThenByDescending(x => x.order.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new CustomerOrderListItemDto
            {
                Id = x.order.Id,
                Number = x.order.Number,
                OrderDate = x.order.OrderDate,
                DeliveryDate = x.order.DeliveryDate,
                State = x.order.State,
                CustomerId = x.order.CustomerId,
                CustomerName = x.customer != null ? x.customer.Name : null,
                PersonId = x.order.PersonId,
                PersonName = x.employee != null ? x.employee.ShortName : null,
                Contract = x.order.Contract,
                Note = x.order.Note,
                StatusName = x.statusByLink != null ? x.statusByLink.status.Name : null,
                StatusColor = x.statusByLink != null ? x.statusByLink.status.Color : null,
                CreatedAt = x.order.CreatedAt,
                UpdatedAt = x.order.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResultDto<CustomerOrderListItemDto>(items, total, pageNumber, pageSize);
    }
}
