namespace Psycheflow.Api.Features.Patients;

public sealed record PatientResponse(
    Guid Id,
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly? BirthDate,
    PatientStatus Status,
    AddressDto? Address,
    string? Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt)
{
    public static PatientResponse From(Patient patient) => new(
        patient.Id,
        patient.FullName,
        patient.Cpf.Value,
        patient.Email,
        patient.Phone.Value,
        patient.BirthDate,
        patient.Status,
        patient.Address?.ToDto(),
        patient.Notes,
        patient.CreatedAt,
        patient.UpdatedAt);
}

/// <param name="LastSessionDate">Data da última sessão realizada (concluída), se houver.</param>
public sealed record PatientListItem(
    Guid Id,
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    PatientStatus Status,
    DateOnly? LastSessionDate);
