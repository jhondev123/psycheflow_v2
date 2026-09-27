using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Patients;

public static class PatientErrors
{
    public static readonly Error NotFound = Error.NotFound("patient.not_found", "Paciente não encontrado.");

    public static readonly Error CpfAlreadyRegistered = Error.Conflict(
        "patient.cpf_already_registered", "Já existe um paciente com este CPF na empresa.");
}
