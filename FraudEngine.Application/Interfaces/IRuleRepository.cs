using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines methods for managing and retrieving fraud rule definitions.
/// </summary>
public interface IRuleRepository
{
    /// <summary>
    /// Retrieves all mapped rule definitions asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of rule definitions.</returns>
    public Task<IEnumerable<RuleDefinition>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all currently active rule definitions asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active rule definitions.</returns>
    public Task<IEnumerable<RuleDefinition>> GetActiveRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific rule definition by identifier.
    /// </summary>
    /// <param name="id">The unique rule identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The specified rule if found, otherwise null.</returns>
    public Task<RuleDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing rule definition.
    /// </summary>
    /// <param name="rule">The modified rule definition.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    public Task UpdateAsync(RuleDefinition rule, CancellationToken cancellationToken = default);
}
