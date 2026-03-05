namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines methods for measuring and retrieving transaction velocity metrics.
/// </summary>
public interface IVelocityService
{
    /// <summary>
    ///     Increments the transaction count for an account in the last 60 seconds and returns the new count.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The transaction count for the specified account in the trailing time window.</returns>
    public Task<int> GetRecentTransactionCountAsync(string accountId, CancellationToken cancellationToken = default);
}
