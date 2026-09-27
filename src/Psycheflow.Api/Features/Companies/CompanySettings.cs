using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Time;

namespace Psycheflow.Api.Features.Companies;

/// <summary>Configurações da empresa usadas pelas regras de agenda (RN-32) e financeiro (RN-51).</summary>
public sealed class CompanySettings
{
    public const int DefaultSessionDurationMinutes = 50;
    public const int MinSessionDurationMinutes = 15;
    public const int MaxSessionDurationMinutes = 240;

    private CompanySettings()
    {
    }

    public int SessionDurationMinutes { get; private set; }

    public decimal? SessionDefaultPrice { get; private set; }

    public string TimeZone { get; private set; } = ClinicClock.DefaultTimeZone;

    public static CompanySettings Default() => new()
    {
        SessionDurationMinutes = DefaultSessionDurationMinutes,
        TimeZone = ClinicClock.DefaultTimeZone,
    };

    public Result Update(int sessionDurationMinutes, decimal? sessionDefaultPrice, string timeZone)
    {
        if (sessionDurationMinutes is < MinSessionDurationMinutes or > MaxSessionDurationMinutes)
        {
            return CompanySettingsErrors.InvalidSessionDuration;
        }

        if (sessionDefaultPrice is < 0)
        {
            return CompanySettingsErrors.InvalidPrice;
        }

        if (!ClinicClock.IsValidTimeZone(timeZone))
        {
            return CompanySettingsErrors.InvalidTimeZone;
        }

        SessionDurationMinutes = sessionDurationMinutes;
        SessionDefaultPrice = sessionDefaultPrice;
        TimeZone = timeZone;
        return Result.Success();
    }
}

public static class CompanySettingsErrors
{
    public static readonly Error InvalidSessionDuration = Error.Validation(
        "settings.invalid_session_duration",
        $"A duração da sessão deve ficar entre {CompanySettings.MinSessionDurationMinutes} e {CompanySettings.MaxSessionDurationMinutes} minutos.",
        "sessionDurationMinutes");

    public static readonly Error InvalidPrice = Error.Validation(
        "settings.invalid_price", "O valor padrão da sessão não pode ser negativo.", "sessionDefaultPrice");

    public static readonly Error InvalidTimeZone = Error.Validation(
        "settings.invalid_time_zone", "Fuso horário inválido. Use um identificador IANA, ex.: America/Sao_Paulo.", "timeZone");
}
