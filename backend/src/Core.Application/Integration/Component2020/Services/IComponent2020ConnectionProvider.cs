using System;
using System.Threading;
using System.Threading.Tasks;
using MyIS.Core.Application.Integration.Component2020.Dto;

namespace MyIS.Core.Application.Integration.Component2020.Services;

public interface IComponent2020ConnectionProvider
{
    Task<Component2020ConnectionDto> GetConnectionAsync(Guid? connectionId = null, CancellationToken cancellationToken = default);
    Task SaveConnectionAsync(Component2020ConnectionDto connection, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(Component2020ConnectionDto connection, CancellationToken cancellationToken);
}
