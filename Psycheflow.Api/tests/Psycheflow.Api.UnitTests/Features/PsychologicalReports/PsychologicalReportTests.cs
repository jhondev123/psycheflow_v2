using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.PsychologicalReports;

namespace Psycheflow.Api.UnitTests.Features.PsychologicalReports;

public sealed class PsychologicalReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private static PsychologicalReport Draft() => PsychologicalReport.CreateDraft(
        Guid.CreateVersion7(), Guid.CreateVersion7(), PsychologicalReportTemplate.PsychologicalReport,
        new ReportSections("Finalidade", "Demanda", null, null, null), includeSessionSummary: false);

    private static readonly ReportSections Complete = new("Finalidade", "Demanda", "Procedimento", "Análise", "Conclusão");

    [Fact]
    public void Finalize_WithMissingSections_ReportsTheFirstMissingField()
    {
        Result result = Draft().Finalize(Now);

        result.Error!.Field.ShouldBe("procedure");
    }

    [Fact]
    public void Finalize_Complete_LocksTheReport()
    {
        PsychologicalReport report = Draft();
        report.Update(PsychologicalReportTemplate.PsychologicalReport, Complete, includeSessionSummary: true).IsSuccess.ShouldBeTrue();

        report.Finalize(Now).IsSuccess.ShouldBeTrue();

        report.Status.ShouldBe(PsychologicalReportStatus.Finalized);
        report.FinalizedAt.ShouldBe(Now);
        report.Update(PsychologicalReportTemplate.PsychologicalStatement, Complete, false).Error.ShouldBe(PsychologicalReportErrors.AlreadyFinalized);
        report.Finalize(Now).Error.ShouldBe(PsychologicalReportErrors.AlreadyFinalized);
    }

    [Fact]
    public void Sections_AreTrimmedAndBlankBecomesNull()
    {
        PsychologicalReport report = Draft();

        report.Update(PsychologicalReportTemplate.PsychologicalReport, new ReportSections(" Finalidade ", "  ", null, null, null), false);

        report.Sections.Purpose.ShouldBe("Finalidade");
        report.Sections.Demand.ShouldBeNull();
    }
}
