using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Scheduling;

/// <summary>Intervalo de horário num dia, no horário local da clínica. Sempre com fim depois do início (RN-31).</summary>
public readonly record struct TimeSlot
{
    public static readonly TimeOnly EndOfDay = new(23, 59);

    private TimeSlot(DateOnly date, TimeOnly start, TimeOnly end)
    {
        Date = date;
        Start = start;
        End = end;
    }

    public DateOnly Date { get; }

    public TimeOnly Start { get; }

    public TimeOnly End { get; }

    public int DurationMinutes => (int)(End - Start).TotalMinutes;

    public static Result<TimeSlot> Create(DateOnly date, TimeOnly start, TimeOnly end) =>
        end > start ? new TimeSlot(date, start, end) : SchedulingErrors.InvalidRange;

    public static Result<TimeSlot> FromDuration(DateOnly date, TimeOnly start, int durationMinutes)
    {
        TimeOnly end = start.Add(TimeSpan.FromMinutes(durationMinutes), out int wrappedDays);
        return wrappedDays > 0 || end == TimeOnly.MinValue
            ? SchedulingErrors.CrossesMidnight
            : Create(date, start, end);
    }

    /// <summary>Bloqueio de dia inteiro (RN-39): 00:00–23:59.</summary>
    public static TimeSlot WholeDay(DateOnly date) => new(date, TimeOnly.MinValue, EndOfDay);

    internal static TimeSlot FromStorage(DateOnly date, TimeOnly start, TimeOnly end) => new(date, start, end);

    /// <summary>RN-34: intervalos do mesmo dia que se cruzam (encostar na borda não conflita).</summary>
    public bool Overlaps(TimeSlot other) => Date == other.Date && Start < other.End && other.Start < End;

    public bool StartsBefore(DateTime localDateTime) => Date.ToDateTime(Start) < localDateTime;

    public bool EndsBefore(DateTime localDateTime) => Date.ToDateTime(End) <= localDateTime;
}
