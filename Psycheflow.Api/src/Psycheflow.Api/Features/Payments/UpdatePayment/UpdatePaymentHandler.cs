using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Payments.UpdatePayment;

/// <summary>UC13 / RF013: altera o valor de um pagamento pendente (RN-56). O status muda por "pagar" e "cancelar".</summary>
public sealed class UpdatePaymentHandler(AppDbContext db, PaymentAccess access)
{
    public async Task<Result<PaymentResponse>> Handle(Guid id, UpdatePaymentRequest request, CancellationToken cancellationToken)
    {
        Result<Payment> payment = await access.FindManageableAsync(id, cancellationToken);
        if (payment.IsFailure)
        {
            return payment.Error;
        }

        Result changed = payment.Value.ChangeAmount(request.Amount!.Value);
        if (changed.IsFailure)
        {
            return changed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return PaymentResponse.From(payment.Value);
    }
}
