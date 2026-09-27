using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.GetMyProfile;

public sealed class GetMyProfileHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PsychologistResponse>> Handle(CancellationToken cancellationToken)
    {
        if (currentUser.PsychologistId is not { } psychologistId)
        {
            return PsychologistErrors.ProfileNotFound;
        }

        Psychologist? psychologist = await db.FindPsychologistAsync(psychologistId, cancellationToken);
        return psychologist is null ? PsychologistErrors.ProfileNotFound : PsychologistResponse.From(psychologist);
    }
}
