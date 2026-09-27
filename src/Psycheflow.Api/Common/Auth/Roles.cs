namespace Psycheflow.Api.Common.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Psychologist = "Psychologist";

    /// <summary>Reservada para um futuro portal do paciente (D-01). Nenhum endpoint a aceita hoje.</summary>
    public const string Patient = "Patient";

    public static readonly IReadOnlyList<string> All = [Admin, Manager, Psychologist, Patient];

    /// <summary>Perfis que um Admin/Manager pode atribuir ao cadastrar um usuário da empresa.</summary>
    public static readonly IReadOnlyList<string> Assignable = [Admin, Manager, Psychologist];
}
