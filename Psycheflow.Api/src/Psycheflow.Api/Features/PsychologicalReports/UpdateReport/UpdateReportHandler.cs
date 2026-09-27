using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.PsychologicalReports.UpdateReport;

public sealed class UpdateReportHandler(AppDbContext db, PsychologicalReportAccess access)
{
    public async Task<Result<PsychologicalReportResponse>> Handle(Guid id, UpdateReportRequest request, CancellationToken cancellationToken)
    {
        Result<(PsychologicalReport Report, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        (PsychologicalReport report, string patientName) = found.Value;
        Result updated = report.Update(request.Template!.Value, request.ToSections(), request.IncludeSessionSummary);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return PsychologicalReportResponse.From(report, patientName);
    }
}
