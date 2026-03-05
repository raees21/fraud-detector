using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Common;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

/// <summary>
/// Command to request the evaluation of a single transaction against the active fraud rules.
/// </summary>
public record EvaluateTransactionCommand(TransactionDto Transaction) : IRequest<Result<FraudEvaluationResultDto>>;
