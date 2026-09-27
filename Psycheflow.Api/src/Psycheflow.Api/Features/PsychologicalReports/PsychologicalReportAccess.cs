using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.PsychologicalReports;

/// <summary>Documentos psicológicos são do psicólogo autor (sigilo — D-02, CFP).</summary>
public sealed class PsychologicalReportAccess(AppDbContext db, ICurrentUser currentUser)
{
    public Result<Guid> RequirePsychologist() =>
        currentUser.PsychologistId is { } id ? id : PsychologicalReportErrors.OnlyPsychologists;

    public async Task<Result<(PsychologicalReport Report, string PatientName)>> FindOwnAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await db.PsychologicalReports
            .Where(r => r.Id == id)
            .Select(r => new { Report = r, PatientName = db.Patients.Where(p => p.Id == r.PatientId).Select(p => p.FullName).FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            return PsychologicalReportErrors.NotFound;
        }

        return found.Report.PsychologistId == currentUser.PsychologistId
            ? (found.Report, found.PatientName ?? string.Empty)
            : PsychologicalReportErrors.OnlyAuthor;
    }
}
