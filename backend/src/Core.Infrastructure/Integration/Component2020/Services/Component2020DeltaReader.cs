using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Application.Integration.Component2020.Services;

namespace MyIS.Core.Infrastructure.Integration.Component2020.Services;

public class Component2020DeltaReader : IComponent2020DeltaReader
{
    private readonly IComponent2020ConnectionProvider _connectionProvider;

    public Component2020DeltaReader(IComponent2020ConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider ?? throw new ArgumentNullException(nameof(connectionProvider));
    }

    public async Task<IEnumerable<Component2020Unit>> ReadUnitsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Symbol, Code FROM Unit {whereClause} ORDER BY ID", connection);
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

    public async Task<IEnumerable<Component2020Supplier>> ReadSuppliersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            $"SELECT ID, Name, FullName, INN, KPP, Email, Phone, City, Address, Site, [Login], [Password], Note, [Type] FROM Providers{whereClause} ORDER BY ID",
            connection);

        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var suppliers = new List<Component2020Supplier>();
        while (await reader.ReadAsync(cancellationToken))
        {
            suppliers.Add(new Component2020Supplier
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Inn = reader.IsDBNull(3) ? null : reader.GetString(3),
                Kpp = reader.IsDBNull(4) ? null : reader.GetString(4),
                Email = reader.IsDBNull(5) ? null : reader.GetString(5),
                Phone = reader.IsDBNull(6) ? null : reader.GetString(6),
                City = reader.IsDBNull(7) ? null : reader.GetString(7),
                Address = reader.IsDBNull(8) ? null : reader.GetString(8),
                Site = reader.IsDBNull(9) ? null : reader.GetString(9),
                SiteLogin = reader.IsDBNull(10) ? null : reader.GetString(10),
                SitePassword = reader.IsDBNull(11) ? null : reader.GetString(11),
                Note = reader.IsDBNull(12) ? null : reader.GetString(12),
                ProviderType = reader.IsDBNull(13) ? 0 : reader.GetInt32(13)
            });
        }

        return suppliers;
    }

    public async Task<IEnumerable<Component2020Item>> ReadItemsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            $"SELECT ID, Code, Name, Description, [Group], UnitID, PartNumber FROM Component{whereClause} ORDER BY ID",
            connection);
        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        Console.WriteLine($"Executing query: {command.CommandText}, lastProcessedKey={lastProcessedKey}");
        var items = new List<Component2020Item>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var item = new Component2020Item
            {
                Id = reader.GetInt32(0),
                Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                Name = reader.GetString(2),
                Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                GroupId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                UnitId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                PartNumber = reader.IsDBNull(6) ? null : reader.GetString(6)
            };
            Console.WriteLine($"Read Component Item: Id={item.Id}, Code={item.Code}, Name={item.Name}");
            items.Add(item);
        }

        Console.WriteLine($"Returning {items.Count} items from Component");
        return items;
    }

    public async Task<IEnumerable<Component2020Product>> ReadProductsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Description, Parent, Project, GroupID, Kind, Goods, Own, Blank, MaterialID, MaterialQty, DetailID, Warranty, ProviderID, QRCode, NeedSN, Hidden, PartNumber, Prices, MinQty, DT, UserID, DeptID FROM Product {whereClause} ORDER BY ID", connection);
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
                DeptId = reader.IsDBNull(23) ? null : reader.GetInt32(23)
            });
        }

        return products;
    }

    public async Task<IEnumerable<Component2020Manufacturer>> ReadManufacturersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, FullName, Site, Note FROM Manufact {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var manufacturers = new List<Component2020Manufacturer>();
        while (await reader.ReadAsync(cancellationToken))
        {
            manufacturers.Add(new Component2020Manufacturer
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                Site = reader.IsDBNull(3) ? null : reader.GetString(3),
                Note = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return manufacturers;
    }

    public async Task<IEnumerable<Component2020BodyType>> ReadBodyTypesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Description, Pins, SMT, Photo, FootPrintPath, FootprintRef, FootprintRef2, FootPrintRef3 FROM Body {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var bodyTypes = new List<Component2020BodyType>();
        while (await reader.ReadAsync(cancellationToken))
        {
            bodyTypes.Add(new Component2020BodyType
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? null : reader.GetString(2),
                Pins = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Smt = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                Photo = reader.IsDBNull(5) ? null : reader.GetString(5),
                FootPrintPath = reader.IsDBNull(6) ? null : reader.GetString(6),
                FootprintRef = reader.IsDBNull(7) ? null : reader.GetString(7),
                FootprintRef2 = reader.IsDBNull(8) ? null : reader.GetString(8),
                FootPrintRef3 = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return bodyTypes;
    }

    public async Task<IEnumerable<Component2020Currency>> ReadCurrenciesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Symbol, Code, Rate FROM Curr {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var currencies = new List<Component2020Currency>();
        while (await reader.ReadAsync(cancellationToken))
        {
            currencies.Add(new Component2020Currency
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Symbol = reader.IsDBNull(2) ? null : reader.GetString(2),
                Code = reader.IsDBNull(3) ? null : reader.GetString(3),
                Rate = reader.IsDBNull(4) ? null : reader.GetDecimal(4)
            });
        }

        return currencies;
    }

    public async Task<IEnumerable<Component2020TechnicalParameter>> ReadTechnicalParametersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Symbol, UnitID FROM NPar {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var technicalParameters = new List<Component2020TechnicalParameter>();
        while (await reader.ReadAsync(cancellationToken))
        {
            technicalParameters.Add(new Component2020TechnicalParameter
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Symbol = reader.IsDBNull(2) ? null : reader.GetString(2),
                UnitId = reader.IsDBNull(3) ? null : reader.GetInt32(3)
            });
        }

        return technicalParameters;
    }

    public async Task<IEnumerable<Component2020ParameterSet>> ReadParameterSetsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, P0ID, P1ID, P2ID, P3ID, P4ID, P5ID FROM SPar {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var parameterSets = new List<Component2020ParameterSet>();
        while (await reader.ReadAsync(cancellationToken))
        {
            parameterSets.Add(new Component2020ParameterSet
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                P0Id = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                P1Id = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                P2Id = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                P3Id = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                P4Id = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                P5Id = reader.IsDBNull(7) ? null : reader.GetInt32(7)
            });
        }

        return parameterSets;
    }

    public async Task<IEnumerable<Component2020Symbol>> ReadSymbolsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var whereClause = string.IsNullOrEmpty(lastProcessedKey) ? "" : $"WHERE ID > {lastProcessedKey}";
        var command = new OleDbCommand($"SELECT ID, Name, Symbol, Photo, LibraryPath, LibraryRef FROM Symbol {whereClause} ORDER BY ID", connection);
        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var symbols = new List<Component2020Symbol>();
        while (await reader.ReadAsync(cancellationToken))
        {
            symbols.Add(new Component2020Symbol
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                SymbolValue = reader.IsDBNull(2) ? null : reader.GetString(2),
                Photo = reader.IsDBNull(3) ? null : reader.GetString(3),
                LibraryPath = reader.IsDBNull(4) ? null : reader.GetString(4),
                LibraryRef = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }

        return symbols;
    }

    public async Task<IEnumerable<Component2020Person>> ReadPersonsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            $"SELECT ID, LastName, FirstName, SecondName, Position, DeptID, Hidden, Email, Phone, Note FROM Person{whereClause} ORDER BY ID",
            connection);

        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var people = new List<Component2020Person>();
        while (await reader.ReadAsync(cancellationToken))
        {
            people.Add(new Component2020Person
            {
                Id = reader.GetInt32(0),
                LastName = reader.IsDBNull(1) ? null : reader.GetString(1),
                FirstName = reader.IsDBNull(2) ? null : reader.GetString(2),
                SecondName = reader.IsDBNull(3) ? null : reader.GetString(3),
                Position = reader.IsDBNull(4) ? null : reader.GetString(4),
                DeptId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Hidden = !reader.IsDBNull(6) && reader.GetBoolean(6),
                Email = reader.IsDBNull(7) ? null : reader.GetString(7),
                Phone = reader.IsDBNull(8) ? null : reader.GetString(8),
                Note = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return people;
    }

    public async Task<IEnumerable<Component2020User>> ReadUsersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            $"SELECT ID, Name, Password, Hidden, RoleID, PersonID, Roles, UI FROM Users{whereClause} ORDER BY ID",
            connection);

        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var users = new List<Component2020User>();
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new Component2020User
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Password = reader.IsDBNull(2) ? null : reader.GetString(2),
                Hidden = !reader.IsDBNull(3) && reader.GetBoolean(3),
                RoleId = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                PersonId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Roles = reader.IsDBNull(6) ? null : reader.GetString(6),
                Ui = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return users;
    }

    public async Task<IEnumerable<Component2020Role>> ReadRolesAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var roles = new List<Component2020Role>();
        try
        {
            var command = new OleDbCommand("SELECT ID, Name, Code FROM Roles ORDER BY ID", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(new Component2020Role
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Code = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
        }
        catch (OleDbException)
        {
            var command = new OleDbCommand("SELECT ID, Name FROM Roles ORDER BY ID", connection);
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                roles.Add(new Component2020Role
                {
                    Id = reader.GetInt32(0),
                    Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1)
                });
            }
        }

        return roles;
    }

    public async Task<IEnumerable<Component2020CustomerOrder>> ReadCustomerOrdersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            "SELECT ID, [Number], [Data], [DeliveryData], State, CustomerID, Note, Contract, StoreID, PersonID, [Path], DatePay, DateFinished, ContactID, Discount, Tax, Mark, PN, PaymentForm, PayMethod, PayPeriod, Prepayment, Kind, AccountID " +
            $"FROM CustomerOrder{whereClause} ORDER BY ID",
            connection);

        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var orders = new List<Component2020CustomerOrder>();
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(new Component2020CustomerOrder
            {
                Id = reader.GetInt32(0),
                Number = reader.IsDBNull(1) ? null : reader.GetString(1),
                OrderDate = reader.IsDBNull(2) ? null : reader.GetDateTime(2),
                DeliveryDate = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                State = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                CustomerId = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Note = reader.IsDBNull(6) ? null : reader.GetString(6),
                Contract = reader.IsDBNull(7) ? null : reader.GetString(7),
                StoreId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                PersonId = reader.IsDBNull(9) ? null : reader.GetInt32(9),
                Path = reader.IsDBNull(10) ? null : reader.GetString(10),
                PayDate = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                FinishedDate = reader.IsDBNull(12) ? null : reader.GetDateTime(12),
                ContactId = reader.IsDBNull(13) ? null : reader.GetInt32(13),
                Discount = reader.IsDBNull(14) ? null : reader.GetInt32(14),
                Tax = reader.IsDBNull(15) ? null : reader.GetInt32(15),
                Mark = reader.IsDBNull(16) ? null : reader.GetInt32(16),
                Pn = reader.IsDBNull(17) ? null : reader.GetInt32(17),
                PaymentForm = reader.IsDBNull(18) ? null : reader.GetInt32(18),
                PayMethod = reader.IsDBNull(19) ? null : reader.GetInt32(19),
                PayPeriod = reader.IsDBNull(20) ? null : reader.GetInt32(20),
                Prepayment = reader.IsDBNull(21) ? null : reader.GetInt32(21),
                Kind = reader.IsDBNull(22) ? null : reader.GetInt32(22),
                AccountId = reader.IsDBNull(23) ? null : reader.GetInt32(23)
            });
        }

        return orders;
    }

    public async Task<IEnumerable<Component2020Status>> ReadStatusesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken)
    {
        var connectionDto = await _connectionProvider.GetConnectionAsync(connectionId, cancellationToken);
        var connectionString = BuildConnectionString(connectionDto);

        using var connection = new OleDbConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasLast = int.TryParse(lastProcessedKey, out var lastId);
        var whereClause = hasLast ? " WHERE ID > ?" : string.Empty;

        var command = new OleDbCommand(
            $"SELECT ID, Name, Color, Kind, Code, SN, Flags FROM Status{whereClause} ORDER BY ID",
            connection);

        if (hasLast)
        {
            command.Parameters.AddWithValue("@p1", lastId);
        }

        using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var statuses = new List<Component2020Status>();
        while (await reader.ReadAsync(cancellationToken))
        {
            statuses.Add(new Component2020Status
            {
                Id = reader.GetInt32(0),
                Name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                Color = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Kind = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                Code = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                SortOrder = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                Flags = reader.IsDBNull(6) ? null : reader.GetInt32(6)
            });
        }

        return statuses;
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
