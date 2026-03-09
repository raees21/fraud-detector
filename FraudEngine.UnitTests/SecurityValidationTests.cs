using FraudEngine.Application.DTOs;
using FraudEngine.Application.Features.Evaluations.Queries;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using FraudEngine.Application.Validation;
using FraudEngine.Domain.Enums;

namespace FraudEngine.UnitTests;

public class SecurityValidationTests
{
    [Fact]
    public void SubmitTransactionCommand_InvalidIpAddress_IsRejected()
    {
        var validator = new SubmitTransactionCommandValidator();
        var command = new SubmitTransactionCommand(new TransactionDto(
            "ACC-10001",
            25.50m,
            "USD",
            "Example Merchant",
            "RETAIL",
            TransactionType.CARD,
            "not-an-ip",
            "DEVICE-1",
            365,
            DateTimeOffset.UtcNow));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("IPAddress"));
    }

    [Fact]
    public void SubmitTransactionCommand_MissingIpAddressAndDeviceId_IsRejected()
    {
        var validator = new SubmitTransactionCommandValidator();
        var command = new SubmitTransactionCommand(new TransactionDto(
            "ACC-10001",
            25.50m,
            "USD",
            "Example Merchant",
            "RETAIL",
            TransactionType.CARD,
            string.Empty,
            string.Empty,
            365,
            DateTimeOffset.UtcNow));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("IPAddress"));
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("DeviceId"));
    }

    [Fact]
    public void SubmitTransactionCommand_UnknownTransactionType_IsRejected()
    {
        var validator = new SubmitTransactionCommandValidator();
        var command = new SubmitTransactionCommand(new TransactionDto(
            "ACC-10001",
            25.50m,
            "USD",
            "Example Merchant",
            "RETAIL",
            TransactionType.UNKNOWN,
            "203.0.113.10",
            "DEVICE-1",
            365,
            DateTimeOffset.UtcNow));

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith("TransactionType"));
    }

    [Fact]
    public void GetTransactionsQuery_PageSizeAboveMaximum_IsRejected()
    {
        var validator = new GetTransactionsQueryValidator();
        var query = new GetTransactionsQuery("ALLOW", "ACC-10001", null, null, 1, 101);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetTransactionsQuery.PageSize));
    }

    [Fact]
    public void GetEvaluationsQuery_NegativeMinScore_IsRejected()
    {
        var validator = new GetEvaluationsQueryValidator();
        var query = new GetEvaluationsQuery(null, -1, 1, 20);

        var result = validator.Validate(query);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(GetEvaluationsQuery.MinScore));
    }
}
