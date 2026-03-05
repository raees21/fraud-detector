using FraudEngine.Application.DTOs;
using FraudEngine.Domain.Common;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

public record EvaluateTransactionCommand(TransactionDto Transaction) : IRequest<Result<FraudEvaluationResultDto>>;
