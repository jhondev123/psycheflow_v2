using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Storage;

namespace Psycheflow.Api.Features.MedicalRecords.Attachments;

public sealed record AttachmentDownload(Stream Content, string ContentType, string FileName);

/// <summary>RN-65: anexa PDF/imagem ao prontuário. O arquivo é gravado antes do registro e removido se o banco falhar.</summary>
public sealed class UploadAttachmentHandler(AppDbContext db, MedicalRecordAccess access, IFileStorage storage, ICurrentUser currentUser)
{
    public async Task<Result<AttachmentResponse>> Handle(Guid recordId, IFormFile? file, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(recordId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        if (file is null || file.Length == 0)
        {
            return AttachmentPolicy.Missing;
        }

        if (file.Length > AttachmentPolicy.MaxSizeBytes)
        {
            return AttachmentPolicy.TooLarge;
        }

        await using Stream content = file.OpenReadStream();
        byte[] header = new byte[AttachmentPolicy.HeaderLength];
        int read = await content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        if (AttachmentPolicy.DetectContentType(header.AsSpan(0, read)) is not { } contentType)
        {
            return AttachmentPolicy.UnsupportedType;
        }

        content.Position = 0;
        MedicalRecordAttachment attachment = found.Value.Record.AddAttachment(AttachmentPolicy.SafeFileName(file.FileName), contentType, file.Length);
        string key = attachment.StorageKey(currentUser.CompanyId);
        await storage.SaveAsync(key, content, cancellationToken);

        try
        {
            db.MedicalRecordAttachments.Add(attachment);
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await storage.DeleteAsync(key, CancellationToken.None);
            throw;
        }

        return AttachmentResponse.From(attachment);
    }
}

public sealed class DownloadAttachmentHandler(MedicalRecordAccess access, IFileStorage storage, ICurrentUser currentUser)
{
    public async Task<Result<AttachmentDownload>> Handle(Guid recordId, Guid attachmentId, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(recordId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        MedicalRecordAttachment? attachment = found.Value.Record.Attachments.SingleOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
        {
            return MedicalRecordErrors.AttachmentNotFound;
        }

        Stream content = await storage.OpenReadAsync(attachment.StorageKey(currentUser.CompanyId), cancellationToken);
        return new AttachmentDownload(content, attachment.ContentType, attachment.FileName);
    }
}

/// <summary>Remove o anexo do prontuário (exclusão lógica; o arquivo é mantido pela guarda obrigatória).</summary>
public sealed class DeleteAttachmentHandler(AppDbContext db, MedicalRecordAccess access)
{
    public async Task<Result> Handle(Guid recordId, Guid attachmentId, CancellationToken cancellationToken)
    {
        Result<(MedicalRecord Record, string PatientName)> found = await access.FindOwnAsync(recordId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        MedicalRecordAttachment? attachment = found.Value.Record.Attachments.SingleOrDefault(a => a.Id == attachmentId);
        if (attachment is null)
        {
            return MedicalRecordErrors.AttachmentNotFound;
        }

        db.MedicalRecordAttachments.Remove(attachment);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
