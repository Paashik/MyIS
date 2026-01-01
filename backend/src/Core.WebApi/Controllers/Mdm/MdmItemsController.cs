using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Mdm.References;
using MyIS.Core.Domain.Mdm.Entities;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.WebApi.Controllers.Mdm;

[ApiController]
[Route("api/mdm/items")]
[Authorize]
public sealed class MdmItemsController : ControllerBase
{
    private readonly IMdmReferencesQueryService _service;

    public MdmItemsController(IMdmReferencesQueryService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>
    /// Полные данные item (reference DTO). Используется для экранов справочника/карточки.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entity = await _service.GetItemByIdAsync(id, cancellationToken);
        return entity == null ? NotFound() : Ok(entity);
    }

    /// <summary>
    /// Lightweight lookup DTO для eBOM и других UI-сценариев подбора.
    /// </summary>
    [HttpGet("lookup/{id:guid}")]
    public async Task<IActionResult> GetLookupById(
        Guid id,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Include(x => x.ItemGroup)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (item == null)
        {
            return NotFound();
        }

        return Ok(MapToLookup(item));
    }

    /// <summary>
    /// Поиск номенклатуры для UI (например, подбор строки eBOM).
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        [FromServices] AppDbContext dbContext,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 200);

        q = (q ?? string.Empty).Trim();
        if (q.Length == 0)
        {
            return Ok(Array.Empty<MdmItemLookupDto>());
        }

        var qLike = $"%{q}%";
        var items = await dbContext.Items
            .AsNoTracking()
            .Include(x => x.ItemGroup)
            .Where(x =>
                (x.Code != null && EF.Functions.ILike(x.Code, qLike)) ||
                EF.Functions.ILike(x.NomenclatureNo, qLike) ||
                EF.Functions.ILike(x.Name, qLike) ||
                (x.Designation != null && EF.Functions.ILike(x.Designation, qLike)) ||
                (x.ManufacturerPartNumber != null && EF.Functions.ILike(x.ManufacturerPartNumber, qLike)))
            .OrderBy(x => x.NomenclatureNo)
            .Take(take)
            .ToListAsync(cancellationToken);

        var result = items.Select(MapToLookup).ToArray();
        return Ok(result);
    }

    [HttpGet("{id:guid}/photo")]
    public async Task<IActionResult> GetPhoto(
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

    private static MdmItemLookupDto MapToLookup(Item item) => new()
    {
        Id = item.Id,
        Code = string.IsNullOrWhiteSpace(item.Code) ? null : item.Code,
        Name = item.Name,
        ItemType = MapItemKindToItemType(item.ItemKind),
        GroupName = item.ItemGroup?.Name,
        IsActive = item.IsActive
    };

    private static string MapItemKindToItemType(ItemKind kind) => kind switch
    {
        ItemKind.PurchasedComponent => "Component",
        ItemKind.StandardPart => "Component",
        ItemKind.ManufacturedPart => "Component",
        ItemKind.Material => "Material",
        ItemKind.Assembly => "Assembly",
        ItemKind.Product => "Product",
        ItemKind.ServiceWork => "Service",
        ItemKind.Tool => "Service",
        ItemKind.Equipment => "Service",
        _ => "Component"
    };

    public sealed class MdmItemLookupDto
    {
        public Guid Id { get; init; }
        public string? Code { get; init; }
        public string Name { get; init; } = null!;
        public string ItemType { get; init; } = null!;
        public string? GroupName { get; init; }
        public bool IsActive { get; init; }
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

            if (data.Length >= 12
                && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            {
                return "image/webp";
            }
        }

        return "application/octet-stream";
    }
}