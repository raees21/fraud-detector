using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FraudEngine.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FraudEngine.Domain.Entities;

/// <summary>
/// Represents a financial transaction to be evaluated for fraud.
/// </summary>
[Index(nameof(AccountId))]
[Index(nameof(Timestamp))]
public class Transaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique identifier of the account initiating the transaction.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AccountId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the financial amount of the transaction.
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the currency code of the transaction (ISO 4217).
    /// </summary>
    [Required]
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty; // ISO 4217

    /// <summary>
    /// Gets or sets the name of the merchant involved in the transaction.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string MerchantName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category or type of the merchant.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MerchantCategory { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of transaction being evaluated.
    /// </summary>
    public TransactionType TransactionType { get; set; } = TransactionType.UNKNOWN;

    /// <summary>
    /// Gets or sets the IP address from which the transaction was initiated.
    /// </summary>
    [Required]
    [MaxLength(45)]
    public string IPAddress { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier of the device used for the transaction.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the age of the account in days at the time of the transaction.
    /// </summary>
    public int AccountAgeDays { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the transaction occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the timestamp of when the transaction record was created in the system.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
