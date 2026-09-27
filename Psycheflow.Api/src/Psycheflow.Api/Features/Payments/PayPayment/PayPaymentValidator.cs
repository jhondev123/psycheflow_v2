using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.Payments.PayPayment;

public sealed class PayPaymentValidator : AbstractValidator<PayPaymentRequest>
{
    public PayPaymentValidator()
    {
        RuleFor(x => x.Method).NotNull().WithMessage("Informe o método de pagamento.").IsInEnum().WithMessage("Método inválido.");
        RuleFor(x => x.Amount).GreaterThanOrEqualTo(0).WithMessage(PaymentErrors.InvalidAmount.Message);
        RuleFor(x => x.Notes).OptionalText(Payment.NotesMaxLength);
    }
}
