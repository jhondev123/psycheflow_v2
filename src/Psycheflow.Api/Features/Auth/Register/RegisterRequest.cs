using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Auth.Register;

/// <summary>Criação de conta: a empresa e o usuário responsável (Admin + Psicólogo).</summary>
public sealed record RegisterRequest(
    string CompanyName,
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    ApproachType Approach = ApproachType.NotInformed,
    string? Phone = null);
