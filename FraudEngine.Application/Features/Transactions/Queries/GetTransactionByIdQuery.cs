using System.Text.Json;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Queries;

/// <summary>
/// Query to retrieve the asynchronous fraud-processing status for a specific transaction.
/// </summary>
public record GetTransactionByIdQuery(Guid Id) : IRequest<Result<TransactionStatusDto>>;

/// <summary>
/// Handler for the <see cref="GetTransactionByIdQuery"/>.
/// </summary>
public class GetTransactionByIdQueryHandler : IRequestHandler<GetTransactionByIdQuery, Result<TransactionStatusDto>>
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly ITransactionRepository _repository;

    public GetTransactionByIdQueryHandler(ITransactionRepository repository, IEvaluationRepository evaluationRepository)
    {
        _repository = repository;
        _evaluationRepository = evaluationRepository;
    }

    /// <summary>
    /// Handles the query to retrieve a transaction by ID.
    /// </summary>
    public async Task<Result<TransactionStatusDto>> Handle(GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        Transaction? transaction = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (transaction == null)
            return Result<TransactionStatusDto>.Failure(new Error("Transaction.NotFound",
                "Transaction with the given ID was not found."));

        FraudEvaluation? evaluation = await _evaluationRepository.GetByTransactionIdAsync(request.Id, cancellationToken);

        return Result<TransactionStatusDto>.Success(new TransactionStatusDto(
            transaction.Id,
            transaction.AccountId,
            transaction.Amount,
            transaction.Currency,
            transaction.MerchantName,
            transaction.MerchantCategory,
            transaction.TransactionType,
            transaction.ProcessingStatus.ToString(),
            evaluation?.Decision.ToString(),
            evaluation is null ? Array.Empty<string>() : GetTriggeredRuleNames(evaluation.TriggeredRules),
            transaction.Timestamp,
            transaction.CreatedAt,
            evaluation?.EvaluatedAt,
            transaction.FailureReason
        ));
    }

    private static IReadOnlyList<string> GetTriggeredRuleNames(string triggeredRulesJson)
    {
        if (string.IsNullOrWhiteSpace(triggeredRulesJson))
            return Array.Empty<string>();

        try
        {
            using JsonDocument document = JsonDocument.Parse(triggeredRulesJson);
            return document.RootElement.ValueKind != JsonValueKind.Array
                ? Array.Empty<string>()
                : document.RootElement.EnumerateArray()
                    .Select(rule => rule.TryGetProperty("RuleName", out JsonElement ruleNameElement)
                        ? ruleNameElement.GetString()
                        : null)
                    .Where(ruleName => !string.IsNullOrWhiteSpace(ruleName))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }
}
