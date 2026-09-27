using FluentValidation.Results;
using Psycheflow.Api.Features.Auth.Register;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.UnitTests.Features.Auth;

public sealed class RegisterValidatorTests
{
    private static readonly RegisterRequest Valid = new(
        CompanyName: "Clínica Viver",
        FullName: "Ana Souza",
        Email: "ana@clinica.com",
        Password: "Senha@123",
        LicenseNumber: "06/12345",
        Approach: ApproachType.Psychoanalysis,
        Phone: "(45) 99999-1234");

    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes() => _validator.Validate(Valid).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_PhoneIsOptional() => _validator.Validate(Valid with { Phone = null }).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_EmptyFields_ReportsEachField()
    {
        ValidationResult result = _validator.Validate(new RegisterRequest("", "", "", "", "", ApproachType.NotInformed, null));

        result.Errors.Select(e => e.PropertyName).Distinct()
            .ShouldBe(["CompanyName", "FullName", "Email", "Password", "LicenseNumber"], ignoreOrder: true);
    }

    [Fact]
    public void Validate_InvalidFormats_ReportsFields()
    {
        ValidationResult result = _validator.Validate(Valid with { Email = "x@", LicenseNumber = "123", Phone = "1" });

        result.Errors.Select(e => e.PropertyName).Distinct()
            .ShouldBe(["Email", "LicenseNumber", "Phone"], ignoreOrder: true);
    }

    [Fact]
    public void Validate_UndefinedApproach_Fails() =>
        _validator.Validate(Valid with { Approach = (ApproachType)99 }).Errors
            .ShouldContain(e => e.PropertyName == "Approach");
}
