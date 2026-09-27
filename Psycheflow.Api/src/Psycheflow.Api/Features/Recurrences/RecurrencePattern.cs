namespace Psycheflow.Api.Features.Recurrences;

public enum RecurrenceType
{
    /// <summary>Toda semana, no mesmo dia da semana da data inicial.</summary>
    Weekly = 0,

    /// <summary>Todo mês, no mesmo dia do mês da data inicial (ou no último dia, em meses mais curtos).</summary>
    Monthly = 1,
}

/// <summary>Cálculo puro das datas de uma recorrência (RN-48), independente de banco e agenda.</summary>
public static class RecurrencePattern
{
    /// <summary>Sessões são geradas em janelas de 3 meses (D-05).</summary>
    public const int WindowMonths = 3;

    /// <summary>Último dia (inclusive) da janela que começa em <paramref name="from"/>, limitado pela data final.</summary>
    public static DateOnly WindowEnd(DateOnly from, DateOnly? endDate)
    {
        DateOnly windowEnd = from.AddMonths(WindowMonths).AddDays(-1);
        return endDate is { } end && end < windowEnd ? end : windowEnd;
    }

    /// <summary>Datas da recorrência ancorada em <paramref name="anchor"/> dentro de [<paramref name="from"/>, <paramref name="until"/>].</summary>
    public static IReadOnlyList<DateOnly> Occurrences(RecurrenceType type, DateOnly anchor, DateOnly from, DateOnly until)
    {
        DateOnly start = from < anchor ? anchor : from;
        List<DateOnly> dates = [];

        if (type == RecurrenceType.Weekly)
        {
            int offset = (7 - ((start.DayNumber - anchor.DayNumber) % 7)) % 7;
            for (DateOnly date = start.AddDays(offset); date <= until; date = date.AddDays(7))
            {
                dates.Add(date);
            }

            return dates;
        }

        var firstOfAnchorMonth = new DateOnly(anchor.Year, anchor.Month, 1);
        for (int month = 0; ; month++)
        {
            DateOnly firstOfMonth = firstOfAnchorMonth.AddMonths(month);
            if (firstOfMonth > until)
            {
                break;
            }

            int day = Math.Min(anchor.Day, DateTime.DaysInMonth(firstOfMonth.Year, firstOfMonth.Month));
            DateOnly date = firstOfMonth.AddDays(day - 1);
            if (date >= start && date <= until)
            {
                dates.Add(date);
            }
        }

        return dates;
    }
}
