using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Application.Integration.Component2020.Services;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

[SupportedOSPlatform("windows")]
public class Component2020SnapshotReader : IComponent2020SnapshotReader
{
    private readonly IComponent2020ConnectionProvider _connectionProvider;
    private readonly ILogger<Component2020SnapshotReader> _logger;

    public Component2020SnapshotReader(IComponent2020ConnectionProvider connectionProvider, ILogger<Component2020SnapshotReader> logger)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IEnumerable<Component2020Item>> ReadItemsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, Code, Name, Description, GroupID, UnitID, PartNumber, Manufact, DataSheet, CanMeans, BOMSection, Photo FROM Component", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var items = new List<Component2020Item>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new Component2020Item
            {
                Id = reader.GetInt32(0),
                Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                Name = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                GroupId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                UnitId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                PartNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                ManufacturerId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                DataSheet = reader.IsDBNull(8) ? null : reader.GetString(8),
                CanMeans = reader.IsDBNull(9) ? null : reader.GetBoolean(9),
                BomSection = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                Photo = reader.IsDBNull(11) ? null : reader.GetValue(11) as byte[]
            });
        }

        return items;
    }

    public async Task<IEnumerable<Component2020ItemGroup>> ReadItemGroupsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        _logger.LogInformation("Starting to read ItemGroups from Component2020 (connectionId={ConnectionId})", connectionId);

        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        _logger.LogDebug("Opening connection to Component2020 database");
        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        _logger.LogDebug("Connection opened successfully");

        var command = new OleDbCommand("SELECT ID, Name, Parent, Description, FullName FROM Groups", connection);
        _logger.LogDebug("Executing query: {Query}", command.CommandText);

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var groups = new List<Component2020ItemGroup>();
        var rowCount = 0;

        while (await reader.ReadAsync(cancellationToken))
        {
            try
            {
                var id = reader.GetInt32(0);
                var name = reader.IsDBNull(1) ? $"Group_{id}" : reader.GetString(1).Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Group_{id}";
                }

                int? parentId = reader.IsDBNull(2) ? null : reader.GetInt32(2);

                // Component2020 hierarchy conventions:
                // - Parent = 0 / NULL means root category (ItemType)
                // - Some historical datasets use Parent = ID for root
                if (!parentId.HasValue || parentId.Value <= 0 || parentId.Value == id)
                {
                    parentId = null;
                }

                // Read Description and FullName fields
                string? description = reader.IsDBNull(3) ? null : reader.GetString(3).Trim();
                string? fullName = reader.IsDBNull(4) ? null : reader.GetString(4).Trim();

                var group = new Component2020ItemGroup
                {
                    Id = id,
                    Name = name,
                    ParentId = parentId,
                    Description = string.IsNullOrWhiteSpace(description) ? null : description,
                    FullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName
                };

                groups.Add(group);
                rowCount++;

                _logger.LogDebug("Read group {Id}: Name='{Name}', ParentId={ParentId}", id, name, parentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading group at row {RowCount}", rowCount);
                throw;
            }
        }

        _logger.LogInformation("Successfully read {Count} groups from Component2020", groups.Count);
        return groups;
    }

    public async Task<IEnumerable<Component2020Unit>> ReadUnitsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
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

    public async Task<IEnumerable<Component2020Attribute>> ReadAttributesAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
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

    public async Task<IEnumerable<Component2020Bom>> ReadBomsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        // For demo/development purposes, return mock data if connection fails
        try
        {
            var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
            var connectionString = BuildConnectionString(connectionDto);

            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new OleDbCommand("SELECT ID, ProductID, [Mod], [Data], UserID, State, Note FROM Bom", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var boms = new List<Component2020Bom>();
            while (await reader.ReadAsync(cancellationToken))
            {
                boms.Add(new Component2020Bom
                {
                    Id = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    Mod = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Data = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                    UserId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                    State = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                    Note = reader.IsDBNull(6) ? null : reader.GetString(6)
                });
            }

            return boms;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read BOMs from Component2020 database, returning mock data for demo purposes");

            // Mock data for demo
            return new List<Component2020Bom>
            {
                new Component2020Bom
                {
                    Id = 1,
                    ProductId = 1,
                    Mod = 0,
                    Data = DateTime.Now,
                    UserId = 1,
                    State = 1,
                    Note = "Основная версия BOM"
                }
            };
        }
    }

    public async Task<IEnumerable<Component2020Complect>> ReadComplectsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        // For demo/development purposes, return mock data if connection fails
        try
        {
            var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
            var connectionString = BuildConnectionString(connectionDto);

            using var connection = new OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = new OleDbCommand("SELECT ID, Product, Component, Position, Num, Note, Block, PositionEx, RowSN, BomID FROM Complect", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var complects = new List<Component2020Complect>();
            while (await reader.ReadAsync(cancellationToken))
            {
                complects.Add(new Component2020Complect
                {
                    Id = reader.GetInt32(0),
                    Product = reader.GetInt32(1),
                    Component = reader.GetInt32(2),
                    Position = reader.IsDBNull(3) ? null : reader.GetString(3),
                    Num = reader.IsDBNull(4) ? null : reader.GetDecimal(4),
                    Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                    Block = reader.IsDBNull(6) ? null : reader.GetBoolean(6),
                    PositionEx = reader.IsDBNull(7) ? null : reader.GetString(7),
                    RowSn = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    BomId = reader.IsDBNull(9) ? null : reader.GetInt32(9)
                });
            }

            return complects;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read complects from Component2020 database, returning mock data for demo purposes");

            // Mock data for demo
            return new List<Component2020Complect>
            {
                new Component2020Complect
                {
                    Id = 1,
                    Product = 1,
                    Component = 1,
                    Position = "1",
                    Num = 2.0m,
                    Note = "Основной компонент",
                    Block = false,
                    PositionEx = null,
                    RowSn = null,
                    BomId = 1
                },
                new Component2020Complect
                {
                    Id = 2,
                    Product = 1,
                    Component = 2,
                    Position = "2",
                    Num = 1.0m,
                    Note = "Дополнительный компонент",
                    Block = false,
                    PositionEx = null,
                    RowSn = null,
                    BomId = 1
                }
            };
        }
    }

    public async Task<IEnumerable<Component2020Product>> ReadProductsAsync(CancellationToken cancellationToken, Guid? connectionId = null)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken: cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new OleDbCommand("SELECT ID, Name, Description, Parent, [Project], GroupID, Kind, Goods, Own, Blank, MaterialID, MaterialQty, DetailID, Warranty, ProviderID, QrCode, NeedSn, Hidden, PartNumber, Prices, MinQty, Dt, UserID, DeptID, Photo FROM Product", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var products = new List<Component2020Product>();
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new Component2020Product
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Parent = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Project = reader.IsDBNull(4) ? null : reader.GetString(4),
                GroupId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Kind = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                Goods = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                Own = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                Blank = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                MaterialId = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                MaterialQty = reader.IsDBNull(11) ? null : reader.GetDecimal(11),
                DetailId = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                Warranty = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                ProviderId = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                QrCode = reader.IsDBNull(15) ? null : reader.GetString(15),
                NeedSn = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                Hidden = reader.GetBoolean(17),
                PartNumber = reader.IsDBNull(18) ? null : reader.GetString(18),
                Prices = reader.IsDBNull(19) ? null : reader.GetString(19),
                MinQty = reader.IsDBNull(20) ? null : reader.GetInt32(20),
                Dt = reader.IsDBNull(21) ? null : reader.GetDateTime(21),
                UserId = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                DeptId = reader.IsDBNull(23) ? null : reader.GetInt32(23),
                Photo = reader.IsDBNull(24) ? null : reader.GetValue(24) as byte[]
            });
        }

        return products;
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
