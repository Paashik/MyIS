using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyIS.Core.Application.Mdm.References;

namespace MyIS.Core.WebApi.Controllers.Admin.References;

[ApiController]
[Route("api/admin/references/mdm")]
public sealed class AdminMdmReferencesController : ControllerBase
{
    private readonly IMdmReferencesQueryService _service;

    public AdminMdmReferencesController(IMdmReferencesQueryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    [HttpGet("units")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetUnits(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetUnitsAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("units/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetUnitById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetUnitByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("suppliers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSuppliers(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetSuppliersAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("suppliers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSupplierById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetSupplierByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("customers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCustomers(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetCustomersAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("customers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCustomerById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetCustomerByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("counterparties")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCounterparties(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] string? roleType = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetCounterpartiesAsync(q, isActive, roleType, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("counterparties/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCounterpartyById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetCounterpartyByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("item-groups")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItemGroups(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetItemGroupsAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("item-groups/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItemGroupById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetItemGroupByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("items")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItems(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? groupId = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetItemsAsync(q, isActive, groupId, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpPost("items/purge")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> PurgeItems(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var clearedRequestLines = await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE requests.request_lines SET \"item_id\" = NULL WHERE \"item_id\" IN (SELECT \"Id\" FROM mdm.items);",
            cancellationToken);

        var deletedAttributeValues = await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM mdm.item_attribute_values WHERE \"ItemId\" IN (SELECT \"Id\" FROM mdm.items);",
            cancellationToken);

        var deletedLinks = await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM integration.external_entity_links WHERE \"EntityType\" = 'Item';",
            cancellationToken);

        var deletedItems = await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM mdm.items;",
            cancellationToken);

        var deletedSequences = await dbContext.Database.ExecuteSqlRawAsync(
            "DELETE FROM mdm.item_sequences;",
            cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return Ok(new
        {
            deletedItems,
            deletedLinks,
            deletedAttributeValues,
            deletedSequences,
            clearedRequestLines
        });
    }

    [HttpGet("items/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItemById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetItemByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("items/{id:guid}/photo")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetItemPhoto(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var photo = await dbContext.Items
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => x.Photo)
            .FirstOrDefaultAsync(cancellationToken);

        if (photo == null || photo.Length == 0)
        {
            return NotFound();
        }

        var contentType = ResolveImageContentType(photo);
        return File(photo, contentType);
    }

    [HttpGet("manufacturers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetManufacturers(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetManufacturersAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("manufacturers/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetManufacturerById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetManufacturerByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("body-types")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetBodyTypes(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetBodyTypesAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("body-types/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetBodyTypeById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetBodyTypeByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("currencies")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCurrencies(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetCurrenciesAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("currencies/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetCurrencyById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetCurrencyByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("technical-parameters")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetTechnicalParameters(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetTechnicalParametersAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("technical-parameters/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetTechnicalParameterById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetTechnicalParameterByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("parameter-sets")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetParameterSets(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetParameterSetsAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("parameter-sets/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetParameterSetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetParameterSetByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("symbols")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSymbols(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetSymbolsAsync(q, isActive, skip, take, cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    [HttpGet("symbols/{id:guid}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetSymbolById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetSymbolByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    [HttpGet("external-links")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetExternalLinks(
        CancellationToken cancellationToken,
        [FromQuery] string? q = null,
        [FromQuery] string? entityType = null,
        [FromQuery] string? externalSystem = null,
        [FromQuery] string? externalEntity = null,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 200)
    {
        var result = await _service.GetExternalEntityLinksAsync(
            q,
            entityType,
            externalSystem,
            externalEntity,
            skip,
            take,
            cancellationToken);
        return Ok(new { total = result.Total, items = result.Items });
    }

    private static string ResolveImageContentType(byte[] data)
    {
        if (data.Length >= 4)
        {
            if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47)
            {
                return "image/png";
            }
            if (data[0] == 0xFF && data[1] == 0xD8)
            {
                return "image/jpeg";
            }
            if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46)
            {
                return "image/gif";
            }
            if (data[0] == 0x42 && data[1] == 0x4D)
            {
                return "image/bmp";
            }
            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            {
                return "image/webp";
            }
        }

        return "application/octet-stream";
    }
}
