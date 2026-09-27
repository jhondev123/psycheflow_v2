using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Dashboard.GetDashboard;

public sealed record DashboardQuery(Guid? PsychologistId = null);

/// <summary>
/// RC-06: números do dia/semana, agenda de hoje, próximos atendimentos e financeiro. Psicólogo vê só a própria agenda;
/// Admin/Manager veem a clínica inteira ou filtram por psicólogo.
/// </summary>
public sealed class GetDashboardHandler(AppDbContext db, ICurrentUser currentUser, ClinicClock clock)
{
    public const int UpcomingLimit = 6;

    public async Task<Result<DashboardResponse>> Handle(DashboardQuery query, CancellationToken cancellationToken)
    {
        Guid? psychologistId = query.PsychologistId;
        if (psychologistId is { } requested)
        {
            if (!currentUser.CanManagePsychologist(requested))
            {
                return SchedulingErrors.CannotManageAgenda;
            }
        }
        else if (!currentUser.IsManagement())
        {
            psychologistId = currentUser.PsychologistId;
        }

        CompanySettings settings = await db.GetCurrentSettingsAsync(currentUser, cancellationToken);
        DateTime now = clock.LocalNow(settings.TimeZone);
        DashboardPeriod period = DashboardPeriod.For(DateOnly.FromDateTime(now));
        DateOnly today = period.Today;
        var currentTime = TimeOnly.FromDateTime(now);

        IQueryable<Session> sessions = db.Sessions.AsNoTracking();
        IQueryable<Schedule> schedules = db.Schedules.AsNoTracking();
        IQueryable<Payment> payments = db.Payments.AsNoTracking();
        if (psychologistId is { } id)
        {
            sessions = sessions.Where(s => s.PsychologistId == id);
            schedules = schedules.Where(s => s.PsychologistId == id);
            payments = payments.Where(p => p.Session!.PsychologistId == id);
        }

        IQueryable<Session> notCancelled = sessions.Where(s => s.Status != SessionStatus.Cancelled);

        var pending = await payments
            .Where(p => p.Status == PaymentStatus.Pending && p.Session!.Status == SessionStatus.Completed)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Amount = g.Sum(p => p.Amount) })
            .SingleOrDefaultAsync(cancellationToken);

        decimal received = await payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaidAt >= period.MonthStart && p.PaidAt <= period.MonthEnd)
            .SumAsync(p => p.Amount, cancellationToken);

        return new DashboardResponse(
            today,
            period.WeekStart,
            period.WeekEnd,
            await db.Patients.CountAsync(p => p.Status == PatientStatus.Active, cancellationToken),
            await notCancelled.CountAsync(s => s.Schedule.Date == today, cancellationToken),
            await notCancelled.CountAsync(s => s.Schedule.Date == today && s.Schedule.Status == ScheduleStatus.Confirmed, cancellationToken),
            await notCancelled.CountAsync(s => s.Schedule.Date >= period.WeekStart && s.Schedule.Date <= period.WeekEnd, cancellationToken),
            await sessions.CountAsync(
                s => s.Status == SessionStatus.Scheduled && s.Schedule.Status == ScheduleStatus.Pending && s.Schedule.Date >= today,
                cancellationToken),
            new DashboardFinance(pending?.Count ?? 0, pending?.Amount ?? 0, received),
            await LoadTodayAsync(schedules, today, cancellationToken),
            await sessions
                .Where(s => s.Status == SessionStatus.Scheduled
                    && (s.Schedule.Date > today || (s.Schedule.Date == today && s.Schedule.StartTime >= currentTime)))
                .OrderBy(s => s.Schedule.Date).ThenBy(s => s.Schedule.StartTime)
                .Take(UpcomingLimit)
                .Select(s => new DashboardUpcomingSession(
                    s.Id, s.PsychologistId, s.PatientId, s.Patient!.FullName, s.Schedule.Date, s.Schedule.StartTime, s.Schedule.EndTime, s.Schedule.Status))
                .ToListAsync(cancellationToken));
    }

    private Task<List<DashboardAgendaItem>> LoadTodayAsync(IQueryable<Schedule> schedules, DateOnly today, CancellationToken cancellationToken) =>
        schedules
            .Where(s => s.Date == today && s.Status != ScheduleStatus.Cancelled)
            .OrderBy(s => s.StartTime).ThenBy(s => s.PsychologistId)
            .Select(s => new
            {
                Schedule = s,
                Session = db.Sessions
                    .Where(x => x.ScheduleId == s.Id)
                    .Select(x => new { x.Id, x.PatientId, PatientName = x.Patient!.FullName, x.Status })
                    .FirstOrDefault(),
            })
            .Select(row => new DashboardAgendaItem(
                row.Schedule.Id,
                row.Schedule.PsychologistId,
                row.Schedule.Type,
                row.Schedule.Status,
                row.Schedule.StartTime,
                row.Schedule.EndTime,
                row.Schedule.BlockReason,
                row.Session == null ? null : row.Session.Id,
                row.Session == null ? null : row.Session.PatientId,
                row.Session == null ? null : row.Session.PatientName,
                row.Session == null ? null : row.Session.Status))
            .ToListAsync(cancellationToken);
}
