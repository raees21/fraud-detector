using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of <see cref="IRuleRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class RuleRepository : IRuleRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuleRepository"/> class.
    /// </summary>
    public RuleRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RuleDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .AsNoTracking()
            .OrderBy(r => r.RuleName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RuleDefinition>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RuleDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(RuleDefinition rule, CancellationToken cancellationToken = default)
    {
        _context.RuleDefinitions.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
