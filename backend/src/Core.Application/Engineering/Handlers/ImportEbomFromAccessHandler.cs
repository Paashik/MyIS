using MediatR;
using MyIS.Core.Application.Engineering.Abstractions;
using MyIS.Core.Application.Engineering.Commands;
using MyIS.Core.Application.Integration.Component2020.Services;
using MyIS.Core.Application.Integration.Component2020.Dto;
using MyIS.Core.Domain.Engineering.Entities;
using MyIS.Core.Application.Mdm.Abstractions;
using System.Data;
using System.Transactions;
using System.IO;
using Microsoft.Extensions.Logging;

namespace MyIS.Core.Application.Engineering.Handlers;

/// <summary>
/// Обработчик команды импорта EBOM из файла Access
/// </summary>
public class ImportEbomFromAccessHandler : IRequestHandler<ImportEbomFromAccessCommand, ImportEbomFromAccessResponse>
{
    private readonly ILogger<ImportEbomFromAccessHandler> _logger;
    private readonly IProductRepository _productRepository;
    private readonly IBomVersionRepository _bomVersionRepository;
    private readonly IBomLineRepository _bomLineRepository;
    private readonly IItemRepository _itemRepository;

    public ImportEbomFromAccessHandler(
        ILogger<ImportEbomFromAccessHandler> logger,
        IProductRepository productRepository,
        IBomVersionRepository bomVersionRepository,
        IBomLineRepository bomLineRepository,
        IItemRepository itemRepository)
    {
        _logger = logger;
        _productRepository = productRepository;
        _bomVersionRepository = bomVersionRepository;
        _bomLineRepository = bomLineRepository;
        _itemRepository = itemRepository;
    }

    public async Task<ImportEbomFromAccessResponse> Handle(ImportEbomFromAccessCommand request, CancellationToken cancellationToken)
    {
        // Валидация входных данных
        if (string.IsNullOrWhiteSpace(request.FilePath))
        {
            throw new ArgumentException("FilePath cannot be null or empty", nameof(request.FilePath));
        }

        if (!File.Exists(request.FilePath))
        {
            throw new FileNotFoundException("Access file not found", request.FilePath);
        }

        if (request.ProductId == Guid.Empty)
        {
            throw new ArgumentException("ProductId cannot be empty", nameof(request.ProductId));
        }

        // Проверяем существование продукта
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new KeyNotFoundException($"Product with ID {request.ProductId} not found");
        }



