using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Psychologists;

/// <summary>Perfil profissional de um usuário que atende pacientes. O nome e o e-mail ficam no <see cref="Users.User"/>.</summary>
public sealed class Psychologist : Entity, ITenantEntity, ISoftDeletable
{
    private readonly List<WorkingHoursRange> _workingHours = [];

    private Psychologist()
    {
    }

    public Guid UserId { get; private set; }

    public User? User { get; private set; }

    public Guid CompanyId { get; private set; }

    public LicenseNumber LicenseNumber { get; private set; } = null!;

    public ApproachType Approach { get; private set; }

    public Phone? Phone { get; private set; }

    /// <summary>Expediente semanal (RC-03). Usado para validar agendamentos de sessão (RN-33).</summary>
    public IReadOnlyList<WorkingHoursRange> WorkingHours => _workingHours;

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

    /// <summary>RN-68: substitui todo o expediente. Faixas do mesmo dia não podem se sobrepor e o início precisa ser antes do fim.</summary>
    public Result SetWorkingHours(IEnumerable<WorkingHoursRange> ranges)
    {
        List<WorkingHoursRange> ordered = [.. ranges.OrderBy(r => r.DayOfWeek).ThenBy(r => r.StartTime)];

        if (ordered.Any(r => r.EndTime <= r.StartTime))
        {
            return PsychologistErrors.InvalidWorkingHoursRange;
        }

        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Overlaps(ordered[i - 1]))
            {
                return PsychologistErrors.OverlappingWorkingHours;
            }
        }

        _workingHours.Clear();
        _workingHours.AddRange(ordered);
        return Result.Success();
    }

    /// <summary>RN-33: o intervalo precisa caber inteiro numa faixa de expediente do dia.</summary>
    public bool WorksAt(DayOfWeek dayOfWeek, TimeOnly start, TimeOnly end) =>
        _workingHours.Any(range => range.DayOfWeek == dayOfWeek && range.Contains(start, end));
}
