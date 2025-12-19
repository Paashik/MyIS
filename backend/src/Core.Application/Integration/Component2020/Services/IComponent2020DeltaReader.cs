using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020DeltaReader
{
    Task<IEnumerable<Component2020Unit>> ReadUnitsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Supplier>> ReadSuppliersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Item>> ReadItemsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Product>> ReadProductsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Manufacturer>> ReadManufacturersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020BodyType>> ReadBodyTypesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Currency>> ReadCurrenciesDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020TechnicalParameter>> ReadTechnicalParametersDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020ParameterSet>> ReadParameterSetsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
    Task<IEnumerable<Component2020Symbol>> ReadSymbolsDeltaAsync(Guid connectionId, string? lastProcessedKey, CancellationToken cancellationToken);
}