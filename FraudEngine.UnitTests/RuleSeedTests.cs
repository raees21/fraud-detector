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
}
