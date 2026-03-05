using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

public interface ITransactionRepository
{
    Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
    Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        string? decision, string? accountId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsRecentDuplicateAsync(string accountId, decimal amount, string merchantName, DateTimeOffset since, CancellationToken cancellationToken = default);
}
