using System.Text.Json;
using FraudEngine.Application.IntegrationEvents;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Processes submitted transaction events and persists the resulting fraud evaluation.
/// </summary>
internal sealed class TransactionEventProcessor : ITransactionEventProcessor
{
    private readonly IOptions<KafkaOptions> _kafkaOptions;
    private readonly ILogger<TransactionEventProcessor> _logger;
    private readonly IRulesEngineService _rulesEngineService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly ITransactionWorkflowRepository _workflowRepository;

    public TransactionEventProcessor(
        ITransactionRepository transactionRepository,
        IRulesEngineService rulesEngineService,
        ITransactionWorkflowRepository workflowRepository,
        IOptions<KafkaOptions> kafkaOptions,
        ILogger<TransactionEventProcessor> logger)
    {
        _transactionRepository = transactionRepository;
        _rulesEngineService = rulesEngineService;
        _workflowRepository = workflowRepository;
        _kafkaOptions = kafkaOptions;
        _logger = logger;
    }

    public async Task ProcessAsync(TransactionSubmittedIntegrationEvent integrationEvent, string topic,
        CancellationToken cancellationToken = default)
    {
        bool shouldProcess = await _workflowRepository.TryBeginProcessingAsync(
            integrationEvent.TransactionId,
            integrationEvent.EventId,
            topic,
            cancellationToken);

        if (!shouldProcess)
            return;

        try
        {
            Transaction? transaction =
                await _transactionRepository.GetByIdAsync(integrationEvent.TransactionId, cancellationToken);
            if (transaction is null)
            {
                await _workflowRepository.FailProcessingAsync(
                    integrationEvent.TransactionId,
                    "Transaction was not found during asynchronous processing.",
                    integrationEvent.EventId,
                    topic,
                    cancellationToken);
                return;
            }

            (int score, string triggeredRulesJson, Decision decision) =
                await _rulesEngineService.EvaluateAsync(transaction, cancellationToken);

            var evaluation = new FraudEvaluation
            {
                TransactionId = transaction.Id,
                RiskScore = score,
                Decision = decision,
                TriggeredRules = triggeredRulesJson
            };

            var evaluatedEvent = new TransactionEvaluatedIntegrationEvent(
                Guid.NewGuid(),
                transaction.Id,
                transaction.AccountId,
                decision,
                score,
                GetTriggeredRuleNames(triggeredRulesJson),
                evaluation.EvaluatedAt);

            var outboxMessage = new OutboxMessage
            {
                EventId = evaluatedEvent.EventId,
                Topic = _kafkaOptions.Value.TransactionEvaluatedTopic,
                MessageKey = transaction.Id.ToString(),
                Payload = JsonSerializer.Serialize(evaluatedEvent)
            };

            await _workflowRepository.CompleteProcessingAsync(
                transaction.Id,
                evaluation,
                outboxMessage,
                integrationEvent.EventId,
                topic,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process submitted transaction event {EventId}", integrationEvent.EventId);
            await _workflowRepository.FailProcessingAsync(
                integrationEvent.TransactionId,
                ex.Message,
                integrationEvent.EventId,
                topic,
                cancellationToken);
        }
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
