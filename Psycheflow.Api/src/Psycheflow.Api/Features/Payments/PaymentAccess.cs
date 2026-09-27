using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Payments;

/// <summary>Pagamentos seguem o acesso da sessão (D-02): o psicólogo da sessão ou Admin/Manager.</summary>
public sealed class PaymentAccess(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<Payment>> FindManageableAsync(Guid id, CancellationToken cancellationToken)
    {
        Payment? payment = await db.Payments
            .Include(p => p.Session!).ThenInclude(s => s.Schedule)
            .Include(p => p.Session!).ThenInclude(s => s.Patient)
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (payment?.Session is null)
        {
            return PaymentErrors.NotFound;
        }

        return currentUser.CanManagePsychologist(payment.Session.PsychologistId) ? payment : SessionErrors.CannotAccess;
    }
}
