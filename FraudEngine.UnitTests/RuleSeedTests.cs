using System.Text.Json;
using FraudEngine.Infrastructure.Data;
using RulesEngine.Models;

namespace FraudEngine.UnitTests;

public class RuleSeedTests
{
    [Fact]
    public void GetSeedRules_ForeignCurrencyRule_UsesZarAsBaseCurrency()
    {
        var foreignCurrencyRule = AppDbContextSeed.GetSeedRules()
            .Single(rule => rule.RuleName == "FOREIGN_CURRENCY_HIGH_AMOUNT_RULE");

        Workflow? workflow = JsonSerializer.Deserialize<Workflow>(foreignCurrencyRule.WorkflowJson);

        Assert.NotNull(workflow);
        Rule seededRule = Assert.Single(workflow!.Rules);
        Assert.Equal("input1.Currency != \"ZAR\" AND input1.Amount > 5000", seededRule.Expression);
    }

    [Fact]
    public void GetSeedRules_RecentLocationChangeRule_IsPresent()
    {
        var locationChangeRule = AppDbContextSeed.GetSeedRules()
            .Single(rule => rule.RuleName == "RECENT_LOCATION_CHANGE_RULE");

        Workflow? workflow = JsonSerializer.Deserialize<Workflow>(locationChangeRule.WorkflowJson);

        Assert.NotNull(workflow);
        Rule seededRule = Assert.Single(workflow!.Rules);
        Assert.Equal("input2.HasRecentLocationChange == true", seededRule.Expression);
        Assert.Equal(40, locationChangeRule.ScoreContribution);
    }

    [Fact]
    public void GetSeedRules_RepeatedDeclinedTransactionRule_IsPresent()
    {
        var repeatedDeclinedRule = AppDbContextSeed.GetSeedRules()
            .Single(rule => rule.RuleName == "REPEATED_DECLINED_TRANSACTION_RULE");

        Workflow? workflow = JsonSerializer.Deserialize<Workflow>(repeatedDeclinedRule.WorkflowJson);

        Assert.NotNull(workflow);
        Rule seededRule = Assert.Single(workflow!.Rules);
        Assert.Equal("input2.RecentBlockedAttemptCount >= 3", seededRule.Expression);
        Assert.Equal(70, repeatedDeclinedRule.ScoreContribution);
    }

    [Fact]
    public void GetSeedRules_TransactionTypeRules_ArePresentWithExpectedScores()
    {
        Dictionary<string, (string Expression, int Score)> expectedRules = new()
        {
            ["TRANSACTION_TYPE_EFT_RULE"] = ("input1.TransactionType == TransactionType.EFT", 10),
            ["TRANSACTION_TYPE_CARD_RULE"] = ("input1.TransactionType == TransactionType.CARD", 15),
            ["TRANSACTION_TYPE_AUTOMATED_RECURRING_RULE"] =
                ("input1.TransactionType == TransactionType.AUTOMATED_OR_RECURRING", 5),
            ["TRANSACTION_TYPE_MOBILE_RULE"] = ("input1.TransactionType == TransactionType.MOBILE", 15),
            ["TRANSACTION_TYPE_EWALLET_RULE"] = ("input1.TransactionType == TransactionType.EWALLET", 20)
        };

        foreach ((string ruleName, (string expression, int score)) in expectedRules)
        {
            var seededRule = AppDbContextSeed.GetSeedRules()
                .Single(rule => rule.RuleName == ruleName);

            Workflow? workflow = JsonSerializer.Deserialize<Workflow>(seededRule.WorkflowJson);

            Assert.NotNull(workflow);
            Rule rule = Assert.Single(workflow!.Rules);
            Assert.Equal(expression, rule.Expression);
            Assert.Equal(score, seededRule.ScoreContribution);
        }
    }
}
