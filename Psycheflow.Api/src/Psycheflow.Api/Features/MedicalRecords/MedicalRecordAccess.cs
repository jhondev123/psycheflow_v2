using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.MedicalRecords;

public static class MedicalRecordErrors
{
    public static readonly Error NotFound = Error.NotFound("medical_record.not_found", "Registro de prontuário não encontrado.");

    public static readonly Error AttachmentNotFound = Error.NotFound("medical_record.attachment_not_found", "Anexo não encontrado.");

    public static readonly Error OnlyAuthor = Error.Forbidden(
        "medical_record.only_author", "Somente o psicólogo autor pode acessar este prontuário.");

    public static readonly Error OnlyPsychologists = Error.Forbidden(
        "medical_record.only_psychologists", "Somente psicólogos podem registrar prontuários.");
}

/// <summary>Prontuário é do psicólogo autor (sigilo — D-02, RD001/RD002). Acessos negados e leituras vão para a trilha de acesso.</summary>
public sealed class MedicalRecordAccess(AppDbContext db, ICurrentUser currentUser)
{
    public Result<Guid> RequirePsychologist() =>
        currentUser.PsychologistId is { } id ? id : MedicalRecordErrors.OnlyPsychologists;

    public async Task<Result<(MedicalRecord Record, string PatientName)>> FindOwnAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await db.MedicalRecords
            .Include(r => r.Attachments)
            .Where(r => r.Id == id)
            .Select(r => new { Record = r, PatientName = db.Patients.Where(p => p.Id == r.PatientId).Select(p => p.FullName).FirstOrDefault() })
            .SingleOrDefaultAsync(cancellationToken);

        if (found is null)
        {
            return MedicalRecordErrors.NotFound;
        }

        if (found.Record.PsychologistId != currentUser.PsychologistId)
        {
            await LogAsync(found.Record.Id, MedicalRecordAccessAction.Denied, cancellationToken: cancellationToken);
            return MedicalRecordErrors.OnlyAuthor;
        }

        return (found.Record, found.PatientName ?? string.Empty);
    }

    public async Task LogAsync(
        Guid recordId, MedicalRecordAccessAction action, Guid? attachmentId = null, CancellationToken cancellationToken = default)
    {
        db.MedicalRecordAccessLogs.Add(MedicalRecordAccessLog.Create(recordId, currentUser.UserId, action, attachmentId));
        await db.SaveChangesAsync(cancellationToken);
    }
}
