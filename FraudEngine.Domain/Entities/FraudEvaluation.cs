using System;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Domain.Entities;

public class FraudEvaluation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TransactionId { get; set; }
    
    // Navigation property (EF Core)
    public Transaction Transaction { get; set; } = null!;
    
    public int RiskScore { get; set; }
    public Decision Decision { get; set; }
    
    // Stored as JSONB in PostgreSQL
    public string TriggeredRules { get; set; } = "[]"; 
    
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
}
