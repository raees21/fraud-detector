using FraudEngine.Domain.Entities;

namespace FraudEngine.Application.Interfaces;

/// <summary>
/// Defines atomic persistence workflows for asynchronous transaction processing.
/// </summary>
public interface ITransactionWorkflowRepository
{
    /// <summary>
    /// Persists a submitted transaction and its outbox message atomically.
    /// </summary>
    public Task SubmitAsync(Transaction transaction, OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Attempts to transition a submitted transaction into processing for a unique event.
    /// </summary>
    public Task<bool> TryBeginProcessingAsync(Guid transactionId, Guid eventId, string topic,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes transaction processing, stores the evaluation, emits a follow-up outbox event,
    /// and records the consumed event atomically.
    /// </summary>
    public Task CompleteProcessingAsync(Guid transactionId, FraudEvaluation evaluation, OutboxMessage outboxMessage,
        Guid eventId, string topic, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks processing as failed and records the consumed event atomically.
    /// </summary>
    public Task FailProcessingAsync(Guid transactionId, string failureReason, Guid eventId, string topic,
        CancellationToken cancellationToken = default);
}
