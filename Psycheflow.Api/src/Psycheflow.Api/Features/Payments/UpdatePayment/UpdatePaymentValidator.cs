using FluentValidation;

namespace Psycheflow.Api.Features.Payments.UpdatePayment;

public sealed class UpdatePaymentValidator : AbstractValidator<UpdatePaymentRequest>
{
    public UpdatePaymentValidator() =>
        RuleFor(x => x.Amount)
            .NotNull().WithMessage("Informe o valor.")
            .GreaterThanOrEqualTo(0).WithMessage(PaymentErrors.InvalidAmount.Message);
}
