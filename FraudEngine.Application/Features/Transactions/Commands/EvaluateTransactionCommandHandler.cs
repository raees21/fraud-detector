using System.Text.Json;
using FraudEngine.Application.DTOs;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Common;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using MediatR;

namespace FraudEngine.Application.Features.Transactions.Commands;

/// <summary>
/// Handler for the <see cref="EvaluateTransactionCommand"/>.
/// </summary>
public class
    EvaluateTransactionCommandHandler : IRequestHandler<EvaluateTransactionCommand, Result<FraudEvaluationResultDto>>
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IRulesEngineService _rulesEngineService;
    private readonly ITransactionRepository _transactionRepository;

    public EvaluateTransactionCommandHandler(
        ITransactionRepository transactionRepository,
        IEvaluationRepository evaluationRepository,
        IRulesEngineService rulesEngineService)
    {
        _transactionRepository = transactionRepository;
        _evaluationRepository = evaluationRepository;
        _rulesEngineService = rulesEngineService;
    }

    /// <summary>
    /// Handles the command to evaluate a transaction.
    /// </summary>
    public async Task<Result<FraudEvaluationResultDto>> Handle(EvaluateTransactionCommand request,
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
            Timestamp = dto.Timestamp
        };

        // Save transaction
        await _transactionRepository.AddAsync(transaction, cancellationToken);

        // Evaluate rules
        (int score, string triggeredRules, Decision decision) =
            await _rulesEngineService.EvaluateAsync(transaction, cancellationToken);

        var evaluation = new FraudEvaluation
        {
            TransactionId = transaction.Id, RiskScore = score, Decision = decision, TriggeredRules = triggeredRules
        };

        // Save evaluation
        await _evaluationRepository.AddAsync(evaluation, cancellationToken);

        return Result<FraudEvaluationResultDto>.Success(new FraudEvaluationResultDto(
            transaction.Id,
            transaction.AccountId,
            evaluation.Decision.ToString(),
            GetTriggeredRuleNames(triggeredRules),
            evaluation.EvaluatedAt
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
