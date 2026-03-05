using System.Text.Json;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using RulesEngine.Models;

namespace FraudEngine.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="IRulesEngineService"/> using Microsoft RulesEngine.
/// </summary>
public class RulesEngineService : IRulesEngineService
{
    private static readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<RulesEngineService> _logger;
    private readonly IRuleRepository _ruleRepository;
    private readonly Dictionary<string, int> _ruleScores = new();
    private readonly ITransactionRepository _transactionRepository;
    private readonly IVelocityService _velocityService;

    private RulesEngine.RulesEngine? _rulesEngine;

    /// <summary>
    /// Initializes a new instance of the <see cref="RulesEngineService"/> class.
    /// </summary>
    public RulesEngineService(
        IRuleRepository ruleRepository,
        ITransactionRepository transactionRepository,
        IVelocityService velocityService,
        ILogger<RulesEngineService> logger)
    {
        _ruleRepository = ruleRepository;
        _transactionRepository = transactionRepository;
        _velocityService = velocityService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ReloadRulesAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Reloading rules into RulesEngine...");
            IEnumerable<RuleDefinition> activeRules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);

            var workflows = new List<Workflow>();
            _ruleScores.Clear();

            foreach (RuleDefinition rule in activeRules)
                try
                {
                    Workflow? workflow = JsonSerializer.Deserialize<Workflow>(rule.WorkflowJson);
                    if (workflow != null)
                    {
                        workflows.Add(workflow);
                        _ruleScores[rule.RuleName] = rule.ScoreContribution;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse workflow JSON for rule {RuleName}", rule.RuleName);
                }

            var reSettings = new ReSettings
            {
                CustomTypes = new[] { typeof(DateTimeOffset), typeof(TimeSpan), typeof(string) }
            };

            _rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray(), reSettings);
            _logger.LogInformation("Successfully reloaded {Count} rules", workflows.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <inheritdoc />
    public async Task<(int TotalScore, string TriggeredRulesJson, Decision Decision)> EvaluateAsync(
        Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (_rulesEngine == null) await ReloadRulesAsync(cancellationToken);

        // Preparation for complex rules
        int velocityCount =
            await _velocityService.GetRecentTransactionCountAsync(transaction.AccountId, cancellationToken);
        bool isDuplicate = await _transactionRepository.ExistsRecentDuplicateAsync(
            transaction.AccountId,
            transaction.Amount,
            transaction.MerchantName,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            cancellationToken);

        // Provide necessary data for evaluation
        dynamic[] inputs = new dynamic[]
        {
            transaction,
            new
            {
                VelocityCount = velocityCount,
                IsDuplicate = isDuplicate,
                CurrentHourUtc = DateTimeOffset.UtcNow.Hour
            }
        };

        var triggeredRulesList = new List<object>();
        int totalScore = 0;

        foreach (string ruleName in _ruleScores.Keys)
        {
            List<RuleResultTree>? resultList = await _rulesEngine!.ExecuteAllRulesAsync(ruleName, inputs);

            foreach (RuleResultTree result in resultList)
                if (result.IsSuccess)
                {
                    int score = _ruleScores[ruleName];
                    totalScore += score;
                    triggeredRulesList.Add(new
                    {
                        RuleName = ruleName,
                        Reason = resultList.First().Rule.RuleExpressionType == RuleExpressionType.LambdaExpression
                            ? resultList.First().Rule.SuccessEvent
                            : ruleName,
                        ScoreContribution = score
                    });
                }
        }

        Decision decision = DetermineDecision(totalScore);
        string json = JsonSerializer.Serialize(triggeredRulesList);

        return (totalScore, json, decision);
    }

    private Decision DetermineDecision(int score)
    {
        if (score >= 70) return Decision.BLOCK;
        if (score >= 40) return Decision.REVIEW;
        return Decision.ALLOW;
    }
}
