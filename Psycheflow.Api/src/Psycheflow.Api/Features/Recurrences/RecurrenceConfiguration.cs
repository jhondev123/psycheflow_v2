using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Recurrences;

internal sealed class RecurrenceConfiguration : IEntityTypeConfiguration<Recurrence>
{
    public void Configure(EntityTypeBuilder<Recurrence> builder)
    {
        builder.ToTable("recurrences", table => table.HasCheckConstraint("ck_recurrences_price", "price >= 0"));
        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.EndReason).HasMaxLength(Recurrence.ReasonMaxLength);
        builder.Ignore(r => r.NextWindowStart);
        builder.Ignore(r => r.IsFullyGenerated);

        builder.HasIndex(r => r.PatientId);
        builder.HasOne<Patient>().WithMany().HasForeignKey(r => r.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Psychologist>().WithMany().HasForeignKey(r => r.PsychologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(r => r.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
