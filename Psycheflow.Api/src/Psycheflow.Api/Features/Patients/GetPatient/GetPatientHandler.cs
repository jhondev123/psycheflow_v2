using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Patients.GetPatient;

public sealed class GetPatientHandler(AppDbContext db)
{
    public async Task<Result<PatientResponse>> Handle(Guid id, CancellationToken cancellationToken)
    {
        Patient? patient = await db.Patients.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        return patient is null ? PatientErrors.NotFound : PatientResponse.From(patient);
    }
}
