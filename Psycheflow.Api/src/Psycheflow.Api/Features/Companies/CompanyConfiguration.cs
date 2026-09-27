using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Psycheflow.Api.Features.Companies;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("companies");
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Company.NameMaxLength);

        builder.OwnsOne(c => c.Settings, settings =>
        {
            settings.ToTable("company_settings", table => table.HasCheckConstraint(
                "ck_company_settings_session_duration",
                $"session_duration_minutes BETWEEN {CompanySettings.MinSessionDurationMinutes} AND {CompanySettings.MaxSessionDurationMinutes}"));
            settings.WithOwner().HasForeignKey("CompanyId");
            settings.HasKey("CompanyId");
            settings.Property<Guid>("CompanyId").HasColumnName("company_id");
            settings.Property(s => s.SessionDurationMinutes).HasDefaultValue(CompanySettings.DefaultSessionDurationMinutes);
            settings.Property(s => s.TimeZone).HasMaxLength(64);
        });

        builder.Navigation(c => c.Settings).IsRequired();
    }
}