        try
        {
            // Прямое чтение из Access файла
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={request.FilePath};";
            using var connection = new System.Data.OleDb.OleDbConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Читаем Bom (версии) из Access файла
            var bomCommand = new System.Data.OleDb.OleDbCommand("SELECT ID, ProductID, [Mod], [Data], UserID, State, Note FROM Bom WHERE State = 2", connection); // Только действующие версии
            var bomDict = new Dictionary<int, dynamic>();
            using (var reader = await bomCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var bom = new
                    {
                        Id = reader.GetInt32(0),
                        ProductId = reader.GetInt32(1),
                        Mod = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                        Data = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3),
                        UserId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        State = reader.GetInt32(5),
                        Note = reader.IsDBNull(6) ? null : reader.GetString(6)
                    };
                    bomDict[bom.Id] = bom;
                }
            }

            // P1 Problem 3: Проверка существования версий и формирование versionCode
            var skippedBomsCount = 0;
            var skippedBoms = new List<SkippedBomInfo>();
            
            // Получаем Item для продукта, чтобы найти его ExternalId
            var productItem = await _itemRepository.FindByIdAsync(product.ItemId);
            if (productItem == null)
            {
                throw new InvalidOperationException($"Item {product.ItemId} not found for Product {request.ProductId}");
            }

            // Находим ExternalEntityLink для получения Access Product ID
            var productExternalLink = await _itemRepository.FindByExternalAsync("Component2020", productItem.Code);
            if (productExternalLink == null)
            {
                return new ImportEbomFromAccessResponse(
                    ImportedLinesCount: 0,
                    SkippedLinesCount: 0,
                    MissingItems: new List<MissingItemInfo>(),
                    SkippedBomsCount: 0,
                    SkippedBoms: new List<SkippedBomInfo>(),
                    Message: $"Product {product.Code} has no external link to Component2020");
            }

            // Парсим ExternalId как int для поиска в BOM
            if (!int.TryParse(productExternalLink.Code, out int productAccessId))
            {
                return new ImportEbomFromAccessResponse(
                    ImportedLinesCount: 0,
                    SkippedLinesCount: 0,
                    MissingItems: new List<MissingItemInfo>(),
                    SkippedBomsCount: 0,
                    SkippedBoms: new List<SkippedBomInfo>(),
                    Message: $"Invalid external ID format for Product {product.Code}: {productExternalLink.Code}");
            }

            // Находим BOM для текущего продукта
            var targetBom = bomDict.Values.FirstOrDefault(b => b.ProductId == productAccessId);
            if (targetBom == null)
            {
                return new ImportEbomFromAccessResponse(
                    ImportedLinesCount: 0,
                    SkippedLinesCount: 0,
                    MissingItems: new List<MissingItemInfo>(),
                    SkippedBomsCount: 0,
                    SkippedBoms: new List<SkippedBomInfo>(),
                    Message: $"No active BOM found for Product Access ID {productAccessId} in Access file");
            }

            // Формируем versionCode: если Bom.Mod есть → "v{Mod}", иначе → "v{Data:yyyyMMdd}"
            string versionCode;
            if (targetBom.Mod.HasValue)
            {
                versionCode = $"v{targetBom.Mod.Value}";
            }
            else if (targetBom.Data.HasValue)
            {
                versionCode = $"v{targetBom.Data.Value:yyyyMMdd}";
            }
            else
            {
                versionCode = "v1";
            }

            // Проверяем существование версии
            var existingVersion = await _bomVersionRepository.GetByProductIdAndVersionCodeAsync(
                request.ProductId, versionCode, cancellationToken);
            
            if (existingVersion != null)
            {
                _logger.LogWarning(
                    "BOM version {VersionCode} already exists for Product {ProductId}. Skipping import.",
                    versionCode, request.ProductId);
                
                skippedBomsCount++;
                skippedBoms.Add(new SkippedBomInfo(
                    BomId: targetBom.Id,
                    ProductId: targetBom.ProductId,
                    VersionCode: versionCode,
                    Reason: $"Version {versionCode} already exists"));

                return new ImportEbomFromAccessResponse(
                    ImportedLinesCount: 0,
                    SkippedLinesCount: 0,
                    MissingItems: new List<MissingItemInfo>(),
                    SkippedBomsCount: skippedBomsCount,
                    SkippedBoms: skippedBoms,
                    Message: $"BOM version {versionCode} already exists. Import skipped.");
            }

            // Сохраняем данные в транзакции
            using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            // Создаем BomVersion сущность
            var bomVersionEntity = new BomVersion(request.ProductId, versionCode, BomSource.Imported);
            bomVersionEntity.Update(versionCode, $"Imported from {Path.GetFileName(request.FilePath)}");

            await _bomVersionRepository.AddAsync(bomVersionEntity, cancellationToken);

            // Читаем complects из Access файла для активных Bom
            var bomIds = string.Join(",", bomDict.Keys);
            if (string.IsNullOrEmpty(bomIds))
            {
                return new ImportEbomFromAccessResponse(
                    ImportedLinesCount: 0,
                    SkippedLinesCount: 0,
                    MissingItems: new List<MissingItemInfo>(),
                    SkippedBomsCount: 0,
                    SkippedBoms: new List<SkippedBomInfo>(),
                    Message: "No active BOM versions found in Access file");
            }
            var complectCommand = new System.Data.OleDb.OleDbCommand($"SELECT ID, Product, Component, Position, Num, Note, Block, PositionEx, RowSN, BomID FROM Complect WHERE BomID IN ({bomIds})", connection);
            var complects = new List<dynamic>();
            using (var reader = await complectCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    complects.Add(new
                    {
                        Id = reader.GetInt32(0),
                        Product = reader.GetInt32(1),
                        Component = reader.GetInt32(2),
                        Position = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Num = reader.IsDBNull(4) ? (decimal?)null : reader.GetDecimal(4),
                        Note = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Block = reader.IsDBNull(6) ? (bool?)null : reader.GetBoolean(6),
                        PositionEx = reader.IsDBNull(7) ? null : reader.GetString(7),
                        RowSn = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                        BomId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9)
                    });
                }
            }

            // Читаем components
            var componentCommand = new System.Data.OleDb.OleDbCommand("SELECT ID, Code, Name, Description, GroupID, UnitID, PartNumber, Manufact, DataSheet, CanMeans, BOMSection, Photo FROM Component", connection);
            var componentDict = new Dictionary<int, dynamic>();
            using (var reader = await componentCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var component = new
                    {
                        Id = reader.GetInt32(0),
                        Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Name = reader.GetString(2),
                        Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                        GroupId = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4),
                        UnitId = reader.IsDBNull(5) ? (int?)null : reader.GetInt32(5),
                        PartNumber = reader.IsDBNull(6) ? null : reader.GetString(6),
                        ManufacturerId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                        DataSheet = reader.IsDBNull(8) ? null : reader.GetString(8),
                        CanMeans = reader.IsDBNull(9) ? (bool?)null : reader.GetBoolean(9),
                        BomSection = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10),
                        Photo = reader.IsDBNull(11) ? null : reader.GetValue(11)
                    };
                    componentDict[component.Id] = component;
                }
            }
            _logger.LogInformation("Read {Count} component records from Access", componentDict.Count);

            // Читаем units
            var unitCommand = new System.Data.OleDb.OleDbCommand("SELECT ID, Code, Name, Symbol FROM Unit", connection);
            var unitDict = new Dictionary<int, dynamic>();
            using (var reader = await unitCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var unit = new
                    {
                        Id = reader.GetInt32(0),
                        Code = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Name = reader.GetString(2),
                        Symbol = reader.GetString(3)
                    };
                    unitDict[unit.Id] = unit;
                }
            }
            _logger.LogInformation("Read {Count} unit records from Access", unitDict.Count);

            // Читаем products для получения item по external_id
            var accessProductCommand = new System.Data.OleDb.OleDbCommand("SELECT ID, Name, Description FROM Product", connection);
            var accessProductDict = new Dictionary<int, dynamic>();
            using (var reader = await accessProductCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var accessProduct = new
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                    };
                    accessProductDict[accessProduct.Id] = accessProduct;
                }
            }
            _logger.LogInformation("Read {Count} product records from Access", accessProductDict.Count);

            // P1 Problem 4: Читаем ProductStruct для импорта связей Product → Product
            var productStructCommand = new System.Data.OleDb.OleDbCommand("SELECT ParentID, ProductID, Qty FROM ProductStruct", connection);
            var productStructList = new List<dynamic>();
            using (var reader = await productStructCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    productStructList.Add(new
                    {
                        ParentId = reader.GetInt32(0),
                        ProductId = reader.GetInt32(1),
                        Qty = reader.GetInt32(2)
                    });
                }
            }
            _logger.LogInformation("Read {Count} ProductStruct records from Access", productStructList.Count);

            // P0 Fix: Батч-загрузка всех Component и Product Items
            var allComponentIds = complects.Select(c => ((int)c.Component).ToString()).Distinct().ToList();
            var allProductIds = complects.Select(c => ((int)c.Product).ToString()).Distinct().ToList();
            
            // P1 Problem 4: Добавляем Product IDs из ProductStruct
            var productStructParentIds = productStructList.Select(ps => ((int)ps.ParentId).ToString()).Distinct();
            var productStructChildIds = productStructList.Select(ps => ((int)ps.ProductId).ToString()).Distinct();
            allProductIds = allProductIds.Concat(productStructParentIds).Concat(productStructChildIds).Distinct().ToList();
            
            var allExternalIds = allComponentIds.Concat(allProductIds).Distinct().ToList();

            _logger.LogInformation("Batch loading {Count} items from database", allExternalIds.Count);
            var itemsByExternalId = await _itemRepository.FindByExternalBatchAsync("Component2020", allExternalIds, cancellationToken);
            _logger.LogInformation("Loaded {Count} items from database", itemsByExternalId.Count);

            // P0 Fix: Счётчики и список пропущенных строк
            var importedLinesCount = 0;
            var skippedLinesCount = 0;
            var missingItems = new List<MissingItemInfo>();

            foreach (var complect in complects)
            {
                if (!componentDict.TryGetValue(complect.Component, out dynamic comp))
                {
                    skippedLinesCount++;
                    int componentId = complect.Component;
                    _logger.LogWarning("Skipped line: Component ID {ComponentId} not found in Access Component table", componentId);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: componentId.ToString(),
                        Name: "Unknown",
                        Reason: "Component not found in Access Component table"));
                    continue;
                }

                // P0 Fix: Используем батч-загруженные items
                int compId = comp.Id;
                string compName = comp.Name;
                if (!itemsByExternalId.TryGetValue(compId.ToString(), out var item))
                {
                    skippedLinesCount++;
                    _logger.LogWarning("Skipped line: Component ID {ComponentId} (Name: {Name}) not found in MyIS Items",
                        compId, compName);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: compId.ToString(),
                        Name: compName,
                        Reason: "Component not imported to MyIS Items"));
                    continue;
                }

                // Определяем parent item: если Complect.Product == Bom.ProductID, то product.ItemId, иначе item для Complect.Product
                Guid parentItemId;
                dynamic bom = null;
                if (complect.BomId.HasValue && bomDict.TryGetValue(complect.BomId.Value, out bom))
                {
                    if (complect.Product == bom.ProductId)
                    {
                        parentItemId = product.ItemId;
                    }
                    else
                    {
                        // P0 Fix: Используем батч-загруженные items
                        int productId = complect.Product;
                        if (!itemsByExternalId.TryGetValue(productId.ToString(), out var parentItem))
                        {
                            skippedLinesCount++;
                            string productName = "Unknown";
                            if (accessProductDict.TryGetValue(productId, out dynamic accessProd))
                            {
                                productName = accessProd.Name;
                            }
                            _logger.LogWarning("Skipped line: Parent Product ID {ProductId} (Name: {Name}) not found in MyIS Items",
                                productId, productName);
                            missingItems.Add(new MissingItemInfo(
                                AccessId: productId.ToString(),
                                Name: productName,
                                Reason: "Parent Product not imported to MyIS Items"));
                            continue;
                        }
                        parentItemId = parentItem.Id;
                    }
                }
                else
                {
                    skippedLinesCount++;
                    int? bomId = complect.BomId;
                    int componentId = complect.Component;
                    string componentName = comp.Name;
                    _logger.LogWarning("Skipped line: BomID {BomId} not found for Component {ComponentId}",
                        bomId, componentId);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: componentId.ToString(),
                        Name: componentName,
                        Reason: $"BomID {bomId} not found"));
                    continue;
                }

                // Валидация данных
                decimal quantity = complect.Num ?? 1;
                if (quantity <= 0)
                {
                    skippedLinesCount++;
                    _logger.LogWarning("Skipped line: Invalid quantity {Quantity} for Component {ComponentId} (Name: {Name})",
                        quantity, compId, compName);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: compId.ToString(),
                        Name: compName,
                        Reason: $"Invalid quantity: {quantity}"));
                    continue;
                }

                // Получаем unit code
                string? unitOfMeasure = null;
                int? unitId = comp.UnitId;
                if (unitId.HasValue && unitDict.TryGetValue(unitId.Value, out dynamic unit))
                {
                    unitOfMeasure = unit.Code;
                }

                // Маппинг роли
                int? bomSection = comp.BomSection;
                var bomRole = bomSection switch
                {
                    0 or 1 => BomRole.Component,
                    3 => BomRole.Material,
                    _ => BomRole.Component
                };

                // Маппинг статуса
                var lineStatus = complect.Block == true ? LineStatus.Archived : LineStatus.Valid;

                // Создаем BomLine сущность
                var bomLineEntity = new BomLine(
                    bomVersionId: bomVersionEntity.Id,
                    parentItemId: parentItemId,
                    itemId: item.Id,
                    role: bomRole,
                    quantity: quantity,
                    unitOfMeasure: unitOfMeasure
                );

                bomLineEntity.Update(
                    positionNo: complect.Position,
                    notes: complect.Note
                );
                bomLineEntity.UpdateStatus(lineStatus);

                await _bomLineRepository.AddAsync(bomLineEntity, cancellationToken);
                importedLinesCount++;
            }

            // P1 Problem 4: Импорт ProductStruct как BomLine с Role = SubAssembly
            foreach (var productStruct in productStructList)
            {
                int parentId = productStruct.ParentId;
                int childId = productStruct.ProductId;
                int qty = productStruct.Qty;

                // Находим parent item
                if (!itemsByExternalId.TryGetValue(parentId.ToString(), out var parentItem))
                {
                    skippedLinesCount++;
                    string parentName = "Unknown";
                    if (accessProductDict.TryGetValue(parentId, out dynamic accessProd))
                    {
                        parentName = accessProd.Name;
                    }
                    _logger.LogWarning(
                        "Skipped ProductStruct: Parent Product ID {ParentId} (Name: {Name}) not found in MyIS Items",
                        parentId, parentName);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: parentId.ToString(),
                        Name: parentName,
                        Reason: "Parent Product not imported to MyIS Items (ProductStruct)"));
                    continue;
                }

                // Находим child item
                if (!itemsByExternalId.TryGetValue(childId.ToString(), out var childItem))
                {
                    skippedLinesCount++;
                    string childName = "Unknown";
                    if (accessProductDict.TryGetValue(childId, out dynamic accessProd))
                    {
                        childName = accessProd.Name;
                    }
                    _logger.LogWarning(
                        "Skipped ProductStruct: Child Product ID {ChildId} (Name: {Name}) not found in MyIS Items",
                        childId, childName);
                    missingItems.Add(new MissingItemInfo(
                        AccessId: childId.ToString(),
                        Name: childName,
                        Reason: "Child Product not imported to MyIS Items (ProductStruct)"));
                    continue;
                }

                // Валидация количества
                if (qty <= 0)
                {
                    skippedLinesCount++;
                    _logger.LogWarning(
                        "Skipped ProductStruct: Invalid quantity {Qty} for Parent {ParentId} → Child {ChildId}",
                        qty, parentId, childId);
                    continue;
                }

                // Создаем BomLine с Role = SubAssembly
                var subAssemblyLine = new BomLine(
                    bomVersionId: bomVersionEntity.Id,
                    parentItemId: parentItem.Id,
                    itemId: childItem.Id,
                    role: BomRole.SubAssembly,
                    quantity: qty,
                    unitOfMeasure: "шт" // По умолчанию для изделий
                );

                subAssemblyLine.Update(
                    positionNo: null,
                    notes: "Imported from ProductStruct"
                );

                await _bomLineRepository.AddAsync(subAssemblyLine, cancellationToken);
                importedLinesCount++;
            }

            transaction.Complete();

            // P0 Fix: Возвращаем детальную статистику
            _logger.LogInformation(
                "Import completed: {Imported} lines imported, {Skipped} lines skipped, {Missing} missing items, {SkippedBoms} skipped BOMs",
                importedLinesCount, skippedLinesCount, missingItems.Count, skippedBomsCount);

            return new ImportEbomFromAccessResponse(
                ImportedLinesCount: importedLinesCount,
                SkippedLinesCount: skippedLinesCount,
                MissingItems: missingItems,
                SkippedBomsCount: skippedBomsCount,
                SkippedBoms: skippedBoms,
                Message: $"Successfully imported {importedLinesCount} BOM lines from {Path.GetFileName(request.FilePath)}. Skipped {skippedLinesCount} lines.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error importing EBOM from {request.FilePath}: {ex.Message}", ex);
        }
    }
}