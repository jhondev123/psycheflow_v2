using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Sessions.GetSession;

public sealed class GetSessionHandler(SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Session> session = await access.FindManageableAsync(id, cancellationToken);
        return session.IsSuccess ? access.ToResponse(session.Value) : session.Error;
    }
}
