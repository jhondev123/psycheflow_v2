namespace Psycheflow.Api.Common.Auth;

/// <summary>Nomes das claims do JWT (nomes curtos, sem o mapeamento legado do WS-Federation).</summary>
public static class AuthClaims
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";
    public const string CompanyId = "company_id";
    public const string PsychologistId = "psychologist_id";
    public const string MustChangePassword = "must_change_password";
}
