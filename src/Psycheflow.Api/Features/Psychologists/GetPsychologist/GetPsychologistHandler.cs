using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.GetPsychologist;

public sealed class GetPsychologistHandler(AppDbContext db)
{
    public async Task<Result<PsychologistResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Psychologist? psychologist = await db.FindPsychologistAsync(id, cancellationToken);
        return psychologist is null ? PsychologistErrors.NotFound : PsychologistResponse.From(psychologist);
    }
}
