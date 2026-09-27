using FluentValidation;

namespace Psycheflow.Api.Features.PsychologicalReports.CreateReport;

/// <param name="Template">Laudo (PsychologicalReport) ou relatório psicológico (PsychologicalStatement).</param>
/// <param name="Purpose">Finalidade/motivo do documento (obrigatório, RN-62).</param>
/// <param name="IncludeSessionSummary">Acrescenta ao procedimento o número e o período das sessões realizadas.</param>
public sealed record CreateReportRequest(
    Guid PatientId,
    PsychologicalReportTemplate? Template,
    string? Purpose,
    string? Demand = null,
    string? Procedure = null,
    string? Analysis = null,
    string? Conclusion = null,
    bool IncludeSessionSummary = false) : IReportContent;

public sealed class CreateReportValidator : AbstractValidator<CreateReportRequest>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty().WithMessage("Informe o paciente.");
        Include(new ReportContentValidator());
    }
}
