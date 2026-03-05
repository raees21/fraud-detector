using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Provides a service to evaluate transactions using the configured rules engine.
/// </summary>
public interface IRulesEngineService
{
    /// <summary>
    /// Asynchronously reloads the active rules into the engine cache or executing environment.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the reload operation.</returns>
    public Task ReloadRulesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Evaluates a specific transaction against the current set of rules.
    /// </summary>
    /// <param name="transaction">The transaction to evaluate.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A tuple containing the accumulated score, triggered rules JSON, and the resulting decision.</returns>
    public Task<(int TotalScore, string TriggeredRulesJson, Decision Decision)> EvaluateAsync(Transaction transaction,
        CancellationToken cancellationToken = default);
}
