namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Represents a transport-delivered integration event awaiting acknowledgement.
/// </summary>
public sealed record ReceivedIntegrationEvent(
    Guid EventId,
    string Topic,
    string Key,
    string Payload,
    Func<CancellationToken, Task> AcknowledgeAsync
);
