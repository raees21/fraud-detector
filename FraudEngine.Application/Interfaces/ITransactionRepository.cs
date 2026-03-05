using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines methods for interacting with transaction records in the abstract data store.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Adds a new transaction asynchronously.
    /// </summary>
    /// <param name="transaction">The transaction to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a transaction by its unique identifier.
    /// </summary>
    /// <param name="id">The GUID identifier of the transaction.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task containing the transaction, or null if not found.</returns>
    public Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paginated list of transactions, optionally filtered by criteria.
    /// </summary>
    /// <param name="decision">Optional filter for the final evaluation decision string.</param>
    /// <param name="accountId">Optional filter for a specific account identifier.</param>
    /// <param name="from">Optional lower bound for the timestamp range.</param>
    /// <param name="to">Optional upper bound for the timestamp range.</param>
    /// <param name="page">Page index.</param>
    /// <param name="pageSize">Page size limit.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple of items and total matching count.</returns>
    public Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        string? decision, string? accountId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a recently duplicated transaction with similar attributes exists.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="merchantName">The merchant name.</param>
    /// <param name="since">The threshold time to check since.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if a duplicate is found, otherwise false.</returns>
    public Task<bool> ExistsRecentDuplicateAsync(string accountId, decimal amount, string merchantName,
        DateTimeOffset since, CancellationToken cancellationToken = default);
}
