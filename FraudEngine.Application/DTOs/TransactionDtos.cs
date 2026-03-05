using System;

namespace FraudEngine.Application.DTOs;

/// <summary>
/// Data transfer object representing an incoming transaction for evaluation.
/// </summary>
public record TransactionDto(
    string AccountId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string MerchantCategory,
    string IPAddress,
    string DeviceId,
    int AccountAgeDays,
    DateTimeOffset Timestamp
);

/// <summary>
/// Data transfer object representing the result of a fraud evaluation.
/// </summary>
public record FraudEvaluationResultDto(
    Guid TransactionId,
    int RiskScore,
    string Decision,
    string TriggeredRules
);
