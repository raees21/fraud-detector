using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FraudEngine.Application.Interfaces;
using FraudEngine.Domain.Entities;
using FraudEngine.Domain.Enums;
using FraudEngine.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace FraudEngine.UnitTests;

public class EvaluationTests
{
    private readonly Mock<IRuleRepository> _ruleRepoMock;
    private readonly Mock<ITransactionRepository> _txRepoMock;
    private readonly Mock<IVelocityService> _velocitySvcMock;
    private readonly RulesEngineService _sut;

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
        var json = $$"""
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

        return new RuleDefinition
        {
            RuleName = name,
            ScoreContribution = score,
            WorkflowJson = json,
            IsActive = true
        };
    }

    [Fact]
    public async Task EvaluateAsync_HighAmount_TriggersRule()
    {
        // Arrange
        SetupMockRules(CreateRule("HIGH_AMOUNT_RULE", 35, "input1.Amount > 10000"));
        var tx = new Transaction { Amount = 15000 };

        _velocitySvcMock.Setup(x => x.GetRecentTransactionCountAsync(It.IsAny<string>(), default))
            .ReturnsAsync(1);
        _txRepoMock.Setup(x => x.ExistsRecentDuplicateAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<DateTimeOffset>(), default))
            .ReturnsAsync(false);

        // Act
        var (score, rulesJson, decision) = await _sut.EvaluateAsync(tx);

        // Assert
        score.Should().Be(35);
        decision.Should().Be(Decision.ALLOW); // < 40
        rulesJson.Should().Contain("HIGH_AMOUNT_RULE");
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
        var (score, rulesJson, decision) = await _sut.EvaluateAsync(tx);

        // Assert
        score.Should().Be(40);
        decision.Should().Be(Decision.REVIEW); // 40 = REVIEW
        rulesJson.Should().Contain("VELOCITY_RULE");
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
        var (score, rulesJson, decision) = await _sut.EvaluateAsync(tx);

        // Assert
        score.Should().Be(75); // 35 + 40
        decision.Should().Be(Decision.BLOCK); // >= 70 BLOCK
        rulesJson.Should().Contain("HIGH_AMOUNT_RULE").And.Contain("VELOCITY_RULE");
    }
}
