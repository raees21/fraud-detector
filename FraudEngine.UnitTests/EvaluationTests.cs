using Xunit;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using FraudEngine.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FraudEngine.UnitTests;

public class EvaluationTests
{
    private readonly Mock<IRuleRepository> _ruleRepoMock;
    private readonly RulesEngineService _sut;
    private readonly Mock<ITransactionRepository> _txRepoMock;
    private readonly Mock<IVelocityService> _velocitySvcMock;

    public EvaluationTests()
    {
        _ruleRepoMock = new Mock<IRuleRepository>();
        _txRepoMock = new Mock<ITransactionRepository>();
        _velocitySvcMock = new Mock<IVelocityService>();

        _sut = new RulesEngineService(
            _ruleRepoMock.Object,
            _txRepoMock.Object,
            _velocitySvcMock.Object,
            new NullLogger<RulesEngineService>()
        );
    }

    private void SetupMockRules(params RuleDefinition[] rules)
    {
        _ruleRepoMock.Setup(x => x.GetActiveRulesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(rules);
    }

    private RuleDefinition CreateRule(string name, int score, string expression)
    {
        string json = $$"""
                        {
                            "WorkflowName": "{{name}}",
                            "Rules": [
                                {
                                    "RuleName": "{{name}}_Condition",
                                    "SuccessEvent": "Matched",
                                    "RuleExpressionType": "LambdaExpression",
                                    "Expression": "{{expression}}"
                                }
                            ]
                        }
                        """;

        return new RuleDefinition { RuleName = name, ScoreContribution = score, WorkflowJson = json, IsActive = true };
    }

    [Fact]
    public async Task EvaluateAsync_HighAmount_TriggersRule()
    {
        // Arrange
        SetupMockRules(CreateRule("HIGH_AMOUNT_RULE", 35, "input1.Amount > 10000"));
        var tx = new Transaction { Amount = 15000 };

        _velocitySvcMock.Setup(x => x.GetRecentTransactionCountAsync(It.IsAny<string>(), default))
            .ReturnsAsync(1);
        _txRepoMock.Setup(x => x.ExistsRecentDuplicateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(), default))
            .ReturnsAsync(false);

        // Act
        (int score, string rulesJson, Decision decision) = await _sut.EvaluateAsync(tx);

        // Assert
        Assert.Equal(35, score);
        Assert.Equal(Decision.ALLOW, decision); // < 40
        Assert.Contains("HIGH_AMOUNT_RULE", rulesJson);
    }

    [Fact]
    public async Task EvaluateAsync_VelocityExceeded_TriggersRule()
    {
        // Arrange
        SetupMockRules(CreateRule("VELOCITY_RULE", 40, "input2.VelocityCount > 3"));
        var tx = new Transaction { AccountId = "ACC-123" };

        _velocitySvcMock.Setup(x => x.GetRecentTransactionCountAsync("ACC-123", default))
            .ReturnsAsync(4); // Triggering condition

        // Act
        (int score, string rulesJson, Decision decision) = await _sut.EvaluateAsync(tx);

        // Assert
        Assert.Equal(40, score);
        Assert.Equal(Decision.REVIEW, decision); // 40 = REVIEW
        Assert.Contains("VELOCITY_RULE", rulesJson);
    }

    [Fact]
    public async Task EvaluateAsync_MultipleRules_ScoreAggregatesAndDecidesBlock()
    {
        // Arrange
        SetupMockRules(
            CreateRule("HIGH_AMOUNT_RULE", 35, "input1.Amount > 10000"),
            CreateRule("VELOCITY_RULE", 40, "input2.VelocityCount > 3")
        );
        var tx = new Transaction { Amount = 15000, AccountId = "ACC-123" };

        _velocitySvcMock.Setup(x => x.GetRecentTransactionCountAsync("ACC-123", default)).ReturnsAsync(4);

        // Act
        (int score, string rulesJson, Decision decision) = await _sut.EvaluateAsync(tx);

        // Assert
        Assert.Equal(75, score); // 35 + 40
        Assert.Equal(Decision.BLOCK, decision); // >= 70 BLOCK
        Assert.Contains("HIGH_AMOUNT_RULE", rulesJson);
        Assert.Contains("VELOCITY_RULE", rulesJson);
    }
}
