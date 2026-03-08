using System.Net;
using FluentValidation;
using FraudEngine.Application.Features.Evaluations.Queries;
using FraudEngine.Application.Features.Transactions.Commands;
using FraudEngine.Application.Features.Transactions.Queries;
using FraudEngine.Domain.Enums;

namespace FraudEngine.Application.Validation;

/// <summary>
/// Validates incoming transaction evaluation requests before they reach persistence or rule execution.
/// </summary>
internal sealed class EvaluateTransactionCommandValidator : AbstractValidator<EvaluateTransactionCommand>
{
    public EvaluateTransactionCommandValidator()
    {
        RuleFor(command => command.Transaction.AccountId)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(command => command.Transaction.Amount)
            .GreaterThan(0m)
            .LessThanOrEqualTo(9999999999999999.99m);

        RuleFor(command => command.Transaction.Currency)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Z]{3}$")
            .WithMessage("Currency must be a three-letter ISO 4217 code.");

        RuleFor(command => command.Transaction.MerchantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.Transaction.MerchantCategory)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(command => command.Transaction.IPAddress)
            .Must(BeValidIpAddress)
            .WithMessage("IPAddress must be a valid IPv4 or IPv6 address when provided.")
            .When(command => !string.IsNullOrWhiteSpace(command.Transaction.IPAddress));

        RuleFor(command => command.Transaction.DeviceId)
            .MaximumLength(100)
            .When(command => !string.IsNullOrWhiteSpace(command.Transaction.DeviceId));

        RuleFor(command => command.Transaction.AccountAgeDays)
            .InclusiveBetween(0, 36500);

        RuleFor(command => command.Transaction.Timestamp)
            .Must(timestamp => timestamp != default)
            .WithMessage("Timestamp must be provided.");
    }

    private static bool BeValidIpAddress(string? ipAddress)
    {
        return string.IsNullOrWhiteSpace(ipAddress) || IPAddress.TryParse(ipAddress, out _);
    }
}

/// <summary>
/// Validates historical transaction query parameters to keep query cost bounded.
/// </summary>
internal sealed class GetTransactionsQueryValidator : AbstractValidator<GetTransactionsQuery>
{
    public GetTransactionsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.AccountId)
            .MaximumLength(100)
            .When(query => !string.IsNullOrWhiteSpace(query.AccountId));

        RuleFor(query => query.Decision)
            .Must(BeValidDecision)
            .WithMessage("Decision must be one of ALLOW, REVIEW, or BLOCK.")
            .When(query => !string.IsNullOrWhiteSpace(query.Decision));

        RuleFor(query => query)
            .Must(HaveValidDateRange)
            .WithMessage("'from' must be earlier than or equal to 'to'.");
    }

    private static bool BeValidDecision(string? decision)
    {
        return Enum.TryParse<Decision>(decision, ignoreCase: true, out _);
    }

    private static bool HaveValidDateRange(GetTransactionsQuery query)
    {
        return !query.From.HasValue || !query.To.HasValue || query.From <= query.To;
    }
}

/// <summary>
/// Validates evaluation history query parameters to keep query cost bounded.
/// </summary>
internal sealed class GetEvaluationsQueryValidator : AbstractValidator<GetEvaluationsQuery>
{
    public GetEvaluationsQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThan(0);

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(query => query.MinScore)
            .InclusiveBetween(0, 1000)
            .When(query => query.MinScore.HasValue);
    }
}
