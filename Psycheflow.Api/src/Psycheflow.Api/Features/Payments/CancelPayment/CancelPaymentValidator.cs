using FluentValidation;

namespace Psycheflow.Api.Features.Payments.CancelPayment;

public sealed class CancelPaymentValidator : AbstractValidator<CancelPaymentRequest>
{
    public CancelPaymentValidator() =>
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(PaymentErrors.ReasonRequired.Message)
            .MaximumLength(Payment.NotesMaxLength).WithMessage($"Use no máximo {Payment.NotesMaxLength} caracteres.");
}
