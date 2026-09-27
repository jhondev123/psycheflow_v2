using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Psycheflow.Api.Features.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.FullName).HasMaxLength(User.FullNameMaxLength);
        builder.HasIndex(u => u.CompanyId);

        builder.HasOne(u => u.Company)
            .WithMany()
            .HasForeignKey(u => u.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
