using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions.ListSessions;

/// <summary>UC05 / RF005: sessões por período (início obrigatório), horário, paciente e status.</summary>
public sealed class ListSessionsHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PagedResponse<SessionListItem>>> Handle(ListSessionsQuery query, CancellationToken cancellationToken)
    {
        IQueryable<Session> sessions = db.Sessions.AsNoTracking();

        if (query.PsychologistId is { } psychologistId)
        {
            if (!currentUser.CanManagePsychologist(psychologistId))
            {
                return SchedulingErrors.CannotManageAgenda;
            }

            sessions = sessions.Where(s => s.PsychologistId == psychologistId);
        }
        else if (!currentUser.IsManagement())
        {
            // Psicólogo sem papel de gestão: apenas a própria agenda (D-02).
            Guid? own = currentUser.PsychologistId;
            sessions = sessions.Where(s => s.PsychologistId == own);
        }

        DateOnly from = query.From!.Value;
        sessions = sessions.Where(s => s.Schedule.Date >= from);

        if (query.To is { } to)
        {
            sessions = sessions.Where(s => s.Schedule.Date <= to);
        }

        if (query.TimeFrom is { } timeFrom)
        {
            sessions = sessions.Where(s => s.Schedule.StartTime >= timeFrom);
        }

        if (query.TimeTo is { } timeTo)
        {
            sessions = sessions.Where(s => s.Schedule.StartTime <= timeTo);
        }

        if (query.PatientId is { } patientId)
        {
            sessions = sessions.Where(s => s.PatientId == patientId);
        }

        if (query.Status is { } status)
        {
            sessions = sessions.Where(s => s.Status == status);
        }

        return await sessions
            .OrderBy(s => s.Schedule.Date)
            .ThenBy(s => s.Schedule.StartTime)
            .Select(s => new SessionListItem(
                s.Id,
                s.PsychologistId,
                s.Psychologist!.User!.FullName,
                s.PatientId,
                s.Patient!.FullName,
                s.Schedule.Date,
                s.Schedule.StartTime,
                s.Schedule.EndTime,
                s.Status,
                s.Schedule.Status))
            .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
    }
}
