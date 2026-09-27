using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.UnitTests.Common;

public sealed class PhoneTests
{
    [Theory]
    [InlineData("(45) 99999-1234", "45999991234")]
    [InlineData("45999991234", "45999991234")]
    [InlineData("(45) 3222-1234", "4532221234")]
    [InlineData("45 3222 1234", "4532221234")]
    public void Create_ValidBrazilianPhone_StoresDigitsOnly(string input, string expected) =>
        Phone.Create(input).Value.Value.ShouldBe(expected);

    [Theory]
    [InlineData("123")]
    [InlineData("(00) 99999-1234")]
    [InlineData("(45) 99999-12345")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidPhone_Fails(string? input)
    {
        Phone.Create(input).Error.ShouldBe(Phone.Invalid);
        Phone.IsValid(input).ShouldBeFalse();
    }
}
