using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.MedicalRecords;

public enum MedicalRecordAccessAction
{
    Viewed = 0,
    AttachmentDownloaded = 1,

    /// <summary>Tentativa de acesso de quem não é o autor (negada com 403).</summary>
    Denied = 2,
}

/// <summary>
/// Trilha de acesso ao prontuário (DT-24, RD002/LGPD): quem leu, baixou anexo ou tentou acessar sem permissão, e quando.
/// Registros só são incluídos, nunca alterados.
/// </summary>
public sealed class MedicalRecordAccessLog : Entity, ITenantEntity
{
    private MedicalRecordAccessLog()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid MedicalRecordId { get; private set; }

    public Guid UserId { get; private set; }

    public MedicalRecordAccessAction Action { get; private set; }

    public Guid? AttachmentId { get; private set; }

    public static MedicalRecordAccessLog Create(Guid medicalRecordId, Guid userId, MedicalRecordAccessAction action, Guid? attachmentId = null) =>
        new() { MedicalRecordId = medicalRecordId, UserId = userId, Action = action, AttachmentId = attachmentId };
}

public sealed record MedicalRecordAccessEntry(DateTimeOffset At, Guid UserId, string UserName, MedicalRecordAccessAction Action, Guid? AttachmentId);

internal sealed class MedicalRecordAccessLogConfiguration : IEntityTypeConfiguration<MedicalRecordAccessLog>
{
    public void Configure(EntityTypeBuilder<MedicalRecordAccessLog> builder)
    {
        builder.ToTable("medical_record_access_logs");
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.HasIndex(l => new { l.MedicalRecordId, l.CreatedAt });
        builder.HasOne<MedicalRecord>().WithMany().HasForeignKey(l => l.MedicalRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(l => l.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
