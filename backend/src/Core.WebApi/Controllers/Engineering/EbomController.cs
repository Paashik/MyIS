using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Engineering.Commands;
using MyIS.Core.Application.Engineering.Queries;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.WebApi.Controllers.Engineering;

/// <summary>
/// Контроллер для работы с eBOM (Engineering Bill of Materials)
/// </summary>
[ApiController]
[Route("api/engineering/ebom")]
public class EbomController : ControllerBase
{
    private readonly ISender _sender;
    private readonly AppDbContext _dbContext;

    public EbomController(ISender sender, AppDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
    }

    [HttpGet("products")]
    [AllowAnonymous] // Временно для тестирования
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 200)
    {
        var query = new GetEbomProductsQuery(
            Search: search,
            Type: type,
            PageNumber: pageNumber,
            PageSize: pageSize);

        var response = await _sender.Send(query);

        var result = response.Products.Select(p => new
        {
            id = p.Id,
            code = p.Code,
            name = p.Name,
            description = p.Description,
            type = p.Type,
            itemId = p.ItemId,
            hasBomVersions = p.HasBomVersions,
            bomVersionsCount = p.BomVersionsCount,
            updatedAt = p.UpdatedAt
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("versions")]
    [AllowAnonymous] // Временно для тестирования
    public async Task<IActionResult> GetVersions([FromQuery] Guid itemId)
    {
        var query = new GetEbomVersionsQuery(itemId);
        var response = await _sender.Send(query);

        var result = response.Versions.Select(v => new
        {
            id = v.Id,
            itemId = v.ItemId,
            versionCode = v.VersionCode,
            status = v.Status,
            source = v.Source,
            updatedAt = v.UpdatedAt
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("versions/{bomVersionId:guid}")]
    public async Task<IActionResult> GetVersion(Guid bomVersionId)
    {
        var query = new GetEbomVersionQuery(bomVersionId);
        var response = await _sender.Send(query);

        return Ok(new
        {
            id = response.Version.Id,
            itemId = response.Version.ItemId,
            versionCode = response.Version.VersionCode,
            status = response.Version.Status,
            source = response.Version.Source,
            updatedAt = response.Version.UpdatedAt
        });
    }

    [HttpGet("{bomVersionId:guid}/tree")]
    public async Task<IActionResult> GetTree(Guid bomVersionId, [FromQuery] bool includeLeaves = false, [FromQuery] string? q = null)
    {
        var query = new GetEbomTreeQuery(bomVersionId, includeLeaves, q);
        var response = await _sender.Send(query);

        return Ok(new
        {
            rootItemId = response.Tree.RootItemId,
            nodes = response.Tree.Nodes.Select(n => new
            {
                itemId = n.ItemId,
                parentItemId = n.ParentItemId,
                code = n.Code,
                name = n.Name,
                itemType = n.ItemType,
                hasErrors = n.HasErrors
            }).ToArray()
        });
    }

    [HttpGet("{bomVersionId:guid}/lines")]
    public async Task<IActionResult> GetLines(Guid bomVersionId, [FromQuery] Guid parentItemId, [FromQuery] bool onlyErrors = false)
    {
        var query = new GetEbomLinesQuery(bomVersionId, parentItemId, onlyErrors);
        var response = await _sender.Send(query);

        var result = response.Lines.Select(l => new
        {
            id = l.Id,
            parentItemId = l.ParentItemId,
            itemId = l.ItemId,
            itemCode = l.ItemCode,
            itemName = l.ItemName,
            role = l.Role,
            qty = l.Qty,
            uomCode = l.UomCode,
            positionNo = l.PositionNo,
            notes = l.Notes,
            lineStatus = l.LineStatus
        }).ToArray();

        return Ok(result);
    }

    [HttpGet("{bomVersionId:guid}/explosion")]
    public async Task<IActionResult> GetExplosion(
        Guid bomVersionId,
        [FromQuery] int maxDepth = 64,
        [FromQuery] int maxRows = 20000)
    {
        var query = new GetEbomExplosionQuery(
            BomVersionId: bomVersionId,
            MaxDepth: maxDepth,
            MaxRows: maxRows);

        var response = await _sender.Send(query);

        return Ok(new
        {
            rootItemId = response.RootItemId,
            rows = response.Rows.Select(r => new
            {
                lineId = r.LineId,
                parentItemId = r.ParentItemId,
                itemId = r.ItemId,
                itemCode = r.ItemCode,
                itemName = r.ItemName,
                role = r.Role,
                qty = r.Qty,
                totalQty = r.TotalQty,
                uomCode = r.UomCode,
                positionNo = r.PositionNo,
                notes = r.Notes,
                lineStatus = r.LineStatus,
                level = r.Level,
                path = r.Path
            }).ToArray()
        });
    }

    [HttpPost("{bomVersionId:guid}/lines")]
    public async Task<IActionResult> CreateLine(Guid bomVersionId, [FromBody] CreateBomLineRequest request)
    {
        var command = new CreateEbomLineCommand(
            BomVersionId: bomVersionId,
            ParentItemId: request.ParentItemId,
            ItemId: request.ItemId,
            Role: request.Role,
            Qty: request.Qty,
            PositionNo: request.PositionNo,
            Notes: request.Notes);

        var response = await _sender.Send(command);

        return Created(string.Empty, new
        {
            id = response.Line.Id,
            parentItemId = response.Line.ParentItemId,
            itemId = response.Line.ItemId,
            itemCode = response.Line.ItemCode,
            itemName = response.Line.ItemName,
            role = response.Line.Role,
            qty = response.Line.Qty,
            uomCode = response.Line.UomCode,
            positionNo = response.Line.PositionNo,
            notes = response.Line.Notes,
            lineStatus = response.Line.LineStatus
        });
    }

    [HttpPut("lines/{lineId:guid}")]
    public async Task<IActionResult> UpdateLine(Guid lineId, [FromBody] UpdateBomLineRequest request)
    {
        var command = new UpdateEbomLineCommand(
            LineId: lineId,
            Role: request.Role,
            Qty: request.Qty,
            PositionNo: request.PositionNo,
            Notes: request.Notes,
            ItemId: request.ItemId);

        var response = await _sender.Send(command);

        return Ok(new
        {
            id = response.Line.Id,
            parentItemId = response.Line.ParentItemId,
            itemId = response.Line.ItemId,
            itemCode = response.Line.ItemCode,
            itemName = response.Line.ItemName,
            role = response.Line.Role,
            qty = response.Line.Qty,
            uomCode = response.Line.UomCode,
            positionNo = response.Line.PositionNo,
            notes = response.Line.Notes,
            lineStatus = response.Line.LineStatus
        });
    }

    [HttpDelete("lines/{lineId:guid}")]
    public async Task<IActionResult> DeleteLine(Guid lineId)
    {
        await _sender.Send(new DeleteEbomLineCommand(lineId));
        return NoContent();
    }

    [HttpPost("{bomVersionId}/validate")]
    public IActionResult Validate(string bomVersionId)
    {
        // TODO: Реализовать через MediatR
        return Ok(new[]
        {
            new
            {
                severity = "Warning",
                targetType = "Node",
                targetId = Guid.NewGuid(),
                message = "Узел имеет ошибки в дочерних элементах"
            }
        });
    }

    [HttpGet("{bomVersionId}/operations")]
    public IActionResult GetOperations(string bomVersionId)
    {
        // TODO: Реализовать через MediatR
        return Ok(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                code = "OP-001",
                name = "Сборка компонентов",
                areaName = "Цех сборки",
                durationMin = 30,
                status = "Active"
            }
        });
    }

    // [HttpPost("import-from-access")]
    // public async Task<IActionResult> ImportFromAccess([FromBody] ImportFromAccessRequest request)
    // {
    //     // Валидация входных данных
    //     if (request == null)
    //     {
    //         return BadRequest("Request body is required");
    //     }
    //
    //     if (string.IsNullOrWhiteSpace(request.FilePath))
    //     {
    //         return BadRequest("FilePath is required");
    //     }
    //
    //     if (request.ProductId == Guid.Empty)
    //     {
    //         return BadRequest("ProductId is required");
    //     }
    //
    //     var command = new MyIS.Core.Application.Engineering.Commands.ImportEbomFromAccessCommand(
    //         FilePath: request.FilePath,
    //         ProductId: request.ProductId);
    //
    //     var response = await _sender.Send(command);
    //
    //     var apiResponse = new ImportFromAccessResponse(
    //         ImportedLinesCount: response.ImportedLinesCount,
    //         Message: response.Message);
    //
    //     return Ok(apiResponse);
    // }

    [HttpPost("import-from-component2020")]
    // [Authorize] // TODO: Добавить подходящую политику - temporarily disabled for testing
    public async Task<IActionResult> ImportFromComponent2020([FromBody] ImportFromComponent2020Request request)
    {
        if (string.IsNullOrWhiteSpace(request.BomVersionId))
        {
            return BadRequest("BomVersionId is required");
        }

        if (!Guid.TryParse(request.BomVersionId, out _))
        {
            return BadRequest("Invalid BomVersionId format");
        }

        // ConnectionId is optional - will use active connection if not provided
        try
        {
            var syncMode = request.SyncMode switch
            {
                "SnapshotUpsert" => Component2020SyncMode.SnapshotUpsert,
                "Overwrite" => Component2020SyncMode.Overwrite,
                _ => Component2020SyncMode.Delta
            };

            Guid connectionId;
            if (!string.IsNullOrWhiteSpace(request.ConnectionId))
            {
                connectionId = Guid.Parse(request.ConnectionId);
            }
            else
            {
                // TODO: вынести получение активного подключения в Application (Query/Handler), чтобы WebApi не трогал EF.
                var connection = await _dbContext.Component2020Connections
                    .OrderByDescending(c => c.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (connection == null || !connection.IsActive)
                {
                    return BadRequest("No active Component2020 connection found. Please configure connection in Administration -> Integrations.");
                }

                connectionId = connection.Id;
            }

            var syncCommand = new RunComponent2020SyncCommand
            {
                ConnectionId = connectionId,
                Scope = Component2020SyncScope.Bom,
                DryRun = request.DryRun ?? false,
                SyncMode = syncMode,
                StartedByUserId = null // TODO: Получить из контекста пользователя
            };

            var response = await _sender.Send(syncCommand) as RunComponent2020SyncResponse;

            if (response == null)
            {
                return StatusCode(500, new ImportFromComponent2020Response(
                    Success: false,
                    Message: "Не удалось получить ответ от сервиса синхронизации",
                    ProcessedCount: 0
                ));
            }

            return Ok(new ImportFromComponent2020Response(
                Success: response.Status is "Success" or "Partial",
                Message: response.Status == "Success"
                    ? $"Импорт успешно завершен. Обработано: {response.ProcessedCount} записей"
                    : response.ErrorMessage ?? $"Импорт завершен со статусом: {response.Status}",
                ProcessedCount: response.ProcessedCount
            ));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ImportFromComponent2020Response(
                Success: false,
                Message: $"Ошибка импорта: {ex.Message}",
                ProcessedCount: 0
            ));
        }
    }

    [HttpGet("{bomVersionId}/history")]
    public IActionResult GetHistory(string bomVersionId)
    {
        // TODO: Реализовать через MediatR
        return Ok(new[]
        {
            new
            {
                id = Guid.NewGuid(),
                timestamp = DateTimeOffset.UtcNow,
                userName = "Иванов И.И.",
                action = "Создание версии",
                details = "Создана версия v1.0"
            }
        });
    }
}

// DTOs для запросов
public record ImportFromAccessRequest(
    string FilePath,
    Guid ProductId);

public record ImportFromAccessResponse(
    int ImportedLinesCount,
    string Message);

public record CreateBomLineRequest(
    Guid ParentItemId,
    Guid ItemId,
    string Role,
    decimal Qty,
    string? PositionNo = null,
    string? Notes = null
);

public record UpdateBomLineRequest(
    string? Role = null,
    decimal? Qty = null,
    string? PositionNo = null,
    string? Notes = null,
    Guid? ItemId = null
);

public record ImportFromComponent2020Request(
    string BomVersionId,
    string? ConnectionId = null,
    string? SyncMode = "Delta",
    bool? DryRun = false
);

public record ImportFromComponent2020Response(
    bool Success,
    string Message,
    int? ProcessedCount = null
);