using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;

namespace Psycheflow.Api.Features.MedicalRecords.RecordCrud;

/// <summary>UC20 / RF019 / RN-65: novo registro no prontuário do paciente (autor = psicólogo logado).</summary>
public sealed class CreateMedicalRecordHandler(AppDbContext db, MedicalRecordAccess access)
{
    public async Task<Result<MedicalRecordResponse>> Handle(CreateMedicalRecordRequest request, CancellationToken cancellationToken)
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

        var record = MedicalRecord.Create(patient.Id, psychologistId.Value, request.Title!, request.Content!);
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return MedicalRecordResponse.From(record, patient.FullName);
    }
}

/// <summary>UC21 / RF020 / RN-66: busca nos prontuários do psicólogo por paciente, período e palavra-chave.</summary>
public sealed class ListMedicalRecordsHandler(AppDbContext db, ICurrentUser currentUser)
{
    private const int ExcerptLength = 160;

    public async Task<PagedResponse<MedicalRecordListItem>> Handle(ListMedicalRecordsQuery query, CancellationToken cancellationToken)
    {
        Guid? own = currentUser.PsychologistId;
        IQueryable<MedicalRecord> records = db.MedicalRecords.AsNoTracking().Where(r => r.PsychologistId == own);

        if (query.PatientId is { } patientId)
        {
            records = records.Where(r => r.PatientId == patientId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string pattern = $"%{query.Search.Trim().Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
            records = records.Where(r => EF.Functions.ILike(r.Title, pattern) || EF.Functions.ILike(r.Content, pattern));
        }

        if (query.From is not null || query.To is not null)
        {
            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById((await db.GetCurrentSettingsAsync(currentUser, cancellationToken)).TimeZone);
            if (query.From is { } from)
            {
                DateTimeOffset start = StartOfDayUtc(from, timeZone);
                records = records.Where(r => r.CreatedAt >= start);
            }

            if (query.To is { } to)
            {
                DateTimeOffset end = StartOfDayUtc(to.AddDays(1), timeZone);
                records = records.Where(r => r.CreatedAt < end);
            }
        }

        return await records
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MedicalRecordListItem(
                r.Id,
                r.PatientId,
                db.Patients.Where(p => p.Id == r.PatientId).Select(p => p.FullName).FirstOrDefault() ?? string.Empty,
                r.Title,
                r.Content.Length > ExcerptLength ? r.Content.Substring(0, ExcerptLength) : r.Content,
                r.Attachments.Count,
                r.CreatedAt,
                r.UpdatedAt))
            .ToPagedResponseAsync(query.Page, query.PageSize, cancellationToken);
    }

    private static DateTimeOffset StartOfDayUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        DateTime local = date.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(local, timeZone.GetUtcOffset(local)).ToUniversalTime();
    }
}

public sealed class GetMedicalRecordHandler(MedicalRecordAccess access)
{
    public async Task<Result<MedicalRecordResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        await access.LogAsync(id, MedicalRecordAccessAction.Viewed, cancellationToken: cancellationToken);
        return MedicalRecordResponse.From(found.Value.Record, found.Value.PatientName);
    }
}

public sealed class UpdateMedicalRecordHandler(AppDbContext db, MedicalRecordAccess access)
{
    public async Task<Result<MedicalRecordResponse>> Handle(Guid id, UpdateMedicalRecordRequest request, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        found.Value.Record.Update(request.Title!, request.Content!);
        await db.SaveChangesAsync(cancellationToken);
        return MedicalRecordResponse.From(found.Value.Record, found.Value.PatientName);
    }
}

/// <summary>Exclusão lógica: o registro some das consultas, mas é mantido pela guarda obrigatória do prontuário.</summary>
public sealed class DeleteMedicalRecordHandler(AppDbContext db, MedicalRecordAccess access)
{
    public async Task<Result> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        db.MedicalRecords.Remove(found.Value.Record);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

/// <summary>DT-24: histórico de acessos ao registro (somente o autor consulta).</summary>
public sealed class GetAccessLogHandler(AppDbContext db, MedicalRecordAccess access)
{
    public async Task<Result<IReadOnlyList<MedicalRecordAccessEntry>>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        List<MedicalRecordAccessEntry> entries = await db.MedicalRecordAccessLogs
            .AsNoTracking()
            .Where(l => l.MedicalRecordId == id)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new MedicalRecordAccessEntry(
                l.CreatedAt,
                l.UserId,
                db.Users.Where(u => u.Id == l.UserId).Select(u => u.FullName).FirstOrDefault() ?? string.Empty,
                l.Action,
                l.AttachmentId))
            .ToListAsync(cancellationToken);

        return entries;
    }
}
