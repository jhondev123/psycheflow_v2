namespace Psycheflow.Api.Features.PsychologicalReports;

public sealed record PsychologicalReportResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid PsychologistId,
    PsychologicalReportTemplate Template,
    string? Purpose,
    string? Demand,
    string? Procedure,
    string? Analysis,
    string? Conclusion,
    bool IncludeSessionSummary,
    PsychologicalReportStatus Status,
    DateTimeOffset? FinalizedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PsychologicalReportResponse From(PsychologicalReport report, string patientName) => new(
        report.Id,
        report.PatientId,
        patientName,
        report.PsychologistId,
        report.Template,
        report.Purpose,
        report.Demand,
        report.Procedure,
        report.Analysis,
        report.Conclusion,
        report.IncludeSessionSummary,
        report.Status,
        report.FinalizedAt,
        report.CreatedAt,
        report.UpdatedAt);
}
