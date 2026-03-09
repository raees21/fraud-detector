using FraudEngine.Application.IntegrationEvents;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines the asynchronous processor for submitted transaction events.
/// </summary>
public interface ITransactionEventProcessor
{
    /// <summary>
    /// Processes a submitted transaction event.
    /// </summary>
    public Task ProcessAsync(TransactionSubmittedIntegrationEvent integrationEvent, string topic,
        CancellationToken cancellationToken = default);
}
