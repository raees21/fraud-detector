using System.Threading;
using System.Threading.Tasks;

namespace FraudEngine.Application.Interfaces;

public interface IVelocityService
{
    /// <summary>
    /// Increments the transaction count for an account in the last 60 seconds and returns the new count.
    /// </summary>
    Task<int> GetRecentTransactionCountAsync(string accountId, CancellationToken cancellationToken = default);
}
