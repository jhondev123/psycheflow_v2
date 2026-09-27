using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Users.CreateUser;

/// <param name="Role">Admin, Manager ou Psychologist.</param>
/// <param name="LicenseNumber">CRP — obrigatório quando o perfil é Psychologist.</param>
public sealed record CreateUserRequest(
    string FullName,
    string Email,
    string Role,
    string? LicenseNumber = null,
    ApproachType? Approach = null);
