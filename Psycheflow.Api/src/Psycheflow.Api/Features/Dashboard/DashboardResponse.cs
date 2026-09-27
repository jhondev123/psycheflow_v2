using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Dashboard;

/// <summary>Semana (segunda a domingo) e mês de referência do painel, a partir do "hoje" da clínica.</summary>
public sealed record DashboardPeriod(DateOnly Today, DateOnly WeekStart, DateOnly WeekEnd, DateOnly MonthStart, DateOnly MonthEnd)
{
    public static DashboardPeriod For(DateOnly today)
    {
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        DateOnly weekStart = today.AddDays(-daysSinceMonday);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        return new DashboardPeriod(today, weekStart, weekStart.AddDays(6), monthStart, monthStart.AddMonths(1).AddDays(-1));
    }
}

/// <summary>Resumo do painel inicial (RC-06).</summary>
/// <param name="PendingConfirmations">Sessões de hoje em diante ainda não confirmadas.</param>
public sealed record DashboardResponse(
    DateOnly Today,
    DateOnly WeekStart,
    DateOnly WeekEnd,
    int ActivePatients,
    int SessionsToday,
    int ConfirmedToday,
    int SessionsThisWeek,
    int PendingConfirmations,
    DashboardFinance Finance,
    IReadOnlyList<DashboardAgendaItem> TodayItems,
    IReadOnlyList<DashboardUpcomingSession> Upcoming);

/// <param name="PendingPayments">Pagamentos pendentes de sessões já concluídas (a receber).</param>
/// <param name="ReceivedThisMonth">Soma dos pagamentos recebidos no mês corrente.</param>
public sealed record DashboardFinance(int PendingPayments, decimal PendingAmount, decimal ReceivedThisMonth);

public sealed record DashboardAgendaItem(
    Guid ScheduleId,
    Guid PsychologistId,
    ScheduleType Type,
    ScheduleStatus Status,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? BlockReason,
    Guid? SessionId,
    Guid? PatientId,
    string? PatientName,
    SessionStatus? SessionStatus);

public sealed record DashboardUpcomingSession(
    Guid SessionId,
    Guid PsychologistId,
    Guid PatientId,
    string PatientName,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    ScheduleStatus ScheduleStatus);
