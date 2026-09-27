using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Scheduling;

internal sealed class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("schedules", table =>
            table.HasCheckConstraint("ck_schedules_time_range", "end_time > start_time"));
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Property(s => s.BlockReason).HasMaxLength(Schedule.ReasonMaxLength);
        builder.Ignore(s => s.Slot);

        // Consulta de conflitos e da agenda: por psicólogo e dia.
        builder.HasIndex(s => new { s.PsychologistId, s.Date });

        builder.HasOne<Psychologist>().WithMany().HasForeignKey(s => s.PsychologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
