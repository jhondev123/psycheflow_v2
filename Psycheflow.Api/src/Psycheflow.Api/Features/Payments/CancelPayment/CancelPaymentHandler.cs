using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Payments.CancelPayment;

/// <summary>Cancela um pagamento pendente ou estorna um pago, com motivo.</summary>
public sealed class CancelPaymentHandler(AppDbContext db, PaymentAccess access)
{
    public async Task<Result<PaymentResponse>> Handle(Guid id, CancelPaymentRequest request, CancellationToken cancellationToken)
    {
        Result<Payment> payment = await access.FindManageableAsync(id, cancellationToken);
        if (payment.IsFailure)
        {
            return payment.Error;
        }

        Result cancelled = payment.Value.Cancel(request.Reason!);
        if (cancelled.IsFailure)
        {
            return cancelled.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return PaymentResponse.From(payment.Value);
    }
}
