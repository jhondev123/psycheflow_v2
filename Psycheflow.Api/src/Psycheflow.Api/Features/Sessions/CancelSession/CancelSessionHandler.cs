using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Sessions.CancelSession;

/// <summary>UC07 / RF007: cancela com motivo, libera o horário e cancela o pagamento pendente (RN-43).</summary>
public sealed class CancelSessionHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, CancelSessionRequest request, CancellationToken cancellationToken)
    {
        Result<Session> session = await access.FindManageableAsync(id, cancellationToken);
        if (session.IsFailure)
        {
            return session.Error;
        }

        Result cancelled = SessionAccess.Cancel(session.Value, request.Reason!);
        if (cancelled.IsFailure)
        {
            return cancelled.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return access.ToResponse(session.Value);
    }
}
