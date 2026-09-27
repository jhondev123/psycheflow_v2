using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.Features.Documents.FeedbackReport;

/// <summary>
/// UC16 / RF015-B / RN-64: feedbacks do paciente nas sessões concluídas com o psicólogo logado.
/// Feedback é dado clínico: só o próprio psicólogo emite (D-02).
/// </summary>
public sealed class FeedbackReportHandler(AppDbContext db, ICurrentUser currentUser, DocumentHeaderFactory headers)
{
    public async Task<Result<PdfFile>> Handle(FeedbackReportQuery query, CancellationToken cancellationToken)
    {
        if (currentUser.PsychologistId is not { } psychologistId)
        {
            return DocumentErrors.OnlyPsychologists;
        }

        Patient? patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.Id == query.PatientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        DateOnly from = query.From!.Value;
        IQueryable<Session> sessions = db.Sessions
            .AsNoTracking()
            .Where(s => s.PatientId == patient.Id
                && s.PsychologistId == psychologistId
                && s.Status == SessionStatus.Completed
                && s.FeedbackScore != null
                && s.Schedule.Date >= from);

        if (query.To is { } to)
        {
            sessions = sessions.Where(s => s.Schedule.Date <= to);
        }

        List<FeedbackReportRow> rows = await sessions
            .OrderBy(s => s.Schedule.Date)
            .ThenBy(s => s.Schedule.StartTime)
            .Select(s => new FeedbackReportRow(s.Schedule.Date, s.Schedule.StartTime, s.FeedbackScore!.Value, s.FeedbackComment))
            .ToListAsync(cancellationToken);

        Psychologist? psychologist = await db.FindPsychologistAsync(psychologistId, cancellationToken);
        var header = await headers.CreateAsync(psychologist, cancellationToken);

        var model = new FeedbackReportModel(
            header,
            patient.FullName,
            patient.Cpf.Value,
            AgeOn(patient.BirthDate, DateOnly.FromDateTime(header.IssuedAt.DateTime)),
            from,
            query.To,
            rows);

        byte[] pdf = new FeedbackReportDocument(model).GeneratePdf();
        return new PdfFile(pdf, PdfFile.BuildFileName("relatorio-feedback", patient.FullName));
    }

    private static int? AgeOn(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not { } birth)
        {
            return null;
        }

        int age = today.Year - birth.Year;
        return birth.AddYears(age) > today ? age - 1 : age;
    }
}
