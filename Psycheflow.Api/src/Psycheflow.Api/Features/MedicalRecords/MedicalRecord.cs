using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.MedicalRecords;

/// <summary>
/// Registro do prontuário de um paciente (RF019/RF020, Resolução CFP 001/2009): texto (Markdown) e anexos.
/// Sigiloso — só o psicólogo autor acessa (D-02). A exclusão é lógica, preservando a guarda obrigatória.
/// </summary>
public sealed class MedicalRecord : Entity, ITenantEntity, ISoftDeletable
{
    public const int TitleMaxLength = 200;
    public const int ContentMaxLength = 50_000;

    private readonly List<MedicalRecordAttachment> _attachments = [];

    private MedicalRecord()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid PatientId { get; private set; }

    public Guid PsychologistId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Content { get; private set; } = string.Empty;

    public IReadOnlyList<MedicalRecordAttachment> Attachments => _attachments;

    public DateTimeOffset? DeletedAt { get; private set; }

    public static MedicalRecord Create(Guid patientId, Guid psychologistId, string title, string content) =>
        new() { PatientId = patientId, PsychologistId = psychologistId, Title = title.Trim(), Content = content.Trim() };

    public void Update(string title, string content)
    {
        Title = title.Trim();
        Content = content.Trim();
    }

    public MedicalRecordAttachment AddAttachment(string fileName, string contentType, long sizeBytes)
    {
        var attachment = MedicalRecordAttachment.Create(Id, fileName, contentType, sizeBytes);
        _attachments.Add(attachment);
        return attachment;
    }
}

/// <summary>Anexo (PDF ou imagem). O arquivo fica no <c>IFileStorage</c> sob <see cref="StorageKey"/>.</summary>
public sealed class MedicalRecordAttachment : Entity, ISoftDeletable
{
    private MedicalRecordAttachment()
    {
    }

    public Guid MedicalRecordId { get; private set; }

    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public string StorageKey(Guid companyId) => $"{companyId:N}/medical-records/{MedicalRecordId:N}/{Id:N}";

    internal static MedicalRecordAttachment Create(Guid recordId, string fileName, string contentType, long sizeBytes) =>
        new() { MedicalRecordId = recordId, FileName = fileName, ContentType = contentType, SizeBytes = sizeBytes };
}
