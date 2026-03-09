using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Common;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

/// <summary>
/// Command to accept a transaction for asynchronous fraud processing.
/// </summary>
public record SubmitTransactionCommand(TransactionDto Transaction) : IRequest<Result<TransactionSubmissionAcceptedDto>>;
