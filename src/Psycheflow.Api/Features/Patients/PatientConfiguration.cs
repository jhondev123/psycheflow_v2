using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Patients;

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("patients");
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.FullName).HasMaxLength(Patient.FullNameMaxLength);
        builder.Property(p => p.Email).HasMaxLength(Patient.EmailMaxLength);
        builder.Property(p => p.Notes).HasMaxLength(Patient.NotesMaxLength);
        builder.Property(p => p.Cpf)
            .HasConversion(v => v.Value, v => Cpf.FromDatabase(v))
            .HasMaxLength(Cpf.Length)
            .IsFixedLength();
        builder.Property(p => p.Phone)
            .HasConversion(v => v.Value, v => Phone.FromDatabase(v))
            .HasMaxLength(11);

        builder.OwnsOne(p => p.Address, address =>
        {
            address.Property(a => a.ZipCode).HasMaxLength(Address.ZipCodeLength).IsFixedLength();
            address.Property(a => a.Street).HasMaxLength(150);
            address.Property(a => a.Number).HasMaxLength(20);
            address.Property(a => a.Complement).HasMaxLength(100);
            address.Property(a => a.Neighborhood).HasMaxLength(100);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.State).HasMaxLength(2).IsFixedLength();
        });

        // RN-22: CPF único por empresa entre pacientes não excluídos.
        builder.HasIndex(p => new { p.CompanyId, p.Cpf }).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => new { p.CompanyId, p.FullName });

        builder.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
