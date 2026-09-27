namespace Psycheflow.Api.Common.Time;

/// <summary>
/// Converte o "agora" (UTC, vindo do <see cref="TimeProvider"/>) para o horário local da clínica.
/// A agenda é guardada em data/hora locais; comparações com o presente sempre passam por aqui.
/// </summary>
public sealed class ClinicClock(TimeProvider timeProvider)
{
    public const string DefaultTimeZone = "America/Sao_Paulo";

    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();

    public DateTime LocalNow(string timeZoneId)
    {
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime;
    }

    public DateOnly Today(string timeZoneId) => DateOnly.FromDateTime(LocalNow(timeZoneId));

    public static bool IsValidTimeZone(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
}
