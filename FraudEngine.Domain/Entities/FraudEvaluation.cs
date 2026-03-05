using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Domain.Entities;

/// <summary>
/// Represents the result of evaluating a transaction against fraud rules.
/// </summary>
[Index(nameof(Decision))]
[Index(nameof(RiskScore))]
public class FraudEvaluation
{
    /// <summary>
    /// Gets or sets the unique identifier for the fraud evaluation.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique identifier of the evaluated transaction.
    /// </summary>
    [ForeignKey(nameof(Transaction))]
    public Guid TransactionId { get; set; }

    /// <summary>
    /// Gets or sets the evaluated transaction entity.
    /// </summary>
    // Navigation property (EF Core)
    public Transaction Transaction { get; set; } = null!;

    /// <summary>
    /// Gets or sets the calculated risk score for the transaction.
    /// </summary>
    public int RiskScore { get; set; }

    /// <summary>
    /// Gets or sets the final decision made based on the evaluation (e.g., ALLOW, REVIEW, BLOCK).
    /// </summary>
    [Required]
    public Decision Decision { get; set; }

    /// <summary>
    /// Gets or sets a JSON string representing the list of rules that were triggered during evaluation.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string TriggeredRules { get; set; } = "[]";

    /// <summary>
    /// Gets or sets the timestamp of when the evaluation was performed.
    /// </summary>
    public DateTimeOffset EvaluatedAt { get; set; } = DateTimeOffset.UtcNow;
}
