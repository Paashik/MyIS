using System;
using System.Data.OleDb;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.EntityFrameworkCore;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Infrastructure.Data;
using MyIS.Core.Infrastructure.Data.Entities.Integration;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

[SupportedOSPlatform("windows")]
public class Component2020ConnectionProvider : IComponent2020ConnectionProvider
{
    private readonly AppDbContext _dbContext;

    public Component2020ConnectionProvider(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Component2020ConnectionDto> GetConnectionAsync(Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        Component2020Connection? connection = null;

        if (connectionId.HasValue && connectionId.Value != Guid.Empty)
        {
            connection = await _dbContext.Component2020Connections
                .FirstOrDefaultAsync(c => c.Id == connectionId.Value, cancellationToken);
        }

        connection ??= await _dbContext.Component2020Connections
            .Where(c => c.IsActive)
            .OrderByDescending(c => c.UpdatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (connection == null)
        {
            return new Component2020ConnectionDto(); // empty
        }

        return new Component2020ConnectionDto
        {
            MdbPath = connection.MdbPath ?? string.Empty,
            Login = connection.Login,
            Password = connection.EncryptedPassword,
            IsActive = connection.IsActive
        };
    }

    public async Task SaveConnectionAsync(Component2020ConnectionDto connection, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.Component2020Connections.FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
        {
            var password = ResolvePassword(existing.EncryptedPassword, connection);
            existing.Update(connection.MdbPath, connection.Login, password, connection.IsActive);
        }
        else
        {
            var newConnection = new Component2020Connection();
            var password = ResolvePassword(null, connection);
            newConnection.Update(connection.MdbPath, connection.Login, password, connection.IsActive);
            _dbContext.Component2020Connections.Add(newConnection);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TestConnectionAsync(Component2020ConnectionDto connection, CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = BuildConnectionString(connection);
            using var oleDbConnection = new OleDbConnection(connectionString);
            await oleDbConnection.OpenAsync(cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildConnectionString(Component2020ConnectionDto connection)
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

        return builder.ConnectionString;
    }

    private static string? ResolvePassword(string? existingPassword, Component2020ConnectionDto connection)
    {
        if (connection.ClearPassword)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(connection.Password))
        {
            return connection.Password;
        }

        return existingPassword;
    }
}
