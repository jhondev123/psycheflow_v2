using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.Features.Documents.SessionsReport;

/// <summary>
/// UC15 / RF015 / RN-63: relatório de sessões por período, status da sessão e status do pagamento.
/// As observações (anotações) só entram para as sessões do próprio solicitante (sigilo, D-02).
/// </summary>
public sealed class SessionsReportHandler(AppDbContext db, ICurrentUser currentUser, DocumentHeaderFactory headers)
{
    private const string All = "Todos";

    public async Task<Result<PdfFile>> Handle(SessionsReportQuery query, CancellationToken cancellationToken)
    {
        Guid? psychologistId = query.PsychologistId;
        if (psychologistId is { } requested && !currentUser.CanManagePsychologist(requested))
        {
            return SchedulingErrors.CannotManageAgenda;
        }

        if (psychologistId is null && !currentUser.IsManagement())
        {
            psychologistId = currentUser.PsychologistId;
        }

        DateOnly from = query.From!.Value;
        IQueryable<Session> sessions = db.Sessions
            .AsNoTracking()
            .Include(s => s.Schedule)
            .Include(s => s.Patient)
            .Include(s => s.Payment)
            .Where(s => s.Schedule.Date >= from);

        if (psychologistId is { } id)
        {
            sessions = sessions.Where(s => s.PsychologistId == id);
        }

        if (query.To is { } to)
        {
            sessions = sessions.Where(s => s.Schedule.Date <= to);
        }

        if (query.SessionStatus is { } sessionStatus)
        {
            sessions = sessions.Where(s => s.Status == sessionStatus);
        }

        if (query.PaymentStatus is { } paymentStatus)
        {
            sessions = sessions.Where(s => s.Payment != null && s.Payment.Status == paymentStatus);
        }

        List<Session> list = await sessions
            .OrderBy(s => s.Schedule.Date)
            .ThenBy(s => s.Schedule.StartTime)
            .ToListAsync(cancellationToken);

        Psychologist? professional = psychologistId is { } pid ? await db.FindPsychologistAsync(pid, cancellationToken) : null;

        var model = new SessionsReportModel(
            await headers.CreateAsync(professional, cancellationToken),
            from,
            query.To,
            query.SessionStatus is { } s1 ? DocumentLabels.Of(s1) : All,
            query.PaymentStatus is { } p1 ? DocumentLabels.Of(p1) : All,
            [
                .. list.Select(s => new SessionsReportRow(
                    s.Schedule.Date,
                    s.Schedule.StartTime,
                    s.Patient!.FullName,
                    s.Status,
                    s.PsychologistId == currentUser.PsychologistId ? s.Notes : null,
                    s.Payment?.Amount,
                    s.Payment?.Status,
                    s.Payment?.Method)),
            ]);

        byte[] pdf = new SessionsReportDocument(model).GeneratePdf();
        return new PdfFile(pdf, PdfFile.BuildFileName("relatorio-sessoes", from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
    }
}
