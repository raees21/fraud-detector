using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines methods for interacting with fraud evaluation records in the data store.
/// </summary>
public interface IEvaluationRepository
{
    /// <summary>
    /// Adds a new evaluation record to the repository asynchronously.
    /// </summary>
    /// <param name="evaluation">The fraud evaluation to add.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddAsync(FraudEvaluation evaluation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of evaluations, optionally filtered by decision and minimum score.
    /// </summary>
    /// <param name="decision">Optional filter by decision outcome.</param>
    /// <param name="minScore">Optional lower bound for the risk score.</param>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that yields a tuple containing the retrieved items and the total count.</returns>
    public Task<(IEnumerable<FraudEvaluation> Items, int TotalCount)> GetPagedAsync(
        Decision? decision, int? minScore, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts recent blocked evaluations for a specific account.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="since">The lower bound for recent blocked attempts.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The number of blocked evaluations for the account since the provided time.</returns>
    public Task<int> CountRecentBlockedAttemptsAsync(string accountId, DateTimeOffset since,
        CancellationToken cancellationToken = default);
}
