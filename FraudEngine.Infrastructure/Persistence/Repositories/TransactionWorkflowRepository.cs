using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Infrastructure.Persistence.Repositories;

/// <summary>
/// Implementation of <see cref="ITransactionWorkflowRepository"/> using Entity Framework Core.
/// </summary>
internal sealed class TransactionWorkflowRepository : ITransactionWorkflowRepository
{
    private readonly AppDbContext _context;

    public TransactionWorkflowRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task SubmitAsync(Transaction transaction, OutboxMessage outboxMessage,
        CancellationToken cancellationToken = default)
    {
        await using var databaseTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.Transactions.AddAsync(transaction, cancellationToken);
        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await databaseTransaction.CommitAsync(cancellationToken);
    }

    public async Task<bool> TryBeginProcessingAsync(Guid transactionId, Guid eventId, string topic,
        CancellationToken cancellationToken = default)
    {
        await using var databaseTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        if (await _context.ProcessedIntegrationEvents.AnyAsync(item => item.EventId == eventId, cancellationToken))
        {
            await databaseTransaction.CommitAsync(cancellationToken);
            return false;
        }

        Transaction? transaction = await _context.Transactions
            .FirstOrDefaultAsync(item => item.Id == transactionId, cancellationToken);

        if (transaction is null || transaction.ProcessingStatus != TransactionProcessingStatus.PENDING)
        {
            await _context.ProcessedIntegrationEvents.AddAsync(new ProcessedIntegrationEvent
            {
                EventId = eventId,
                Topic = topic
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            await databaseTransaction.CommitAsync(cancellationToken);
            return false;
        }

        transaction.ProcessingStatus = TransactionProcessingStatus.PROCESSING;
        transaction.FailureReason = null;

        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task CompleteProcessingAsync(Guid transactionId, FraudEvaluation evaluation, OutboxMessage outboxMessage,
        Guid eventId, string topic, CancellationToken cancellationToken = default)
    {
        await using var databaseTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        Transaction transaction = await _context.Transactions
            .FirstAsync(item => item.Id == transactionId, cancellationToken);

        transaction.ProcessingStatus = TransactionProcessingStatus.COMPLETED;
        transaction.FailureReason = null;

        await _context.FraudEvaluations.AddAsync(evaluation, cancellationToken);
        await _context.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _context.ProcessedIntegrationEvents.AddAsync(new ProcessedIntegrationEvent
        {
            EventId = eventId,
            Topic = topic
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
    }

    public async Task FailProcessingAsync(Guid transactionId, string failureReason, Guid eventId, string topic,
        CancellationToken cancellationToken = default)
    {
        await using var databaseTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        Transaction? transaction = await _context.Transactions
            .FirstOrDefaultAsync(item => item.Id == transactionId, cancellationToken);

        if (transaction is not null)
        {
            transaction.ProcessingStatus = TransactionProcessingStatus.FAILED;
            transaction.FailureReason = failureReason.Length <= 1000 ? failureReason : failureReason[..1000];
        }

        if (!await _context.ProcessedIntegrationEvents.AnyAsync(item => item.EventId == eventId, cancellationToken))
        {
            await _context.ProcessedIntegrationEvents.AddAsync(new ProcessedIntegrationEvent
            {
                EventId = eventId,
                Topic = topic
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await databaseTransaction.CommitAsync(cancellationToken);
    }
}
