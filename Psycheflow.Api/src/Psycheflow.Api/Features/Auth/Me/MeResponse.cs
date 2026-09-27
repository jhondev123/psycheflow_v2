namespace Psycheflow.Api.Features.Auth.Me;

public sealed record MeResponse(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    Guid CompanyId,
    string CompanyName,
    Guid? PsychologistId,
    bool MustChangePassword);
