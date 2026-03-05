using System.Text.Json;
using FraudEngine.Domain.Entities;
using FraudEngine.Infrastructure.Persistence;
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
        if (!context.RuleDefinitions.Any())
        {
            var rules = new List<RuleDefinition>
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
                CreateRule("DUPLICATE_TRANSACTION_RULE", "Possible duplicate transaction", 35,
                    "input2.IsDuplicate == true"),
                CreateRule("FOREIGN_CURRENCY_HIGH_AMOUNT_RULE", "High-value foreign currency transaction", 20,
                    "input1.Currency != \"USD\" AND input1.Amount > 5000")
            };

            await context.RuleDefinitions.AddRangeAsync(rules);
            await context.SaveChangesAsync();
        }
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
