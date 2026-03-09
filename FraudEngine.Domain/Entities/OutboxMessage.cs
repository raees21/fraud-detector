using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Domain.Entities;

/// <summary>
/// Represents a durable integration message waiting to be published to Kafka.
/// </summary>
[Index(nameof(Status), nameof(CreatedAt))]
public class OutboxMessage
{
    /// <summary>
    /// Gets or sets the unique identifier for the outbox row.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the integration event identifier used for idempotency.
    /// </summary>
    public Guid EventId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the Kafka topic.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Kafka message key.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MessageKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the serialized payload.
    /// </summary>
    [Required]
    [Column(TypeName = "jsonb")]
    public string Payload { get; set; } = "{}";

    /// <summary>
    /// Gets or sets the current publish status.
    /// </summary>
    [Required]
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.PENDING;

    /// <summary>
    /// Gets or sets the number of publish attempts.
    /// </summary>
    public int Attempts { get; set; }

    /// <summary>
    /// Gets or sets the last publish error if one occurred.
    /// </summary>
    [MaxLength(2000)]
    public string? LastError { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the publish timestamp.
    /// </summary>
    public DateTimeOffset? PublishedAt { get; set; }
}
