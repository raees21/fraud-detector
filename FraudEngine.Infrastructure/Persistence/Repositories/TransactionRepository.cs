using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AppDbContext _context;

    public TransactionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<(IEnumerable<Transaction> Items, int TotalCount)> GetPagedAsync(
        string? decision, string? accountId, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Transactions
            .Include(t => _context.FraudEvaluations.FirstOrDefault(e => e.TransactionId == t.Id))
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
            // Join with evaluation to filter by decision
            query = query.Join(
                _context.FraudEvaluations,
                t => t.Id,
                e => e.TransactionId,
                (t, e) => new { Transaction = t, Evaluation = e }
            )
            .Where(x => x.Evaluation.Decision.ToString() == decision)
            .Select(x => x.Transaction);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsRecentDuplicateAsync(string accountId, decimal amount, string merchantName, DateTimeOffset since, CancellationToken cancellationToken = default)
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
