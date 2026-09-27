using FluentValidation;
using Psycheflow.Api.Common.Validation;

namespace Psycheflow.Api.Features.PsychologicalReports;

/// <summary>Campos editáveis de um laudo/relatório (compartilhados entre criação e edição).</summary>
public interface IReportContent
{
    PsychologicalReportTemplate? Template { get; }

    string? Purpose { get; }

    string? Demand { get; }

    string? Procedure { get; }

    string? Analysis { get; }

    string? Conclusion { get; }
}

public sealed class ReportContentValidator : AbstractValidator<IReportContent>
{
    public ReportContentValidator()
    {
        RuleFor(x => x.Template).NotNull().WithMessage("Escolha o modelo do documento.").IsInEnum().WithMessage("Modelo inválido.");
        RuleFor(x => x.Purpose).RequiredText("a finalidade/motivo do documento", PsychologicalReport.SectionMaxLength);
        RuleFor(x => x.Demand).OptionalText(PsychologicalReport.SectionMaxLength);
        RuleFor(x => x.Procedure).OptionalText(PsychologicalReport.SectionMaxLength);
        RuleFor(x => x.Analysis).OptionalText(PsychologicalReport.SectionMaxLength);
        RuleFor(x => x.Conclusion).OptionalText(PsychologicalReport.SectionMaxLength);
    }
}

public static class ReportContentExtensions
{
    public static ReportSections ToSections(this IReportContent content) =>
        new(content.Purpose, content.Demand, content.Procedure, content.Analysis, content.Conclusion);
}
