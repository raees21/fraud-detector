namespace FraudEngine.Domain.Enums;

/// <summary>
/// Represents the publish state of a durable outbox message.
/// </summary>
public enum OutboxMessageStatus
{
    /// <summary>
    /// The message is waiting to be published.
    /// </summary>
    PENDING = 0,

    /// <summary>
    /// The message was published successfully.
    /// </summary>
    PUBLISHED = 1,

    /// <summary>
    /// The message publish failed and should be retried.
    /// </summary>
    FAILED = 2
}
