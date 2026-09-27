using System.Globalization;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.Features.Documents.Attendance;

/// <summary>Declaração de comparecimento de uma sessão concluída.</summary>
public sealed class GetAttendanceHandler(SessionAccess sessions, DocumentHeaderFactory headers)
{
    public async Task<Result<PdfFile>> Handle(Guid sessionId, CancellationToken cancellationToken)
    {
        Result<Session> found = await sessions.FindManageableAsync(sessionId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Session session = found.Value;
        if (session.Status != SessionStatus.Completed)
        {
            return DocumentErrors.AttendanceRequiresCompletedSession;
        }

        var model = new AttendanceModel(
            await headers.CreateAsync(session.Psychologist, cancellationToken),
            session.Patient!.FullName,
            session.Patient.Cpf.Value,
            session.Schedule.Date,
            session.Schedule.StartTime,
            session.Schedule.EndTime);

        byte[] pdf = new AttendanceDocument(model).GeneratePdf();
        return new PdfFile(pdf, PdfFile.BuildFileName("declaracao-comparecimento", session.Schedule.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), session.Patient.FullName));
    }
}
