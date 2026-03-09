using System.Text.Json;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.IntegrationEvents;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

/// <summary>
/// Handles acceptance of a transaction for asynchronous fraud processing.
/// </summary>
public sealed class SubmitTransactionCommandHandler
    : IRequestHandler<SubmitTransactionCommand, Result<TransactionSubmissionAcceptedDto>>
{
    private readonly IIntegrationEventTopicProvider _integrationEventTopicProvider;
    private readonly ITransactionWorkflowRepository _transactionWorkflowRepository;

    public SubmitTransactionCommandHandler(
        ITransactionWorkflowRepository transactionWorkflowRepository,
        IIntegrationEventTopicProvider integrationEventTopicProvider)
    {
        _transactionWorkflowRepository = transactionWorkflowRepository;
        _integrationEventTopicProvider = integrationEventTopicProvider;
    }

    public async Task<Result<TransactionSubmissionAcceptedDto>> Handle(SubmitTransactionCommand request,
        CancellationToken cancellationToken)
    {
        TransactionDto dto = request.Transaction;
        var transaction = new Transaction
        {
            AccountId = dto.AccountId,
            Amount = dto.Amount,
            Currency = dto.Currency,
            MerchantName = dto.MerchantName,
            MerchantCategory = dto.MerchantCategory,
            TransactionType = dto.TransactionType,
            IPAddress = dto.IPAddress.Trim(),
            DeviceId = dto.DeviceId.Trim(),
            AccountAgeDays = dto.AccountAgeDays,
            Timestamp = dto.Timestamp,
            ProcessingStatus = TransactionProcessingStatus.PENDING
        };

        var integrationEvent = new TransactionSubmittedIntegrationEvent(
            Guid.NewGuid(),
            transaction.Id,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.MerchantName,
            transaction.MerchantCategory,
            transaction.TransactionType,
            transaction.IPAddress,
            transaction.DeviceId,
            transaction.AccountAgeDays,
            transaction.Timestamp,
            transaction.CreatedAt);

        var outboxMessage = new OutboxMessage
        {
            EventId = integrationEvent.EventId,
            Topic = _integrationEventTopicProvider.TransactionSubmittedTopic,
            MessageKey = transaction.Id.ToString(),
            Payload = JsonSerializer.Serialize(integrationEvent)
        };

        await _transactionWorkflowRepository.SubmitAsync(transaction, outboxMessage, cancellationToken);

        return Result<TransactionSubmissionAcceptedDto>.Success(new TransactionSubmissionAcceptedDto(
            transaction.Id,
            transaction.AccountId,
            transaction.ProcessingStatus.ToString(),
            transaction.CreatedAt));
    }
}
