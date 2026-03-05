using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using Microsoft.Extensions.Logging;
using RulesEngine.Extensions;
using RulesEngine.Models;
using RulesEngine.Interfaces;

namespace FraudEngine.Infrastructure.Services;

public class RulesEngineService : IRulesEngineService
{
    private readonly IRuleRepository _ruleRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IVelocityService _velocityService;
    private readonly ILogger<RulesEngineService> _logger;

    private RulesEngine.RulesEngine? _rulesEngine;
    private Dictionary<string, int> _ruleScores = new();
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

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

    public async Task ReloadRulesAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            _logger.LogInformation("Reloading rules into RulesEngine...");
            var activeRules = await _ruleRepository.GetActiveRulesAsync(cancellationToken);

            var workflows = new List<Workflow>();
            _ruleScores.Clear();

            foreach (var rule in activeRules)
            {
                try
                {
                    var workflow = JsonSerializer.Deserialize<Workflow>(rule.WorkflowJson);
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
            }

            var reSettings = new ReSettings
            {
                CustomTypes = new[] { typeof(DateTimeOffset), typeof(TimeSpan), typeof(String) }
            };

            _rulesEngine = new RulesEngine.RulesEngine(workflows.ToArray(), reSettings);
            _logger.LogInformation("Successfully reloaded {Count} rules", workflows.Count);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<(int TotalScore, string TriggeredRulesJson, Decision Decision)> EvaluateAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        if (_rulesEngine == null)
        {
            await ReloadRulesAsync(cancellationToken);
        }

        // Preparation for complex rules
        var velocityCount = await _velocityService.GetRecentTransactionCountAsync(transaction.AccountId, cancellationToken);
        var isDuplicate = await _transactionRepository.ExistsRecentDuplicateAsync(
            transaction.AccountId, 
            transaction.Amount, 
            transaction.MerchantName, 
            DateTimeOffset.UtcNow.AddMinutes(-5), 
            cancellationToken);

        // Provide necessary data for evaluation
        var inputs = new dynamic[]
        {
            transaction,
            new { VelocityCount = velocityCount, IsDuplicate = isDuplicate, CurrentHourUtc = DateTimeOffset.UtcNow.Hour }
        };

        var triggeredRulesList = new List<object>();
        int totalScore = 0;

        foreach (var ruleName in _ruleScores.Keys)
        {
            var resultList = await _rulesEngine!.ExecuteAllRulesAsync(ruleName, inputs);

            foreach (var result in resultList)
            {
                if (result.IsSuccess)
                {
                    var score = _ruleScores[ruleName];
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
        }

        var decision = DetermineDecision(totalScore);
        var json = JsonSerializer.Serialize(triggeredRulesList);

        return (totalScore, json, decision);
    }

    private Decision DetermineDecision(int score)
    {
        if (score >= 70) return Decision.BLOCK;
        if (score >= 40) return Decision.REVIEW;
        return Decision.ALLOW;
    }
}
