using System.Threading;
using System.Threading.Tasks;

namespace MyIS.Core.Infrastructure.Data;

public interface IDbHealthService
{
    Task<DbConnectionStatus> CheckConnectionAsync(
        CancellationToken cancellationToken = default);
}