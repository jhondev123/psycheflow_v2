namespace Psycheflow.Api.Features.Psychologists;

public sealed record WorkingHoursDto(DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)
{
    public static IReadOnlyList<WorkingHoursDto> From(IEnumerable<WorkingHoursRange> ranges) =>
    [
        .. ranges
            .OrderBy(r => r.DayOfWeek)
            .ThenBy(r => r.StartTime)
            .Select(r => new WorkingHoursDto(r.DayOfWeek, r.StartTime, r.EndTime)),
    ];
}

public sealed record PsychologistResponse(
    Guid Id,
    Guid UserId,
    string FullName,
    string Email,
    string LicenseNumber,
    ApproachType Approach,
    string? Phone,
    IReadOnlyList<WorkingHoursDto> WorkingHours)
{
    /// <summary>Requer <see cref="Psychologist.User"/> carregado.</summary>
    public static PsychologistResponse From(Psychologist psychologist) => new(
        psychologist.Id,
        psychologist.UserId,
        psychologist.User!.FullName,
        psychologist.User.Email!,
        psychologist.LicenseNumber.Value,
        psychologist.Approach,
        psychologist.Phone?.Value,
        WorkingHoursDto.From(psychologist.WorkingHours));
}
