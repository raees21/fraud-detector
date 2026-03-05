using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

public interface IRuleRepository
{
    Task<IEnumerable<RuleDefinition>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<RuleDefinition>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<RuleDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task UpdateAsync(RuleDefinition rule, CancellationToken cancellationToken = default);
}
