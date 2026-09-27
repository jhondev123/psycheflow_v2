using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.UnitTests.Features.Companies;

public sealed class CompanySettingsTests
{
    [Fact]
    public void Default_Uses50MinutesAndSaoPaulo()
    {
        CompanySettings settings = CompanySettings.Default();

        settings.SessionDurationMinutes.ShouldBe(50);
        settings.SessionDefaultPrice.ShouldBeNull();
        settings.TimeZone.ShouldBe("America/Sao_Paulo");
    }

    [Theory]
    [InlineData(14)]
    [InlineData(0)]
    [InlineData(241)]
    public void Update_DurationOutOfRange_FailsOnDurationField(int minutes)
    {
        Result result = CompanySettings.Default().Update(minutes, null, "America/Sao_Paulo");

        result.Error.ShouldBe(CompanySettingsErrors.InvalidSessionDuration);
    }

    [Fact]
    public void Update_NegativePrice_Fails()
    {
        Result result = CompanySettings.Default().Update(50, -1m, "America/Sao_Paulo");

        result.Error.ShouldBe(CompanySettingsErrors.InvalidPrice);
    }

    [Fact]
    public void Update_UnknownTimeZone_Fails()
    {
        Result result = CompanySettings.Default().Update(50, null, "Brasil/Cascavel");

        result.Error.ShouldBe(CompanySettingsErrors.InvalidTimeZone);
    }

    [Fact]
    public void Update_ValidValues_Applies()
    {
        CompanySettings settings = CompanySettings.Default();

        Result result = settings.Update(15, 180m, "America/Manaus");

        result.IsSuccess.ShouldBeTrue();
        settings.SessionDurationMinutes.ShouldBe(15);
        settings.SessionDefaultPrice.ShouldBe(180m);
        settings.TimeZone.ShouldBe("America/Manaus");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CompanyCreate_BlankName_Fails(string name) =>
        Company.Create(name).Error.ShouldBe(CompanyErrors.InvalidName);

    [Fact]
    public void CompanyCreate_TrimsName() =>
        Company.Create("  Clínica Viver  ").Value.Name.ShouldBe("Clínica Viver");
}
