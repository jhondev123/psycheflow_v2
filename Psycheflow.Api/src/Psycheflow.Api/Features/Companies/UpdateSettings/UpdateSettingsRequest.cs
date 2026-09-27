namespace Psycheflow.Api.Features.Companies.UpdateSettings;

public sealed record UpdateSettingsRequest(int SessionDurationMinutes, decimal? SessionDefaultPrice, string TimeZone);
