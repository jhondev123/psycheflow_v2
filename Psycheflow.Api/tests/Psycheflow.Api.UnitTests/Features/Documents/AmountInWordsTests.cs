using Psycheflow.Api.Features.Documents.Pdf;

namespace Psycheflow.Api.UnitTests.Features.Documents;

public sealed class AmountInWordsTests
{
    [Theory]
    [InlineData(0, "zero reais")]
    [InlineData(1, "um real")]
    [InlineData(2, "dois reais")]
    [InlineData(15, "quinze reais")]
    [InlineData(100, "cem reais")]
    [InlineData(101, "cento e um reais")]
    [InlineData(150, "cento e cinquenta reais")]
    [InlineData(180.5, "cento e oitenta reais e cinquenta centavos")]
    [InlineData(0.01, "um centavo")]
    [InlineData(1000, "mil reais")]
    [InlineData(1250, "mil duzentos e cinquenta reais")]
    [InlineData(2001, "dois mil e um reais")]
    [InlineData(21345.99, "vinte e um mil trezentos e quarenta e cinco reais e noventa e nove centavos")]
    [InlineData(1000000, "um milhão de reais")]
    public void ToPortuguese_WritesCurrencyInFull(double amount, string expected) =>
        AmountInWords.ToPortuguese((decimal)amount).ShouldBe(expected);

    [Fact]
    public void ToPortuguese_Negative_Throws() =>
        Should.Throw<ArgumentOutOfRangeException>(() => AmountInWords.ToPortuguese(-1m));
}
