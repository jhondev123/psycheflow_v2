using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Users.CreateUser;

namespace Psycheflow.Api.UnitTests.Features.Users;

public sealed class CreateUserValidatorTests
{
    private readonly CreateUserValidator _validator = new();

    [Theory]
    [InlineData(Roles.Admin)]
    [InlineData(Roles.Manager)]
    public void Validate_NonPsychologistRole_DoesNotRequireLicense(string role) =>
        _validator.Validate(new CreateUserRequest("Fulano", "fulano@clinica.com", role, null, null)).IsValid.ShouldBeTrue();

    [Fact]
    public void Validate_PsychologistWithoutLicense_Fails() =>
        _validator.Validate(new CreateUserRequest("Fulano", "fulano@clinica.com", Roles.Psychologist, null, null))
            .Errors.ShouldContain(e => e.PropertyName == "LicenseNumber");

    [Theory]
    [InlineData(Roles.Patient)]
    [InlineData("Root")]
    [InlineData("")]
    public void Validate_RoleNotAssignable_Fails(string role) =>
        _validator.Validate(new CreateUserRequest("Fulano", "fulano@clinica.com", role, null, null))
            .Errors.ShouldContain(e => e.PropertyName == "Role");
}
