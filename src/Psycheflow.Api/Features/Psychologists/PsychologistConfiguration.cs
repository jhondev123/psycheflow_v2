using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.Features.Psychologists;

internal sealed class PsychologistConfiguration : IEntityTypeConfiguration<Psychologist>
{
    public void Configure(EntityTypeBuilder<Psychologist> builder)
    {
        builder.ToTable("psychologists");
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LicenseNumber)
            .HasConversion(v => v.Value, v => LicenseNumber.FromDatabase(v))
            .HasMaxLength(LicenseNumber.MaxLength);

        builder.Property(p => p.Phone)
            .HasConversion(v => v!.Value, v => Phone.FromDatabase(v))
            .HasMaxLength(11);

        // Um usuário tem no máximo um perfil de psicólogo ativo.
        builder.HasIndex(p => p.UserId).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => p.CompanyId);

        builder.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
