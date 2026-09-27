using Psycheflow.Api.Features.Patients;

namespace Psycheflow.Api.UnitTests.Features.Patients;

public sealed class CpfTests
{
    [Theory]
    [InlineData("529.982.247-25", "52998224725")]
    [InlineData("52998224725", "52998224725")]
    [InlineData("111.444.777-35", "11144477735")]
    public void Create_ValidCpf_StoresDigitsOnly(string input, string expected) =>
        Cpf.Create(input).Value.Value.ShouldBe(expected);

    [Theory]
    [InlineData("529.982.247-24")]
    [InlineData("111.111.111-11")]
    [InlineData("00000000000")]
    [InlineData("5299822472")]
    [InlineData("529982247255")]
    [InlineData("abc.def.ghi-jk")]
    [InlineData("")]
    [InlineData(null)]
    public void Create_InvalidCpf_Fails(string? input)
    {
        Cpf.Create(input).Error.ShouldBe(Cpf.Invalid);
        Cpf.IsValid(input).ShouldBeFalse();
    }
}
