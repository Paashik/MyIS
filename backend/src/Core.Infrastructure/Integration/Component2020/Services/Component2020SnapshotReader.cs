using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Application.Integration.Component2020.Services;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public class Component2020SnapshotReader : IComponent2020SnapshotReader
{
    private readonly IComponent2020ConnectionProvider _connectionProvider;

    public Component2020SnapshotReader(IComponent2020ConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
    }

    public async Task<IEnumerable<Component2020Item>> ReadItemsAsync(CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, GroupID, Name, Description, UnitID FROM Component", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<Component2020Item>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Component2020Item
            {
                Code = reader.GetInt32(0).ToString(), // assuming ID is code
                Name = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                GroupId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                UnitId = reader.IsDBNull(4) ? null : reader.GetInt32(4)
            });
        }

        return items;
    }

    public async Task<IEnumerable<Component2020ItemGroup>> ReadItemGroupsAsync(CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, Name, Parent FROM Groups", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var groups = new List<Component2020ItemGroup>();
        while (await reader.ReadAsync(cancellationToken))
        {
            groups.Add(new Component2020ItemGroup
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                ParentId = reader.IsDBNull(2) ? null : reader.GetInt32(2)
            });
        }

        return groups;
    }

    public async Task<IEnumerable<Component2020Unit>> ReadUnitsAsync(CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, Name, Symbol, Code FROM Unit", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var units = new List<Component2020Unit>();
        while (await reader.ReadAsync(cancellationToken))
        {
            units.Add(new Component2020Unit
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Symbol = reader.GetString(2),
                Code = reader.IsDBNull(3) ? null : reader.GetString(3)
            });
        }

        return units;
    }

    public async Task<IEnumerable<Component2020Attribute>> ReadAttributesAsync(CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, Name, Symbol FROM NPar", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var attributes = new List<Component2020Attribute>();
        while (await reader.ReadAsync(cancellationToken))
        {
            attributes.Add(new Component2020Attribute
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Symbol = reader.IsDBNull(2) ? null : reader.GetString(2)
            });
        }

        return attributes;
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
}
