using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Scheduling.GetAgenda;

/// <summary>UC18 / RF017: sessões, bloqueios e expediente de um período (visões dia/semana/mês do front).</summary>
public sealed class GetAgendaHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<AgendaResponse>> Handle(AgendaQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Psychologist> psychologists = db.Psychologists.AsNoTracking().Include(p => p.User);

        if (query.PsychologistId is { } psychologistId)
        {
            if (!currentUser.CanManagePsychologist(psychologistId))
            {
                return SchedulingErrors.CannotManageAgenda;
            }

            psychologists = psychologists.Where(p => p.Id == psychologistId);
        }
        else if (!currentUser.IsManagement())
        {
            Guid? own = currentUser.PsychologistId;
            psychologists = psychologists.Where(p => p.Id == own);
        }

        List<Psychologist> shown = await psychologists.OrderBy(p => p.User!.FullName).ToListAsync(cancellationToken);
        List<Guid> ids = [.. shown.Select(p => p.Id)];
        DateOnly from = query.From!.Value;
        DateOnly to = query.To!.Value;

        List<AgendaItem> items = await db.Schedules
            .AsNoTracking()
            .Where(s => ids.Contains(s.PsychologistId) && s.Date >= from && s.Date <= to)
            .OrderBy(s => s.Date)
            .ThenBy(s => s.StartTime)
            .Select(s => new
            {
                Schedule = s,
                Session = db.Sessions
                    .Where(x => x.ScheduleId == s.Id)
                    .Select(x => new { x.Id, x.PatientId, PatientName = x.Patient!.FullName, x.Status })
                    .FirstOrDefault(),
            })
            .Select(row => new AgendaItem(
                row.Schedule.Id,
                row.Schedule.PsychologistId,
                row.Schedule.Type,
                row.Schedule.Status,
                row.Schedule.Date,
                row.Schedule.StartTime,
                row.Schedule.EndTime,
                row.Schedule.BlockReason,
                row.Session == null ? null : row.Session.Id,
                row.Session == null ? null : row.Session.PatientId,
                row.Session == null ? null : row.Session.PatientName,
                row.Session == null ? null : row.Session.Status))
            .ToListAsync(cancellationToken);

        List<PsychologistWorkingHours> workingHours =
        [
            .. shown.Select(p => new PsychologistWorkingHours(p.Id, p.User!.FullName, WorkingHoursDto.From(p.WorkingHours))),
        ];

        return new AgendaResponse(from, to, workingHours, items);
    }
}
