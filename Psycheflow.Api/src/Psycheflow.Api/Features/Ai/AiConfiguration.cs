using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Ai;

internal sealed class AiSettingsConfiguration : IEntityTypeConfiguration<AiSettings>
{
    public void Configure(EntityTypeBuilder<AiSettings> builder)
    {
        builder.ToTable("ai_settings");
        builder.Property(s => s.Id).ValueGeneratedNever();
        builder.Ignore(s => s.Sharing);

        builder.HasIndex(s => s.CompanyId).IsUnique();
        builder.HasOne<Company>().WithMany().HasForeignKey(s => s.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(s => s.ConsentAcceptedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AiUsageLogConfiguration : IEntityTypeConfiguration<AiUsageLog>
{
    public void Configure(EntityTypeBuilder<AiUsageLog> builder)
    {
        builder.ToTable("ai_usage_logs");
        builder.Property(l => l.Id).ValueGeneratedNever();
        builder.Property(l => l.Model).HasMaxLength(AiUsageLog.ModelMaxLength);

        builder.HasIndex(l => new { l.CompanyId, l.CreatedAt });
        builder.HasOne<Company>().WithMany().HasForeignKey(l => l.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(l => l.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(l => l.PatientId).OnDelete(DeleteBehavior.Restrict);
    }
}
