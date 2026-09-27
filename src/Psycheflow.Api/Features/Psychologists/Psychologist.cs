using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Psychologists;

/// <summary>Perfil profissional de um usuário que atende pacientes. O nome e o e-mail ficam no <see cref="Users.User"/>.</summary>
public sealed class Psychologist : Entity, ITenantEntity, ISoftDeletable
{
    private Psychologist()
    {
    }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public Guid CompanyId { get; private set; }

    public LicenseNumber LicenseNumber { get; private set; } = null!;

    public ApproachType Approach { get; private set; }

    public Phone? Phone { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static Psychologist Create(Guid userId, Guid companyId, LicenseNumber licenseNumber, ApproachType approach, Phone? phone) =>
        new()
        {
            UserId = userId,
            CompanyId = companyId,
            LicenseNumber = licenseNumber,
            Approach = approach,
            Phone = phone,
        };

    public void UpdateProfile(LicenseNumber licenseNumber, ApproachType approach, Phone? phone)
    {
        LicenseNumber = licenseNumber;
        Approach = approach;
        Phone = phone;
    }
}
