namespace Psycheflow.Api.Features.Scheduling;

public enum ScheduleStatus
{
    Pending = 0,
    Confirmed = 1,

    /// <summary>Libera o horário para novos agendamentos.</summary>
    Cancelled = 2,
}
