namespace Psycheflow.Api.Features.Patients.CreatePatient;

public sealed record CreatePatientRequest(
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly? BirthDate = null,
    AddressDto? Address = null,
    string? Notes = null) : IPatientData;
