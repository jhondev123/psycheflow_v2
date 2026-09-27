using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Patients.CreatePatient;

/// <summary>UC01 / RF001: cadastro de paciente com CPF único na empresa (RN-22), nascendo ativo (RN-25).</summary>
public sealed class CreatePatientHandler(AppDbContext db)
{
    public async Task<Result<PatientResponse>> Handle(CreatePatientRequest request, CancellationToken cancellationToken)
    {
        Result<Cpf> cpf = Cpf.Create(request.Cpf);
        if (cpf.IsFailure)
        {
            return cpf.Error;
        }

        Result<Phone> phone = Phone.Create(request.Phone);
        if (phone.IsFailure)
        {
            return phone.Error;
        }

        if (await db.Patients.AnyAsync(p => p.Cpf == cpf.Value, cancellationToken))
        {
            return PatientErrors.CpfAlreadyRegistered;
        }

        var patient = Patient.Create(
            request.FullName, cpf.Value, request.Email, phone.Value, request.BirthDate, Address.From(request.Address), request.Notes);
        db.Patients.Add(patient);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.IsUniqueViolation())
        {
            return PatientErrors.CpfAlreadyRegistered;
        }

        return PatientResponse.From(patient);
    }
}
