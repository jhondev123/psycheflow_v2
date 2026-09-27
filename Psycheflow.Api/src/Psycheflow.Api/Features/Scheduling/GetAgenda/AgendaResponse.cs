using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Scheduling.GetAgenda;

/// <param name="WorkingHours">Expediente de cada psicólogo exibido (para destacar horários livres).</param>
/// <param name="Items">Sessões e bloqueios do período (inclusive cancelados), ordenados por data e horário.</param>
public sealed record AgendaResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<PsychologistWorkingHours> WorkingHours,
    IReadOnlyList<AgendaItem> Items);

public sealed record PsychologistWorkingHours(Guid PsychologistId, string FullName, IReadOnlyList<WorkingHoursDto> Hours);

/// <summary>Item da agenda. Campos de sessão vêm nulos para bloqueios; anotações nunca aparecem aqui.</summary>
public sealed record AgendaItem(
    Guid ScheduleId,
    Guid PsychologistId,
    ScheduleType Type,
    ScheduleStatus Status,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? BlockReason,
    Guid? SessionId,
    Guid? PatientId,
    string? PatientName,
    SessionStatus? SessionStatus);
