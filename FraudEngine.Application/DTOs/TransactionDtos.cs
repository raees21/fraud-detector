using System;

namespace FraudEngine.Application.DTOs;

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

public record FraudEvaluationResultDto(
    Guid TransactionId,
    int RiskScore,
    string Decision,
    string TriggeredRules
);
