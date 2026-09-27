using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Patients.UpdatePatient;

/// <summary>UC02 / RF003: edita dados pessoais, endereço e status (CPF não editável).</summary>
public sealed class UpdatePatientHandler(AppDbContext db)
{
    public async Task<Result<PatientResponse>> Handle(Guid id, UpdatePatientRequest request, CancellationToken cancellationToken)
    {
        Patient? patient = await db.Patients.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        Result<Phone> phone = Phone.Create(request.Phone);
        if (phone.IsFailure)
        {
            return phone.Error;
        }

        patient.Update(
            request.FullName, request.Email, phone.Value, request.BirthDate, request.Status, Address.From(request.Address), request.Notes);
        await db.SaveChangesAsync(cancellationToken);

        return PatientResponse.From(patient);
    }
}
