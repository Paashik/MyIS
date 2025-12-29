using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyIS.Core.Application.Integration.Component2020.Commands;
using MyIS.Core.Application.Integration.Component2020.Handlers;
using MyIS.Core.Application.Integration.Component2020.Queries;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.WebApi.Dto.Integration;

namespace MyIS.Core.WebApi.Controllers;

[ApiController]
[Route("api/admin/integrations/component2020")]
[SupportedOSPlatform("windows")]
public class Component2020IntegrationController : ControllerBase
{
    private static Guid? TryGetUserId(ClaimsPrincipal user)
    {
        var candidate =
            user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub")
            ?? user.FindFirstValue("userId")
            ?? user.FindFirstValue("id");

        return Guid.TryParse(candidate, out var guid) ? guid : null;
    }

    [HttpGet("fs")]
    [Authorize(Policy = "Admin.Integration.View")]
    public IActionResult BrowseFs(
        [FromServices] IConfiguration configuration,
        [FromQuery] string? path = null)
    {
        var databasesRoot = configuration.GetSection("GlobalPaths")["DatabasesRoot"] ?? string.Empty;
        databasesRoot = databasesRoot.Trim();

        if (string.IsNullOrWhiteSpace(databasesRoot))
        {
            return Ok(new GetComponent2020FsEntriesResponse
            {
                DatabasesRoot = string.Empty,
                CurrentRelativePath = string.Empty,
                Entries = Array.Empty<Component2020FsEntryDto>()
            });
        }

        string fullRoot;
        try
        {
            fullRoot = Path.GetFullPath(databasesRoot);
        }
        catch
        {
            return BadRequest("GlobalPaths:DatabasesRoot is not a valid path.");
        }

        var requestedRelative = (path ?? string.Empty).Trim().TrimStart('\\', '/');
        var combined = Path.Combine(fullRoot, requestedRelative);

        string currentFull;
        try
        {
            currentFull = Path.GetFullPath(combined);
        }
        catch
        {
            return BadRequest("Invalid path.");
        }

        if (!currentFull.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Path is outside of DatabasesRoot.");
        }

        if (!Directory.Exists(currentFull))
        {
            return Ok(new GetComponent2020FsEntriesResponse
            {
                DatabasesRoot = fullRoot,
                CurrentRelativePath = requestedRelative,
                Entries = Array.Empty<Component2020FsEntryDto>()
            });
        }

        var entries = new List<Component2020FsEntryDto>();

        foreach (var entryPath in Directory.EnumerateFileSystemEntries(currentFull))
        {
            if (entries.Count >= 1000) break;

            try
            {
                var name = Path.GetFileName(entryPath);
                if (name.Equals("#recycle", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var isDir = Directory.Exists(entryPath);

                var rel = Path.GetRelativePath(fullRoot, entryPath);
                if (rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    .Any(seg => seg.Equals("#recycle", StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (isDir)
                {
                    var di = new DirectoryInfo(entryPath);
                    entries.Add(new Component2020FsEntryDto
                    {
                        Name = name,
                        RelativePath = rel,
                        FullPath = di.FullName,
                        IsDirectory = true,
                        SizeBytes = null,
                        LastWriteTimeUtc = di.Exists ? di.LastWriteTimeUtc : null
                    });
                }
                else
                {
                    var fi = new FileInfo(entryPath);
                    entries.Add(new Component2020FsEntryDto
                    {
                        Name = name,
                        RelativePath = rel,
                        FullPath = fi.FullName,
                        IsDirectory = false,
                        SizeBytes = fi.Exists ? fi.Length : null,
                        LastWriteTimeUtc = fi.Exists ? fi.LastWriteTimeUtc : null
                    });
                }
            }
            catch
            {
                // ignore unreadable entries
            }
        }

        var ordered = entries
            .OrderByDescending(e => e.IsDirectory)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new GetComponent2020FsEntriesResponse
        {
            DatabasesRoot = fullRoot,
            CurrentRelativePath = requestedRelative,
            Entries = ordered
        });
    }

    [HttpGet("mdb-files")]
    [Authorize(Policy = "Admin.Integration.View")]
    public IActionResult GetMdbFiles([FromServices] IConfiguration configuration)
    {
        var databasesRoot = configuration.GetSection("GlobalPaths")["DatabasesRoot"] ?? string.Empty;
        databasesRoot = databasesRoot.Trim();

        if (string.IsNullOrWhiteSpace(databasesRoot))
        {
            return Ok(new GetComponent2020MdbFilesResponse
            {
                DatabasesRoot = string.Empty,
                Files = Array.Empty<Component2020MdbFileDto>()
            });
        }

        string fullRoot;
        try
        {
            fullRoot = Path.GetFullPath(databasesRoot);
        }
        catch
        {
            return BadRequest("GlobalPaths:DatabasesRoot is not a valid path.");
        }

        if (!Directory.Exists(fullRoot))
        {
            return Ok(new GetComponent2020MdbFilesResponse
            {
                DatabasesRoot = fullRoot,
                Files = Array.Empty<Component2020MdbFileDto>()
            });
        }

        var files = new List<Component2020MdbFileDto>();

        void AddByPattern(string pattern)
        {
            foreach (var path in Directory.EnumerateFiles(fullRoot, pattern, SearchOption.AllDirectories))
            {
                if (files.Count >= 500) break;

                try
                {
                    var fi = new FileInfo(path);
                    var relative = Path.GetRelativePath(fullRoot, fi.FullName);

                    files.Add(new Component2020MdbFileDto
                    {
                        Name = fi.Name,
                        RelativePath = relative,
                        FullPath = fi.FullName,
                        SizeBytes = fi.Exists ? fi.Length : 0,
                        LastWriteTimeUtc = fi.Exists ? fi.LastWriteTimeUtc : DateTime.MinValue
                    });
                }
                catch
                {
                    // ignore unreadable files
                }
            }
        }

        AddByPattern("*.mdb");
        AddByPattern("*.accdb");

        var ordered = files
            .Where(f => !f.RelativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(seg => seg.Equals("#recycle", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Ok(new GetComponent2020MdbFilesResponse
        {
            DatabasesRoot = fullRoot,
            Files = ordered
        });
    }

    [HttpGet("status")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<Component2020StatusResponse> GetStatus(
        [FromServices] GetComponent2020SyncStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetComponent2020SyncStatusQuery();
        var response = await handler.Handle(query, cancellationToken);

        return new Component2020StatusResponse
        {
            IsConnected = response.IsConnected,
            ConnectionError = response.ConnectionError,
            IsSchedulerActive = response.IsSchedulerActive,
            LastSuccessfulSync = response.LastSuccessfulSync,
            LastSyncStatus = response.LastSyncStatus
        };
    }

    [HttpGet("connection")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetConnection(
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = await dbContext.Component2020Connections
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (connection == null)
        {
            return Ok(new Component2020ConnectionDto
            {
                Id = null,
                MdbPath = string.Empty,
                Login = null,
                IsActive = true,
                HasPassword = false,
                Password = null
            });
        }
        
        return Ok(new Component2020ConnectionDto
        {
            Id = connection.Id.ToString(),
            MdbPath = connection.MdbPath ?? string.Empty,
            Login = connection.Login,
            IsActive = connection.IsActive,
            HasPassword = !string.IsNullOrWhiteSpace(connection.EncryptedPassword),
            Password = null // Never return password
        });
    }

    [HttpGet("runs")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetRuns(
        [FromServices] GetComponent2020SyncRunsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] string? status = null)
    {
        var query = new GetComponent2020SyncRunsQuery
        {
            Page = page,
            PageSize = pageSize,
            FromDate = fromDate,
            Status = status
        };

        var response = await handler.Handle(query, cancellationToken);
        return Ok(response);
    }
    
    [HttpGet("runs/{runId}")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetRunById(
        Guid runId,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.Component2020SyncRuns
            .AsNoTracking()
            .Where(r => r.Id == runId)
            .Select(r => new
            {
                id = r.Id,
                scope = r.Scope,
                mode = r.Mode,
                status = r.Status,
                startedAt = r.StartedAt,
                finishedAt = r.FinishedAt,
                processedCount = r.ProcessedCount,
                errorCount = r.ErrorCount,
                countersJson = r.CountersJson,
                summary = r.Summary
            })
            .FirstOrDefaultAsync(cancellationToken);
        
        return run == null ? NotFound() : Ok(run);
    }

    [HttpPost("test-connection")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> TestConnection(
        [FromBody] Component2020ConnectionDto connection,
        [FromServices] IComponent2020ConnectionProvider connectionProvider,
        CancellationToken cancellationToken)
    {
        var isConnected = await connectionProvider.TestConnectionAsync(
            new MyIS.Core.Application.Integration.Component2020.Dto.Component2020ConnectionDto
            {
                MdbPath = connection.MdbPath,
                Password = connection.Password
            },
            cancellationToken);

        return Ok(new { IsConnected = isConnected });
    }

    [HttpPost("save-connection")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> SaveConnection(
        [FromBody] Component2020ConnectionDto connection,
        [FromServices] IComponent2020ConnectionProvider connectionProvider,
        CancellationToken cancellationToken)
    {
        await connectionProvider.SaveConnectionAsync(
            new MyIS.Core.Application.Integration.Component2020.Dto.Component2020ConnectionDto
            {
                MdbPath = connection.MdbPath,
                Login = connection.Login,
                Password = connection.Password,
                IsActive = connection.IsActive,
                ClearPassword = connection.ClearPassword
            },
            cancellationToken);

        return NoContent();
    }

    [HttpPost("run")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> RunSync(
        [FromBody] RunComponent2020SyncRequest request,
        [FromServices] RunComponent2020SyncHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Component2020SyncScope>(request.Scope, true, out var scope))
        {
            return BadRequest($"Invalid scope: '{request.Scope}'.");
        }

        var syncMode = Component2020SyncMode.Delta;
        if (!string.IsNullOrWhiteSpace(request.SyncMode)
            && !Enum.TryParse<Component2020SyncMode>(request.SyncMode, true, out syncMode))
        {
            return BadRequest($"Invalid syncMode: '{request.SyncMode}'.");
        }

        var command = new RunComponent2020SyncCommand
        {
            ConnectionId = request.ConnectionId,
            Scope = scope,
            DryRun = request.DryRun,
            SyncMode = syncMode,
            StartedByUserId = TryGetUserId(User)
        };

        var response = await handler.Handle(command, cancellationToken);
        return Ok(response);
    }

    [HttpPost("preview")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> PreviewImport(
        [FromBody] Component2020ImportPreviewRequest request,
        [FromServices] IComponent2020ImportPreviewService previewService,
        [FromServices] AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.ConnectionId))
        {
            return BadRequest("ConnectionId is required.");
        }

        if (!Guid.TryParse(request.ConnectionId, out var connectionId))
        {
            return BadRequest("ConnectionId is invalid.");
        }

        var connection = await dbContext.Component2020Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == connectionId, cancellationToken);

        if (connection == null || connection.IsActive == false)
        {
            return BadRequest("Component2020 connection is not active.");
        }

        var syncMode = Component2020SyncMode.Delta;
        if (!string.IsNullOrWhiteSpace(request.SyncMode)
            && !Enum.TryParse<Component2020SyncMode>(request.SyncMode, true, out syncMode))
        {
            return BadRequest($"Invalid syncMode: '{request.SyncMode}'.");
        }

        var preview = await previewService.PreviewAsync(
            new MyIS.Core.Application.Integration.Component2020.Dto.Component2020ImportPreviewRequestDto
            {
                ConnectionId = connectionId,
                SyncMode = syncMode,
                Page = request.Page.GetValueOrDefault(1),
                PageSize = request.PageSize.GetValueOrDefault(200)
            },
            cancellationToken);

        return Ok(preview);
    }

    [HttpPost("schedule")]
    [Authorize(Policy = "Admin.Integration.Execute")]
    public async Task<IActionResult> ScheduleSync(
        [FromBody] ScheduleComponent2020SyncRequest request,
        [FromServices] ScheduleComponent2020SyncHandler handler,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<Component2020SyncScope>(request.Scope, true, out var scope))
        {
            return BadRequest($"Invalid scope: '{request.Scope}'.");
        }

        var command = new ScheduleComponent2020SyncCommand
        {
            Scope = scope,
            DryRun = request.DryRun,
            CronExpression = request.CronExpression,
            IsActive = request.IsActive
        };

        var response = await handler.Handle(command, cancellationToken);
        return Ok(response);
    }

    [HttpGet("runs/{runId}/errors")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> GetRunErrors(
        Guid runId,
        [FromServices] GetComponent2020SyncRunErrorsHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetComponent2020SyncRunErrorsQuery
        {
            RunId = runId
        };

        var response = await handler.Handle(query, cancellationToken);
        return Ok(response);
    }

    [HttpGet("diagnostics/providers")]
    [Authorize(Policy = "Admin.Integration.View")]
    public async Task<IActionResult> DiagnosticsProviders(
        [FromServices] IComponent2020ConnectionProvider connectionProvider,
        [FromQuery] Guid? connectionId,
        CancellationToken cancellationToken)
    {
        var connection = await connectionProvider.GetConnectionAsync(connectionId, cancellationToken);

        if (string.IsNullOrWhiteSpace(connection.MdbPath))
        {
            return BadRequest(new { error = "Component2020 connection is not configured (mdbPath is empty)." });
        }

        try
        {
            var builder = new OleDbConnectionStringBuilder
            {
                Provider = "Microsoft.ACE.OLEDB.12.0",
                DataSource = connection.MdbPath
            };

            if (!string.IsNullOrEmpty(connection.Password))
            {
                builder["Jet OLEDB:Database Password"] = connection.Password;
            }

            using var oleDbConnection = new OleDbConnection(builder.ConnectionString);
            await oleDbConnection.OpenAsync(cancellationToken);

            using var cmd = new OleDbCommand("SELECT [Type], COUNT(*) AS Cnt FROM Providers GROUP BY [Type] ORDER BY [Type]", oleDbConnection);
            using var reader = await cmd.ExecuteReaderAsync(cancellationToken);

            var counts = new List<object>();
            var total = 0;

            while (await reader.ReadAsync(cancellationToken))
            {
                var type = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                var cnt = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
                total += cnt;
                counts.Add(new { type, count = cnt });
            }

            return Ok(new
            {
                mdbPath = connection.MdbPath,
                total,
                byType = counts
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                mdbPath = connection.MdbPath,
                error = ex.Message,
                details = ex.ToString()
            });
        }
    }
}
