using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Documents;
using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.Features.PsychologicalReports.ReportActions;

/// <summary>Gera o PDF do laudo/relatório; opcionalmente resume as sessões realizadas (RN-62).</summary>
public sealed class ReportPdfHandler(AppDbContext db, PsychologicalReportAccess access, DocumentHeaderFactory headers)
{
    public async Task<Result<PdfFile>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(PsychologicalReport Report, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        PsychologicalReport report = found.Value.Report;
        string cpf = await db.Patients.Where(p => p.Id == report.PatientId).Select(p => p.Cpf).SingleAsync(cancellationToken) is { } patientCpf
            ? patientCpf.Value
            : string.Empty;
        Psychologist? psychologist = await db.FindPsychologistAsync(report.PsychologistId, cancellationToken);

        var model = new PsychologicalReportModel(
            await headers.CreateAsync(psychologist, cancellationToken),
            report.Template,
            report.Status,
            found.Value.PatientName,
            cpf,
            report.Sections,
            report.IncludeSessionSummary ? await SessionSummaryAsync(report, cancellationToken) : null);

        byte[] pdf = new PsychologicalReportDocument(model).GeneratePdf();
        return new PdfFile(pdf, PdfFile.BuildFileName(PsychologicalReportDocument.TitleOf(report.Template), found.Value.PatientName));
    }

    private async Task<string> SessionSummaryAsync(PsychologicalReport report, CancellationToken cancellationToken)
    {
        List<DateOnly> dates = await db.Sessions
            .Where(s => s.PatientId == report.PatientId && s.PsychologistId == report.PsychologistId && s.Status == SessionStatus.Completed)
            .Select(s => s.Schedule.Date)
            .ToListAsync(cancellationToken);

        return dates.Count switch
        {
            0 => "Não há sessões concluídas registradas com este paciente.",
            1 => $"Foi realizada 1 sessão, em {PdfLayout.Date(dates[0])}.",
            _ => $"Foram realizadas {dates.Count} sessões, entre {PdfLayout.Date(dates.Min())} e {PdfLayout.Date(dates.Max())}.",
        };
    }
}
