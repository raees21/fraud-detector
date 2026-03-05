using System;

namespace FraudEngine.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty; // ISO 4217
    public string MerchantName { get; set; } = string.Empty;
    public string MerchantCategory { get; set; } = string.Empty;
    public string IPAddress { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int AccountAgeDays { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
