using FluentValidation.Results;
using Microsoft.Extensions.Time.Testing;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Patients.CreatePatient;

namespace Psycheflow.Api.UnitTests.Features.Patients;

public sealed class PatientTests
{
    private static readonly AddressDto ValidAddress = new("85810-000", "Rua Paraná", "1200", null, "Centro", "Cascavel", "pr");

    [Fact]
    public void Create_NewPatient_IsActiveAndNormalized()
    {
        var patient = Patient.Create(
            " Maria  ", Cpf.Create("529.982.247-25").Value, " Maria@Email.COM ", Phone.Create("45988887777").Value,
            new DateOnly(1990, 1, 1), Address.From(ValidAddress), notes: "  ");

        patient.FullName.ShouldBe("Maria");
        patient.Email.ShouldBe("maria@email.com");
        patient.Status.ShouldBe(PatientStatus.Active);
        patient.Notes.ShouldBeNull();
        patient.Address!.ZipCode.ShouldBe("85810000");
        patient.Address.State.ShouldBe("PR");
    }

    [Fact]
    public void Address_NullDto_IsNull() => Address.From(null).ShouldBeNull();

    [Fact]
    public void Validator_BirthDateInTheFuture_Fails()
    {
        var validator = new CreatePatientValidator(new FakeTimeProvider(new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.Zero)));
        var request = new CreatePatientRequest("Ana", "52998224725", "ana@email.com", "45988887777", new DateOnly(2026, 10, 6), null, null);

        ValidationResult result = validator.Validate(request);

        result.Errors.Single().PropertyName.ShouldBe("BirthDate");
    }

    [Theory]
    [InlineData("85810-000", true)]
    [InlineData("85810000", true)]
    [InlineData("8581000", false)]
    [InlineData("ABCDE-FGH", false)]
    public void Validator_ZipCode_Needs8Digits(string zipCode, bool valid)
    {
        var validator = new CreatePatientValidator(TimeProvider.System);
        var request = new CreatePatientRequest(
            "Ana", "52998224725", "ana@email.com", "45988887777", null, ValidAddress with { ZipCode = zipCode }, null);

        validator.Validate(request).IsValid.ShouldBe(valid);
    }
}
