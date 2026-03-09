namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines a transport for publishing integration events.
/// </summary>
public interface IIntegrationEventProducer
{
    /// <summary>
    /// Publishes a serialized integration event.
    /// </summary>
    public Task PublishAsync(string topic, string key, string payload, CancellationToken cancellationToken = default);
}
