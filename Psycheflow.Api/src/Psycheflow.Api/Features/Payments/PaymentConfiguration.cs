using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Psycheflow.Api.Features.Companies;

namespace Psycheflow.Api.Features.Payments;

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments", table => table.HasCheckConstraint("ck_payments_amount", "amount >= 0"));
        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Notes).HasMaxLength(Payment.NotesMaxLength);
        builder.Property(p => p.CancellationReason).HasMaxLength(Payment.NotesMaxLength);

        // Um pagamento por sessão.
        builder.HasOne(p => p.Session)
            .WithOne(s => s.Payment)
            .HasForeignKey<Payment>(p => p.SessionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.SessionId).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasIndex(p => new { p.CompanyId, p.Status });

        builder.HasOne<Company>().WithMany().HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
