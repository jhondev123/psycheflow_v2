using FluentValidation;

namespace Psycheflow.Api.Features.PsychologicalReports.UpdateReport;

public sealed record UpdateReportRequest(
    PsychologicalReportTemplate? Template,
    string? Purpose,
    string? Demand = null,
    string? Procedure = null,
    string? Analysis = null,
    string? Conclusion = null,
    bool IncludeSessionSummary = false) : IReportContent;

public sealed class UpdateReportValidator : AbstractValidator<UpdateReportRequest>
{
    public UpdateReportValidator() => Include(new ReportContentValidator());
}
