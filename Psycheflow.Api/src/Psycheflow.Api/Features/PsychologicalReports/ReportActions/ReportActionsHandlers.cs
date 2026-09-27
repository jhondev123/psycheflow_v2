using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.PsychologicalReports.ReportActions;

public sealed class GetReportHandler(PsychologicalReportAccess access)
{
    public async Task<Result<PsychologicalReportResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(PsychologicalReport Report, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        return found.IsSuccess ? PsychologicalReportResponse.From(found.Value.Report, found.Value.PatientName) : found.Error;
    }
}

public sealed record ListReportsQuery(Guid? PatientId = null);

/// <summary>Documentos do psicólogo logado (outros usuários recebem lista vazia).</summary>
public sealed class ListReportsHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<IReadOnlyList<PsychologicalReportResponse>> Handle(ListReportsQuery query, CancellationToken cancellationToken)
    {
        Guid? own = currentUser.PsychologistId;
        IQueryable<PsychologicalReport> reports = db.PsychologicalReports.AsNoTracking().Where(r => r.PsychologistId == own);
        if (query.PatientId is { } patientId)
        {
            reports = reports.Where(r => r.PatientId == patientId);
        }

        var rows = await reports
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { Report = r, PatientName = db.Patients.Where(p => p.Id == r.PatientId).Select(p => p.FullName).FirstOrDefault() })
            .ToListAsync(cancellationToken);

        return [.. rows.Select(r => PsychologicalReportResponse.From(r.Report, r.PatientName ?? string.Empty))];
    }
}

public sealed class FinalizeReportHandler(AppDbContext db, PsychologicalReportAccess access, TimeProvider timeProvider)
{
    public async Task<Result<PsychologicalReportResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(PsychologicalReport Report, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Result finalized = found.Value.Report.Finalize(timeProvider.GetUtcNow());
        if (finalized.IsFailure)
        {
            return finalized.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return PsychologicalReportResponse.From(found.Value.Report, found.Value.PatientName);
    }
}

/// <summary>Rascunhos podem ser excluídos; documentos finalizados fazem parte do registro e são mantidos.</summary>
public sealed class DeleteReportHandler(AppDbContext db, PsychologicalReportAccess access)
{
    public async Task<Result> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(PsychologicalReport Report, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        if (found.Value.Report.Status == PsychologicalReportStatus.Finalized)
        {
            return PsychologicalReportErrors.AlreadyFinalized;
        }

        db.PsychologicalReports.Remove(found.Value.Report);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
