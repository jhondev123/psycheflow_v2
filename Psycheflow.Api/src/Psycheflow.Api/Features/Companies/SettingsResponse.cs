namespace Psycheflow.Api.Features.Companies;

public sealed record SettingsResponse(int SessionDurationMinutes, decimal? SessionDefaultPrice, string TimeZone)
{
    public static SettingsResponse From(CompanySettings settings) =>
        new(settings.SessionDurationMinutes, settings.SessionDefaultPrice, settings.TimeZone);
}
