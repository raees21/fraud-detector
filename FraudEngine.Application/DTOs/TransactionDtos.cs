using System;
using FraudEngine.Domain.Enums;

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
    TransactionType TransactionType,
    string IPAddress,
    string DeviceId,
    int AccountAgeDays,
    DateTimeOffset Timestamp
);

/// <summary>
/// Data transfer object representing an accepted asynchronous transaction submission.
/// </summary>
public record TransactionSubmissionAcceptedDto(
    Guid TransactionId,
    string AccountId,
    string Status,
    DateTimeOffset SubmittedAt
);

/// <summary>
/// A sanitized view of transaction history safe for general API responses.
/// </summary>
public record TransactionSummaryDto(
    Guid TransactionId,
    string MaskedAccountId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string MerchantCategory,
    TransactionType TransactionType,
    string Status,
    DateTimeOffset Timestamp,
    DateTimeOffset CreatedAt
);

/// <summary>
/// A detailed transaction status view used for polling asynchronous fraud evaluation.
/// </summary>
public record TransactionStatusDto(
    Guid TransactionId,
    string AccountId,
    decimal Amount,
    string Currency,
    string MerchantName,
    string MerchantCategory,
    TransactionType TransactionType,
    string Status,
    string? Decision,
    IReadOnlyList<string> TriggeredRules,
    DateTimeOffset Timestamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EvaluatedAt,
    string? FailureReason
);

/// <summary>
/// A sanitized view of fraud evaluation history safe for general API responses.
/// </summary>
public record FraudEvaluationSummaryDto(
    Guid TransactionId,
    string Decision,
    DateTimeOffset EvaluatedAt
);

/// <summary>
/// A limited rule definition view that excludes internal fraud logic details.
/// </summary>
public record RuleSummaryDto(
    Guid Id,
    string RuleName,
    string Description,
    bool IsActive
);
