namespace Psycheflow.Api.Features.Recurrences;

public sealed record RecurrenceResponse(
    Guid Id,
    Guid PatientId,
    Guid PsychologistId,
    RecurrenceType Type,
    DateOnly StartDate,
    DateOnly? EndDate,
    TimeOnly StartTime,
    int DurationMinutes,
    decimal Price,
    DateOnly GeneratedUntil,
    bool IsActive,
    string? EndReason)
{
    public static RecurrenceResponse From(Recurrence recurrence) => new(
        recurrence.Id,
        recurrence.PatientId,
        recurrence.PsychologistId,
        recurrence.Type,
        recurrence.StartDate,
        recurrence.EndDate,
        recurrence.StartTime,
        recurrence.DurationMinutes,
        recurrence.Price,
        recurrence.GeneratedUntil,
        recurrence.IsActive,
        recurrence.EndReason);
}

/// <summary>Data que não pôde ser agendada e o motivo (código e mensagem da regra que impediu).</summary>
public sealed record SkippedOccurrence(DateOnly Date, string Code, string Reason);

/// <param name="Scheduled">Datas em que as sessões foram criadas nesta geração.</param>
/// <param name="Skipped">Datas puladas (conflito, fora do expediente, passado).</param>
public sealed record RecurrenceGenerationResponse(
    RecurrenceResponse Recurrence,
    IReadOnlyList<DateOnly> Scheduled,
    IReadOnlyList<SkippedOccurrence> Skipped);
