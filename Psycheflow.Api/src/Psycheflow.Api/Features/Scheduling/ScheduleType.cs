namespace Psycheflow.Api.Features.Scheduling;

/// <summary>Tipo do item de agenda.</summary>
public enum ScheduleType
{
    /// <summary>Atendimento (sempre ligado a uma sessão).</summary>
    Session = 0,

    /// <summary>Reserva que impede agendamentos (RN-37).</summary>
    Block = 1,
}
