using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.PsychologicalReports;

/// <summary>Modelos de documento psicológico com a estrutura da Resolução CFP nº 06/2019.</summary>
public enum PsychologicalReportTemplate
{
    /// <summary>Laudo psicológico.</summary>
    PsychologicalReport = 0,

    /// <summary>Relatório psicológico.</summary>
    PsychologicalStatement = 1,
}

public enum PsychologicalReportStatus
{
    Draft = 0,
    Finalized = 1,
}

/// <summary>Seções do documento (CFP 06/2019): identificação/finalidade, demanda, procedimento, análise e conclusão.</summary>
public sealed record ReportSections(string? Purpose, string? Demand, string? Procedure, string? Analysis, string? Conclusion)
{
    public ReportSections Normalize() => new(Clean(Purpose), Clean(Demand), Clean(Procedure), Clean(Analysis), Clean(Conclusion));

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}

/// <summary>
/// Laudo ou relatório psicológico (RF016 / UC17 / RN-62). Rascunho editável; ao finalizar, todas as seções são
/// obrigatórias e o documento fica bloqueado. Sigiloso: só o psicólogo autor acessa.
/// </summary>
public sealed class PsychologicalReport : Entity, ITenantEntity, ISoftDeletable
{
    public const int SectionMaxLength = 10_000;

    private PsychologicalReport()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid PatientId { get; private set; }

    public Guid PsychologistId { get; private set; }

    public PsychologicalReportTemplate Template { get; private set; }

    public string? Purpose { get; private set; }

    public string? Demand { get; private set; }

    public string? Procedure { get; private set; }

    public string? Analysis { get; private set; }

    public string? Conclusion { get; private set; }

    /// <summary>Acrescenta ao procedimento um resumo das sessões realizadas (RN-62).</summary>
    public bool IncludeSessionSummary { get; private set; }

    public PsychologicalReportStatus Status { get; private set; }

    public DateTimeOffset? FinalizedAt { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public ReportSections Sections => new(Purpose, Demand, Procedure, Analysis, Conclusion);

    public static PsychologicalReport CreateDraft(
        Guid patientId, Guid psychologistId, PsychologicalReportTemplate template, ReportSections sections, bool includeSessionSummary)
    {
        var report = new PsychologicalReport
        {
            PatientId = patientId,
            PsychologistId = psychologistId,
            Status = PsychologicalReportStatus.Draft,
        };
        report.Apply(template, sections, includeSessionSummary);
        return report;
    }

    public Result Update(PsychologicalReportTemplate template, ReportSections sections, bool includeSessionSummary)
    {
        if (Status == PsychologicalReportStatus.Finalized)
        {
            return PsychologicalReportErrors.AlreadyFinalized;
        }

        Apply(template, sections, includeSessionSummary);
        return Result.Success();
    }

    public Result Finalize(DateTimeOffset now)
    {
        if (Status == PsychologicalReportStatus.Finalized)
        {
            return PsychologicalReportErrors.AlreadyFinalized;
        }

        (string? Value, string Field, string Label)[] required =
        [
            (Purpose, "purpose", "Finalidade"),
            (Demand, "demand", "Descrição da demanda"),
            (Procedure, "procedure", "Procedimento"),
            (Analysis, "analysis", "Análise"),
            (Conclusion, "conclusion", "Conclusão"),
        ];

        foreach ((string? value, string field, string label) in required)
        {
            if (value is null)
            {
                return PsychologicalReportErrors.SectionRequired(field, label);
            }
        }

        Status = PsychologicalReportStatus.Finalized;
        FinalizedAt = now;
        return Result.Success();
    }

    private void Apply(PsychologicalReportTemplate template, ReportSections sections, bool includeSessionSummary)
    {
        ReportSections clean = sections.Normalize();
        Template = template;
        Purpose = clean.Purpose;
        Demand = clean.Demand;
        Procedure = clean.Procedure;
        Analysis = clean.Analysis;
        Conclusion = clean.Conclusion;
        IncludeSessionSummary = includeSessionSummary;
    }
}

public static class PsychologicalReportErrors
{
    public static readonly Error NotFound = Error.NotFound("psychological_report.not_found", "Documento não encontrado.");

    public static readonly Error AlreadyFinalized = Error.Conflict(
        "psychological_report.already_finalized", "O documento já foi finalizado e não pode mais ser alterado.");

    public static readonly Error OnlyAuthor = Error.Forbidden(
        "psychological_report.only_author", "Somente o psicólogo autor pode acessar este documento.");

    public static readonly Error OnlyPsychologists = Error.Forbidden(
        "psychological_report.only_psychologists", "Somente psicólogos podem emitir laudos e relatórios psicológicos.");

    public static Error SectionRequired(string field, string label) => Error.Validation(
        "psychological_report.section_required", $"Preencha a seção \"{label}\" para finalizar o documento.", field);
}
