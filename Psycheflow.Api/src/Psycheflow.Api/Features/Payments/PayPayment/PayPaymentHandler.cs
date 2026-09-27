using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Payments.PayPayment;

/// <summary>UC11 / RF011: lança o pagamento (pendente → pago) de uma sessão concluída.</summary>
public sealed class PayPaymentHandler(AppDbContext db, PaymentAccess access, ICurrentUser currentUser, ClinicClock clock)
{
    public async Task<Result<PaymentResponse>> Handle(Guid id, PayPaymentRequest request, CancellationToken cancellationToken)
    {
        Result<Payment> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        CompanySettings settings = await db.GetCurrentSettingsAsync(currentUser, cancellationToken);
        DateOnly today = clock.Today(settings.TimeZone);

        Payment payment = found.Value;
        Result paid = payment.Pay(
            sessionCompleted: payment.Session!.Status == SessionStatus.Completed,
            request.Method!.Value,
            request.PaidAt ?? today,
            request.Amount,
            request.Notes,
            today);
        if (paid.IsFailure)
        {
            return paid.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return PaymentResponse.From(payment);
    }
}
