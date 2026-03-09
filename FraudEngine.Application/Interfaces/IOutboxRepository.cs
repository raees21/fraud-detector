using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines durable outbox operations used by the background publisher.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Retrieves pending or previously failed messages for publishing.
    /// </summary>
    public Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as published.
    /// </summary>
    public Task MarkPublishedAsync(Guid outboxMessageId, DateTimeOffset publishedAt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an outbox message as failed and increments the attempt count.
    /// </summary>
    public Task MarkFailedAsync(Guid outboxMessageId, string error, CancellationToken cancellationToken = default);
}
