using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.IntegrationEvents;

/// <summary>
/// Integration event emitted when a transaction is accepted for asynchronous processing.
/// </summary>
public sealed record TransactionSubmittedIntegrationEvent(
    Guid EventId,
    Guid TransactionId,
    string AccountId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string MerchantCategory,
    TransactionType TransactionType,
    string IPAddress,
    string DeviceId,
    int AccountAgeDays,
    DateTimeOffset Timestamp,
    DateTimeOffset SubmittedAt
);

/// <summary>
/// Integration event emitted when a fraud evaluation completes.
/// </summary>
public sealed record TransactionEvaluatedIntegrationEvent(
    Guid EventId,
    Guid TransactionId,
    string AccountId,
    Decision Decision,
    int RiskScore,
    IReadOnlyList<string> TriggeredRules,
    DateTimeOffset EvaluatedAt
);
