using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.MedicalRecords;

internal sealed class MedicalRecordConfiguration : IEntityTypeConfiguration<MedicalRecord>
{
    public void Configure(EntityTypeBuilder<MedicalRecord> builder)
    {
        builder.ToTable("medical_records");
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Title).HasMaxLength(MedicalRecord.TitleMaxLength);
        builder.Property(r => r.Content).HasMaxLength(MedicalRecord.ContentMaxLength);

        builder.HasMany(r => r.Attachments).WithOne().HasForeignKey(a => a.MedicalRecordId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(r => r.Attachments).HasField("_attachments").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(r => new { r.PsychologistId, r.PatientId });
        builder.HasOne<Patient>().WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Psychologist>().WithMany().HasForeignKey(r => r.PsychologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MedicalRecordAttachmentConfiguration : IEntityTypeConfiguration<MedicalRecordAttachment>
{
    public void Configure(EntityTypeBuilder<MedicalRecordAttachment> builder)
    {
        builder.ToTable("medical_record_attachments");
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.FileName).HasMaxLength(200);
        builder.Property(a => a.ContentType).HasMaxLength(100);
    }
}
