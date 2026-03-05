using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

public class RuleRepository : IRuleRepository
{
    private readonly AppDbContext _context;

    public RuleRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RuleDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .AsNoTracking()
            .OrderBy(r => r.RuleName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<RuleDefinition>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<RuleDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RuleDefinitions
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(RuleDefinition rule, CancellationToken cancellationToken = default)
    {
        _context.RuleDefinitions.Update(rule);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
