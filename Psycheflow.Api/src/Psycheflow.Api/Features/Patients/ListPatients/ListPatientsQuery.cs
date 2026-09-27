namespace Psycheflow.Api.Features.Patients.ListPatients;

/// <param name="Search">Parte do nome ou do e-mail, ou o CPF completo.</param>
/// <param name="LastSessionFrom">Última sessão concluída a partir desta data.</param>
/// <param name="LastSessionTo">Última sessão concluída até esta data.</param>
public sealed record ListPatientsQuery(
    string? Search = null,
    PatientStatus? Status = null,
    DateOnly? LastSessionFrom = null,
    DateOnly? LastSessionTo = null,
    int? Page = null,
    int? PageSize = null);
