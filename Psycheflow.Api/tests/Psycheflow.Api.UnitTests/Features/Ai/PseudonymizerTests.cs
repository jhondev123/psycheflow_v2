using Psycheflow.Api.Features.Ai.Assistant;

namespace Psycheflow.Api.UnitTests.Features.Ai;

public sealed class PseudonymizerTests
{
    private static readonly PatientIdentity Mariana = new(
        FullName: "Mariana Júlia de Souza",
        Cpf: "52998224725",
        Email: "mariana.souza@exemplo.com",
        Phone: "45988887777",
        Street: "Rua Paraná");

    [Fact]
    public void Apply_FullNameAndItsParts_AreReplacedIgnoringCaseAndAccents()
    {
        string text = "Mariana Júlia de Souza chegou ansiosa. mariana contou que a mãe a chama de JULIA; a Sra. Souza pediu retorno.";

        string result = Pseudonymizer.Apply(text, Mariana);

        result.ShouldNotContain("Mariana", Case.Insensitive);
        result.ShouldNotContain("Júlia", Case.Insensitive);
        result.ShouldNotContain("Julia", Case.Insensitive);
        result.ShouldNotContain("Souza", Case.Insensitive);
        result.ShouldStartWith($"{Pseudonymizer.PatientPlaceholder} chegou ansiosa.");
        result.ShouldContain("a mãe a chama de [paciente]");
    }

    [Fact]
    public void Apply_NamePartsInsideOtherWordsAndShortParticles_AreKept()
    {
        var identity = new PatientIdentity("Ana Lima", Cpf: null, Email: null, Phone: null, Street: null);

        string result = Pseudonymizer.Apply("Ana tem ansiedade de dia e mora em Limeira.", identity);

        result.ShouldBe("[paciente] tem ansiedade de dia e mora em Limeira.");
    }

    [Theory]
    [InlineData("CPF 529.982.247-25 informado.", "CPF [cpf] informado.")]
    [InlineData("CPF 52998224725 informado.", "CPF [cpf] informado.")]
    [InlineData("Escreveu de mariana.souza@exemplo.com ontem.", "Escreveu de [email] ontem.")]
    [InlineData("Ligar no (45) 98888-7777 ou 45 3222-1111.", "Ligar no [telefone] ou [telefone].")]
    [InlineData("Mora no CEP 85810-000.", "Mora no CEP [cep].")]
    [InlineData("Endereço: rua paraná, 1200.", "Endereço: [endereço], 1200.")]
    public void Apply_ContactAndDocumentData_AreMasked(string text, string expected) =>
        Pseudonymizer.Apply(text, Mariana).ShouldBe(expected);

    [Fact]
    public void Apply_DatesTimesScoresAndAmounts_AreKept()
    {
        const string text = "Sessão de 05/10/2026 às 14:00 (2026-10-05), nota 8/10, valor R$ 150,00, 50 minutos.";

        Pseudonymizer.Apply(text, Mariana).ShouldBe(text);
    }
}
