using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Patients;

namespace Psycheflow.Api.Features.Sessions.UpdateSession;

/// <summary>UC09 / RF008: troca de paciente e anotações. Data e horário mudam pelo reagendamento (com motivo).</summary>
public sealed class UpdateSessionHandler(AppDbContext db, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(Guid id, UpdateSessionRequest request, CancellationToken cancellationToken)
    {
        Result<Session> found = await access.FindManageableAsync(id, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Session session = found.Value;
        if (request.Notes is not null && !access.CanSeeClinicalData(session))
        {
            return SessionErrors.ClinicalDataRestricted;
        }

        if (request.PatientId != session.PatientId)
        {
            Result<Patient> patient = await access.FindActivePatientAsync(request.PatientId, cancellationToken);
            if (patient.IsFailure)
            {
                return patient.Error;
            }
        }

        Result updated = session.UpdateDetails(request.PatientId, request.Notes);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        // O novo paciente está rastreado: o EF ajusta a navegação session.Patient ao salvar.
        await db.SaveChangesAsync(cancellationToken);
        return access.ToResponse(session);
    }
}
