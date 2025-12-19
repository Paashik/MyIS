using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyIS.Core.WebApi.Contracts.Admin;

namespace MyIS.Core.WebApi.Controllers.Admin;

[ApiController]
[Route("api/admin/settings/global-paths")]
[Authorize]
[Authorize(Policy = "Admin.Settings.Access")]
public sealed class AdminGlobalPathsController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AdminGlobalPathsController> _logger;

    public AdminGlobalPathsController(
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AdminGlobalPathsController> logger)
    {
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public ActionResult<GlobalPathsSettingsResponse> Get()
    {
        var settings = ReadFromConfiguration();
        return Ok(BuildResponse(settings));
    }

    [HttpPost]
    public ActionResult<GlobalPathsSettingsResponse> Update([FromBody] UpdateGlobalPathsSettingsRequest request)
    {
        var settings = new GlobalPathsSettingsDto
        {
            ProjectsRoot = (request.ProjectsRoot ?? string.Empty).Trim(),
            DocumentsRoot = (request.DocumentsRoot ?? string.Empty).Trim(),
            DatabasesRoot = (request.DatabasesRoot ?? string.Empty).Trim()
        };

        var response = BuildResponse(settings);

        if (request.CreateDirectories)
        {
            CreateDirectoriesIfNeeded(settings);
            response = BuildResponse(settings);
        }

        if (!IsValid(settings, out var error))
        {
            return BadRequest(error);
        }

        try
        {
            WriteLocalGlobalPaths(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write global paths to appsettings.Local.json");
            return StatusCode(500, "Failed to save settings on server.");
        }

        return Ok(response);
    }

    private GlobalPathsSettingsDto ReadFromConfiguration()
    {
        var section = _configuration.GetSection("GlobalPaths");
        return new GlobalPathsSettingsDto
        {
            ProjectsRoot = section["ProjectsRoot"] ?? string.Empty,
            DocumentsRoot = section["DocumentsRoot"] ?? string.Empty,
            DatabasesRoot = section["DatabasesRoot"] ?? string.Empty
        };
    }

    private static GlobalPathsSettingsResponse BuildResponse(GlobalPathsSettingsDto settings)
    {
        return new GlobalPathsSettingsResponse
        {
            Settings = settings,
            ProjectsRoot = CheckDirectory(settings.ProjectsRoot),
            DocumentsRoot = CheckDirectory(settings.DocumentsRoot),
            DatabasesRoot = CheckDirectory(settings.DatabasesRoot)
        };
    }

    private static bool IsValid(GlobalPathsSettingsDto settings, out string? error)
    {
        error = null;

        if (!IsValidPathOrEmpty(settings.ProjectsRoot, out var err1))
        {
            error = $"ProjectsRoot: {err1}";
            return false;
        }

        if (!IsValidPathOrEmpty(settings.DocumentsRoot, out var err2))
        {
            error = $"DocumentsRoot: {err2}";
            return false;
        }

        if (!IsValidPathOrEmpty(settings.DatabasesRoot, out var err3))
        {
            error = $"DatabasesRoot: {err3}";
            return false;
        }

        return true;
    }

    private static bool IsValidPathOrEmpty(string value, out string? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(value)) return true;

        try
        {
            _ = Path.GetFullPath(value);
            return true;
        }
        catch (Exception ex)
        {
            error = $"{ex.GetType().Name}: {ex.Message}";
            return false;
        }
    }

    private static void CreateDirectoriesIfNeeded(GlobalPathsSettingsDto settings)
    {
        EnsureDirectory(settings.ProjectsRoot);
        EnsureDirectory(settings.DocumentsRoot);
        EnsureDirectory(settings.DatabasesRoot);
    }

    private static void EnsureDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        Directory.CreateDirectory(path);
    }

    private void WriteLocalGlobalPaths(GlobalPathsSettingsDto settings)
    {
        var contentRoot = _environment.ContentRootPath;
        var path = Path.Combine(contentRoot, "appsettings.Local.json");

        JsonNode rootNode;

        if (System.IO.File.Exists(path))
        {
            using var stream = System.IO.File.OpenRead(path);
            rootNode = JsonNode.Parse(stream) ?? new JsonObject();
        }
        else
        {
            rootNode = new JsonObject();
        }

        if (rootNode is not JsonObject rootObject)
        {
            rootObject = new JsonObject();
        }

        var globalPaths = new JsonObject
        {
            ["ProjectsRoot"] = settings.ProjectsRoot,
            ["DocumentsRoot"] = settings.DocumentsRoot,
            ["DatabasesRoot"] = settings.DatabasesRoot
        };

        rootObject["GlobalPaths"] = globalPaths;

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        using var writeStream = System.IO.File.Create(path);
        JsonSerializer.Serialize(writeStream, rootObject, options);
    }

    private static GlobalPathCheckDto CheckDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new GlobalPathCheckDto { IsSet = false, Exists = false, CanWrite = false };
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            var exists = Directory.Exists(fullPath);
            var canWrite = exists && CanWriteToDirectory(fullPath);

            return new GlobalPathCheckDto
            {
                IsSet = true,
                Exists = exists,
                CanWrite = canWrite,
                Error = exists ? null : "Directory does not exist."
            };
        }
        catch (Exception ex)
        {
            return new GlobalPathCheckDto
            {
                IsSet = true,
                Exists = false,
                CanWrite = false,
                Error = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    private static bool CanWriteToDirectory(string directoryPath)
    {
        try
        {
            var filePath = Path.Combine(directoryPath, $".myis_write_test_{Guid.NewGuid():N}.tmp");
            using (System.IO.File.Create(filePath, 1, FileOptions.DeleteOnClose))
            {
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}

