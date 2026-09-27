using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.UnitTests.Features.Psychologists;

public sealed class LicenseNumberTests
{
    [Theory]
    [InlineData("06/12345")]
    [InlineData("06/1234")]
    [InlineData("12/123456")]
    [InlineData(" 06/12345 ")]
    public void Create_ValidCrp_Succeeds(string value)
    {
        Result<LicenseNumber> result = LicenseNumber.Create(value);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Value.ShouldBe(value.Trim());
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("6/12345")]
    [InlineData("06/123")]
    [InlineData("06/1234567")]
    [InlineData("06-12345")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidCrp_Fails(string? value)
    {
        LicenseNumber.Create(value).Error.ShouldBe(LicenseNumber.Invalid);
        LicenseNumber.IsValid(value).ShouldBeFalse();
    }
}
