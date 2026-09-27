using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Sessions.MarkNoShow;

/// <summary>RN-47: registra a falta do paciente depois do horário de início.</summary>
public sealed class MarkNoShowHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Session> session = await access.FindManageableAsync(id, cancellationToken);
        if (session.IsFailure)
        {
            return session.Error;
        }

        (_, DateTime localNow) = await access.GetClinicContextAsync(cancellationToken);
        Result marked = session.Value.MarkNoShow(localNow);
        if (marked.IsFailure)
        {
            return marked.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return access.ToResponse(session.Value);
    }
}
