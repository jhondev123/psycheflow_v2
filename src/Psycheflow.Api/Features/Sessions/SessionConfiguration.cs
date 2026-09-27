using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.Features.Sessions;

internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions", table => table.HasCheckConstraint(
            "ck_sessions_feedback_score",
            $"feedback_score IS NULL OR feedback_score BETWEEN {Session.MinFeedbackScore} AND {Session.MaxFeedbackScore}"));
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Notes).HasMaxLength(Session.NotesMaxLength);
        builder.Property(s => s.FeedbackComment).HasMaxLength(Session.CommentMaxLength);
        builder.Property(s => s.CancellationReason).HasMaxLength(Session.ReasonMaxLength);
        builder.Property(s => s.RescheduleReason).HasMaxLength(Session.ReasonMaxLength);

        // RN-36: cada sessão tem exatamente um item de agenda.
        builder.HasOne(s => s.Schedule).WithOne().HasForeignKey<Session>(s => s.ScheduleId).OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(s => s.Schedule).IsRequired();

        builder.HasOne(s => s.Psychologist).WithMany().HasForeignKey(s => s.PsychologistId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Patient).WithMany().HasForeignKey(s => s.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.PatientId, s.Status });
        builder.HasIndex(s => s.PsychologistId);
    }
}
