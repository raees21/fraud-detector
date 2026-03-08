using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of <see cref="ITransactionRepository"/> using Entity Framework Core.
/// </summary>
public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository"/> class.
    /// </summary>
    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        string? decision, string? accountId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Transaction> query = _context.Transactions
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrEmpty(accountId))
            query = query.Where(t => t.AccountId == accountId);

        if (from.HasValue)
            query = query.Where(t => t.Timestamp >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Timestamp <= to.Value);

        if (!string.IsNullOrEmpty(decision))
        {
            string normalizedDecision = decision.Trim().ToUpperInvariant();
            // Join with evaluation to filter by decision
            query = query.Join(
                    _context.FraudEvaluations,
                    t => t.Id,
                    e => e.TransactionId,
                    (t, e) => new { Transaction = t, Evaluation = e }
                )
                .Where(x => x.Evaluation.Decision.ToString() == normalizedDecision)
                .Select(x => x.Transaction);
        }

        int totalCount = await query.CountAsync(cancellationToken);

        List<Transaction> items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsRecentDuplicateAsync(string accountId, decimal amount, string merchantName,
        DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .AnyAsync(t =>
                    t.AccountId == accountId &&
                    t.Amount == amount &&
                    t.MerchantName == merchantName &&
                    t.Timestamp >= since,
                cancellationToken);
    }
}
