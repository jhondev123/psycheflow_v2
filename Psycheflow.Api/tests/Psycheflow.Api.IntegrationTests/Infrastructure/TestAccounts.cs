using Bogus;

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

public sealed record TestAccount(
    string AccessToken,
    Guid UserId,
    Guid CompanyId,
    Guid? PsychologistId,
    string Email,
    string Password);

/// <summary>Dados de teste para contas e usuários.</summary>
public static class TestAccounts
{
    public const string DefaultPassword = "Senha@123";

    private static readonly Faker Faker = new("pt_BR");

    public static string UniqueEmail() => $"{Guid.NewGuid():N}@teste.psycheflow.dev";

    public static string LicenseNumber() => $"06/{Faker.Random.Number(10000, 99999)}";

    public static string FullName() => Faker.Name.FullName();

    public static object RegisterPayload(string? email = null, string? password = null) => new
    {
        companyName = $"Clínica {Faker.Company.CompanyName()}",
        fullName = FullName(),
        email = email ?? UniqueEmail(),
        password = password ?? DefaultPassword,
        licenseNumber = LicenseNumber(),
        approach = "CognitiveBehavioral",
        phone = "(45) 99999-1234",
    };
}
