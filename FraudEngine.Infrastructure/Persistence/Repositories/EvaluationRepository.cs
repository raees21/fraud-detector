using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of <see cref="IEvaluationRepository"/> using Entity Framework Core.
/// </summary>
public class EvaluationRepository : IEvaluationRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="EvaluationRepository"/> class.
    /// </summary>
    public EvaluationRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task AddAsync(FraudEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await _context.FraudEvaluations.AddAsync(evaluation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<FraudEvaluation> Items, int TotalCount)> GetPagedAsync(
        Decision? decision, int? minScore, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<FraudEvaluation> query = _context.FraudEvaluations
            .AsNoTracking()
            .AsQueryable();

        if (decision.HasValue)
            query = query.Where(e => e.Decision == decision.Value);

        if (minScore.HasValue)
            query = query.Where(e => e.RiskScore >= minScore.Value);

        int totalCount = await query.CountAsync(cancellationToken);

        List<FraudEvaluation> items = await query
            .OrderByDescending(e => e.EvaluatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
