using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.PsychologicalReports;

internal sealed class PsychologicalReportConfiguration : IEntityTypeConfiguration<PsychologicalReport>
{
    public void Configure(EntityTypeBuilder<PsychologicalReport> builder)
    {
        builder.ToTable("psychological_reports");
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Ignore(r => r.Sections);

        builder.Property(r => r.Purpose).HasMaxLength(PsychologicalReport.SectionMaxLength);
        builder.Property(r => r.Demand).HasMaxLength(PsychologicalReport.SectionMaxLength);
        builder.Property(r => r.Procedure).HasMaxLength(PsychologicalReport.SectionMaxLength);
        builder.Property(r => r.Analysis).HasMaxLength(PsychologicalReport.SectionMaxLength);
        builder.Property(r => r.Conclusion).HasMaxLength(PsychologicalReport.SectionMaxLength);

        builder.HasIndex(r => new { r.PsychologistId, r.PatientId });
        builder.HasOne<Patient>().WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Psychologist>().WithMany().HasForeignKey(r => r.PsychologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
