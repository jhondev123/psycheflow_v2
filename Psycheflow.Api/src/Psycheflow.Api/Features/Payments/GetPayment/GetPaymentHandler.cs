using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Payments.GetPayment;

public sealed class GetPaymentHandler(PaymentAccess access)
{
    public async Task<Result<PaymentResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Payment> payment = await access.FindManageableAsync(id, cancellationToken);
        return payment.IsSuccess ? PaymentResponse.From(payment.Value) : payment.Error;
    }
}
