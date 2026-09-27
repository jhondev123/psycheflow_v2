namespace Psycheflow.Api.Features.MedicalRecords;

public sealed record AttachmentResponse(Guid Id, string FileName, string ContentType, long SizeBytes, DateTimeOffset CreatedAt)
{
    public static AttachmentResponse From(MedicalRecordAttachment attachment) =>
        new(attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, attachment.CreatedAt);
}

public sealed record MedicalRecordResponse(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid PsychologistId,
    string Title,
    string Content,
    IReadOnlyList<AttachmentResponse> Attachments,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static MedicalRecordResponse From(MedicalRecord record, string patientName) => new(
        record.Id,
        record.PatientId,
        patientName,
        record.PsychologistId,
        record.Title,
        record.Content,
        [.. record.Attachments.OrderBy(a => a.CreatedAt).Select(AttachmentResponse.From)],
        record.CreatedAt,
        record.UpdatedAt);
}

/// <param name="Excerpt">Início do conteúdo (até 160 caracteres), para a listagem.</param>
public sealed record MedicalRecordListItem(
    Guid Id,
    Guid PatientId,
    string PatientName,
    string Title,
    string Excerpt,
    int AttachmentsCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
