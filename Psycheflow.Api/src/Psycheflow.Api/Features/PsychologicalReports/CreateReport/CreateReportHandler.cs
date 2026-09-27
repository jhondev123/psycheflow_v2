using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Patients;

namespace Psycheflow.Api.Features.PsychologicalReports.CreateReport;

/// <summary>UC17 / RF016: cria o rascunho do laudo/relatório do paciente (autor = psicólogo logado).</summary>
public sealed class CreateReportHandler(AppDbContext db, PsychologicalReportAccess access)
{
    public async Task<Result<PsychologicalReportResponse>> Handle(CreateReportRequest request, CancellationToken cancellationToken)
    {
        Result<Guid> psychologistId = access.RequirePsychologist();
        if (psychologistId.IsFailure)
        {
            return psychologistId.Error;
        }

        Patient? patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.Id == request.PatientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        var report = PsychologicalReport.CreateDraft(
            patient.Id, psychologistId.Value, request.Template!.Value, request.ToSections(), request.IncludeSessionSummary);
        db.PsychologicalReports.Add(report);
        await db.SaveChangesAsync(cancellationToken);

        return PsychologicalReportResponse.From(report, patient.FullName);
    }
}
