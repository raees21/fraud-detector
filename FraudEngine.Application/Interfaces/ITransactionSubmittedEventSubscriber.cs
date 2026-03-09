namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines a durable subscription to transaction-submitted integration events.
/// </summary>
public interface ITransactionSubmittedEventSubscriber
{
    /// <summary>
    /// Reads submitted transaction events from the transport.
    /// </summary>
    public IAsyncEnumerable<ReceivedIntegrationEvent> ReadAsync(CancellationToken cancellationToken = default);
}
