using System.Text.Json;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using FraudEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RulesEngine.Models;

namespace FraudEngine.Infrastructure.Data;

/// <summary>
/// Provides functionality to seed the database with initial required data.
/// </summary>
public static class AppDbContextSeed
{
    /// <summary>
    /// Seeds the initial set of fraud rule definitions if none exist.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        IReadOnlyList<RuleDefinition> seededRules = GetSeedRules();
        List<RuleDefinition> existingRules = await context.RuleDefinitions.ToListAsync();
        var existingRulesByName = existingRules.ToDictionary(rule => rule.RuleName, StringComparer.OrdinalIgnoreCase);
        bool hasChanges = false;

        foreach (RuleDefinition seededRule in seededRules)
        {
            if (existingRulesByName.TryGetValue(seededRule.RuleName, out RuleDefinition? existingRule))
            {
                if (existingRule.Description != seededRule.Description ||
                    existingRule.ScoreContribution != seededRule.ScoreContribution ||
                    existingRule.WorkflowJson != seededRule.WorkflowJson)
                {
                    existingRule.Description = seededRule.Description;
                    existingRule.ScoreContribution = seededRule.ScoreContribution;
                    existingRule.WorkflowJson = seededRule.WorkflowJson;
                    hasChanges = true;
                }
            }
            else
            {
                await context.RuleDefinitions.AddAsync(seededRule);
                hasChanges = true;
            }
        }

        if (hasChanges)
            await context.SaveChangesAsync();
    }

    internal static IReadOnlyList<RuleDefinition> GetSeedRules()
    {
        return new List<RuleDefinition>
        {
            CreateRule("HIGH_AMOUNT_RULE", "Transaction exceeds high-value threshold", 35,
                "input1.Amount > 10000"),
            CreateRule("VELOCITY_RULE", "Velocity threshold exceeded", 40,
                "input2.VelocityCount > 3"),
            CreateRule("SUSPICIOUS_HOUR_RULE", "Transaction during high-risk hours", 15,
                "input2.CurrentHourUtc >= 1 AND input2.CurrentHourUtc <= 4"),
            CreateRule("HIGH_RISK_MERCHANT_RULE", "High-risk merchant category", 25,
                "new[] { \"GAMBLING\", \"CRYPTO\", \"ADULT\" }.Contains(input1.MerchantCategory)"),
            CreateRule("NEW_ACCOUNT_RULE", "Account created less than 7 days ago", 20,
                "input1.AccountAgeDays < 7"),
            CreateRule("TRANSACTION_TYPE_EFT_RULE", "EFT transactions carry moderate account-takeover risk", 10,
                "input1.TransactionType == TransactionType.EFT"),
            CreateRule("TRANSACTION_TYPE_CARD_RULE", "Card transactions carry elevated fraud risk", 15,
                "input1.TransactionType == TransactionType.CARD"),
            CreateRule("TRANSACTION_TYPE_AUTOMATED_RECURRING_RULE",
                "Automated or recurring transactions carry low residual fraud risk", 5,
                "input1.TransactionType == TransactionType.AUTOMATED_OR_RECURRING"),
            CreateRule("TRANSACTION_TYPE_MOBILE_RULE", "Mobile transactions carry elevated device-channel risk", 15,
                "input1.TransactionType == TransactionType.MOBILE"),
            CreateRule("TRANSACTION_TYPE_EWALLET_RULE", "E-wallet transactions carry higher transfer-out risk", 20,
                "input1.TransactionType == TransactionType.EWALLET"),
            CreateRule("DUPLICATE_TRANSACTION_RULE", "Possible duplicate transaction", 35,
                "input2.IsDuplicate == true"),
            CreateRule("REPEATED_DECLINED_TRANSACTION_RULE",
                "Multiple blocked transactions in 30 minutes (possible card testing)", 70,
                "input2.RecentBlockedAttemptCount >= 3"),
            CreateRule("RECENT_LOCATION_CHANGE_RULE", "Recent account activity from a different country", 40,
                "input2.HasRecentLocationChange == true"),
            CreateRule("FOREIGN_CURRENCY_HIGH_AMOUNT_RULE", "High-value foreign currency transaction", 20,
                "input1.Currency != \"ZAR\" AND input1.Amount > 5000")
        };
    }

    /// <summary>
    /// Helper method to create a standardized rule definition entity.
    /// </summary>
    private static RuleDefinition CreateRule(string name, string description, int score, string expression)
    {
        var workflow = new Workflow
        {
            WorkflowName = name,
            Rules = new List<Rule>
            {
                new()
                {
                    RuleName = $"{name}_Condition",
                    SuccessEvent = description,
                    ErrorMessage = "Validation failed.",
                    RuleExpressionType = RuleExpressionType.LambdaExpression,
                    Expression = expression
                }
            }
        };

        return new RuleDefinition
        {
            RuleName = name,
            Description = description,
            ScoreContribution = score,
            WorkflowJson = JsonSerializer.Serialize(workflow)
        };
    }
}
