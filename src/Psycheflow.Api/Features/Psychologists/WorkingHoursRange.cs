namespace Psycheflow.Api.Features.Psychologists;

/// <summary>Faixa de atendimento do psicólogo num dia da semana (ex.: segunda, 08:00–12:00). Pertence ao <see cref="Psychologist"/>.</summary>
public sealed class WorkingHoursRange
{
    public WorkingHoursRange(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime)
    {
        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
    }

    private WorkingHoursRange()
    {
    }

    public DayOfWeek DayOfWeek { get; private set; }

    public TimeOnly StartTime { get; private set; }

    public TimeOnly EndTime { get; private set; }

    /// <summary>O intervalo [start, end] cabe inteiro nesta faixa.</summary>
    public bool Contains(TimeOnly start, TimeOnly end) => start >= StartTime && end <= EndTime;

    /// <summary>Faixas do mesmo dia que se sobrepõem (encostar na borda é permitido).</summary>
    public bool Overlaps(WorkingHoursRange other) =>
        DayOfWeek == other.DayOfWeek && StartTime < other.EndTime && other.StartTime < EndTime;
}
