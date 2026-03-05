using FluentValidation;

namespace FraudEngine.Application.Features.Transactions.Commands;

public class EvaluateTransactionCommandValidator : AbstractValidator<EvaluateTransactionCommand>
{
    public EvaluateTransactionCommandValidator()
    {
        RuleFor(x => x.Transaction).NotNull();
        When(x => x.Transaction != null, () =>
        {
            RuleFor(x => x.Transaction.AccountId).NotEmpty();
            RuleFor(x => x.Transaction.Amount).GreaterThan(0);
            RuleFor(x => x.Transaction.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Transaction.MerchantName).NotEmpty();
            RuleFor(x => x.Transaction.MerchantCategory).NotEmpty();
            RuleFor(x => x.Transaction.IPAddress).NotEmpty();
            RuleFor(x => x.Transaction.DeviceId).NotEmpty();
            RuleFor(x => x.Transaction.AccountAgeDays).GreaterThanOrEqualTo(0);
            RuleFor(x => x.Transaction.Timestamp).NotEmpty();
        });
    }
}
