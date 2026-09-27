namespace Psycheflow.Api.Features.Patients.UpdatePatient;

/// <summary>RN-27: o CPF não é editável e por isso não faz parte do request.</summary>
public sealed record UpdatePatientRequest(
    string FullName,
    string Email,
    string Phone,
    PatientStatus Status,
    DateOnly? BirthDate = null,
    AddressDto? Address = null,
    string? Notes = null) : IPatientData;
