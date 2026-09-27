using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Sessions.CompleteSession;

/// <summary>UC08 / RF009: conclusão com anotações e feedback 0–10. Ato clínico: só o psicólogo da sessão.</summary>
public sealed class CompleteSessionHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, CompleteSessionRequest request, CancellationToken cancellationToken)
    {
        Result<Session> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Session session = found.Value;
        if (!access.CanSeeClinicalData(session))
        {
            return SessionErrors.ClinicalDataRestricted;
        }

        (_, DateTime localNow) = await access.GetClinicContextAsync(cancellationToken);
        Result completed = session.Complete(request.Notes!, request.FeedbackScore!.Value, request.FeedbackComment, localNow);
        if (completed.IsFailure)
        {
            return completed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return access.ToResponse(session);
    }
}
