namespace Psycheflow.Api.Features.Users;

public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles,
    Guid? PsychologistId,
    bool MustChangePassword,
    bool IsLockedOut);
