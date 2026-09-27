using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Sessions.ConfirmSession;

public sealed class ConfirmSessionHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Session> session = await access.FindManageableAsync(id, cancellationToken);
        if (session.IsFailure)
        {
            return session.Error;
        }

        Result confirmed = session.Value.Confirm();
        if (confirmed.IsFailure)
        {
            return confirmed.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return access.ToResponse(session.Value);
    }
}
