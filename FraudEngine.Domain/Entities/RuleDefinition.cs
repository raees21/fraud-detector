using System;

namespace FraudEngine.Domain.Entities;

public class RuleDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RuleName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Microsoft Rules Engine JSON definition
    public string WorkflowJson { get; set; } = string.Empty;
    
    public bool IsActive { get; set; } = true;
    public int ScoreContribution { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
