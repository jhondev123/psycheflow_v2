using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.SetWorkingHours;

/// <summary>UC27 / RN-68: substitui o expediente do psicólogo (o próprio ou Admin/Manager).</summary>
public sealed class SetWorkingHoursHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<IReadOnlyList<WorkingHoursDto>>> Handle(
        Guid psychologistId, SetWorkingHoursRequest request, CancellationToken cancellationToken)
    {
        Psychologist? psychologist = await db.FindPsychologistAsync(psychologistId, cancellationToken);
        if (psychologist is null)
        {
            return PsychologistErrors.NotFound;
        }

        if (!currentUser.CanManagePsychologist(psychologist.Id))
        {
            return PsychologistErrors.CannotManage;
        }

        Result updated = psychologist.SetWorkingHours(
            request.Hours.Select(h => new WorkingHoursRange(h.DayOfWeek, h.StartTime, h.EndTime)));
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<IReadOnlyList<WorkingHoursDto>>.Success(WorkingHoursDto.From(psychologist.WorkingHours));
    }
}
