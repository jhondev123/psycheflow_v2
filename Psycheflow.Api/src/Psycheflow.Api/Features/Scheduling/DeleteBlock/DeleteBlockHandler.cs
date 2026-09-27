using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Scheduling.DeleteBlock;

public sealed class DeleteBlockHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result> Handle(Guid id, CancellationToken cancellationToken)
    {
        Schedule? block = await db.Schedules.SingleOrDefaultAsync(s => s.Id == id && s.Type == ScheduleType.Block, cancellationToken);
        if (block is null)
        {
            return SchedulingErrors.BlockNotFound;
        }

        if (!currentUser.CanManagePsychologist(block.PsychologistId))
        {
            return SchedulingErrors.CannotManageAgenda;
        }

        db.Schedules.Remove(block);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
