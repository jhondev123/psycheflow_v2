using Bogus;
using Bogus.Extensions.Brazil;

namespace Psycheflow.Api.IntegrationTests.Infrastructure;

public static class TestPatients
{
    private static readonly Faker Faker = new("pt_BR");

    /// <summary>CPF válido e aleatório, só dígitos.</summary>
    public static string Cpf() => new Person("pt_BR").Cpf(includeFormatSymbols: false);

    public static object Payload(string? cpf = null, string? fullName = null) => new
    {
        fullName = fullName ?? Faker.Name.FullName(),
        cpf = cpf ?? Cpf(),
        email = Faker.Internet.Email(),
        phone = "(45) 98888-7777",
        birthDate = "1990-05-17",
        address = new
        {
            zipCode = "85810-000",
            street = "Rua Paraná",
            number = "1200",
            complement = "Sala 3",
            neighborhood = "Centro",
            city = "Cascavel",
            state = "PR",
        },
        notes = "Encaminhado pela UBS.",
    };
}
