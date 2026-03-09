using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Domain.Entities;

/// <summary>
/// Represents an integration event that has already been processed.
/// </summary>
[Index(nameof(Topic), nameof(ProcessedAt))]
public class ProcessedIntegrationEvent
{
    /// <summary>
    /// Gets or sets the unique event identifier.
    /// </summary>
    [Key]
    public Guid EventId { get; set; }

    /// <summary>
    /// Gets or sets the Kafka topic for the processed event.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp the event was processed.
    /// </summary>
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;
}
