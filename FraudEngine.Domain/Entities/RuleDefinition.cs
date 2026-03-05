namespace FraudEngine.Domain.Entities;

/// <summary>
/// Represents a definition of a rule used to evaluate transactions for fraud.
/// </summary>
public class RuleDefinition
{
    /// <summary>
    /// Gets or sets the unique identifier for the rule definition.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the rule.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of what the rule evaluates.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the JSON definition of the rule, compatible with Microsoft Rules Engine.
    /// </summary>
    // Microsoft Rules Engine JSON definition
    public string WorkflowJson { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the rule is currently active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Gets or sets the score contributed to the overall risk score if this rule is triggered.
    /// </summary>
    public int ScoreContribution { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the rule definition was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
