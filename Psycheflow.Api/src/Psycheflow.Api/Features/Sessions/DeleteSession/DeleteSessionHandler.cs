using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Sessions.DeleteSession;

/// <summary>
/// RF008 (exclusão): exclusão lógica da sessão e do item de agenda, liberando o horário.
/// Sessões concluídas fazem parte do registro clínico e não são excluídas.
/// </summary>
public sealed class DeleteSessionHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<Session> session = await access.FindManageableAsync(id, cancellationToken);
        if (session.IsFailure)
        {
            return session.Error;
        }

        if (session.Value.Status == SessionStatus.Completed)
        {
            return SessionErrors.CompletedCannotBeDeleted;
        }

        db.Sessions.Remove(session.Value);
        db.Schedules.Remove(session.Value.Schedule);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
