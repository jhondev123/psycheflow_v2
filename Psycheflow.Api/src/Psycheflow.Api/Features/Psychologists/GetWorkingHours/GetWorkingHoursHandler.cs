using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.GetWorkingHours;

public sealed class GetWorkingHoursHandler(AppDbContext db)
{
    public async Task<Result<IReadOnlyList<WorkingHoursDto>>> Handle(Guid psychologistId, CancellationToken cancellationToken)
    {
        Psychologist? psychologist = await db.FindPsychologistAsync(psychologistId, cancellationToken);
        return psychologist is null
            ? PsychologistErrors.NotFound
            : Result<IReadOnlyList<WorkingHoursDto>>.Success(WorkingHoursDto.From(psychologist.WorkingHours));
    }
}
