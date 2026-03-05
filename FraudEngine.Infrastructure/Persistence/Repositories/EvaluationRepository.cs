using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

public class EvaluationRepository : IEvaluationRepository
{
    private readonly AppDbContext _context;

    public EvaluationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(FraudEvaluation evaluation, CancellationToken cancellationToken = default)
    {
        await _context.FraudEvaluations.AddAsync(evaluation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IEnumerable<FraudEvaluation> Items, int TotalCount)> GetPagedAsync(
        Decision? decision, int? minScore, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.FraudEvaluations
            .AsNoTracking()
            .AsQueryable();

        if (decision.HasValue)
            query = query.Where(e => e.Decision == decision.Value);

        if (minScore.HasValue)
            query = query.Where(e => e.RiskScore >= minScore.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.EvaluatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
