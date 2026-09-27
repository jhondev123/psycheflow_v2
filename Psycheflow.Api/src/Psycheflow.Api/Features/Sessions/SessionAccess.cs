using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions;

/// <summary>
/// Carregamento e regras de acesso das sessões (D-02): Admin/Manager e o psicólogo da sessão acessam a agenda;
/// anotações e feedback são exclusivos do psicólogo da sessão.
/// </summary>
public sealed class SessionAccess(AppDbContext db, ICurrentUser currentUser, ClinicClock clock)
{
    public bool CanSeeClinicalData(Session session) => currentUser.PsychologistId == session.PsychologistId;

    /// <summary>Sessão (com agenda, paciente e psicólogo) que o usuário pode gerenciar.</summary>
    public async Task<Result<Session>> FindManageableAsync(Guid id, CancellationToken cancellationToken)
    {
        Session? session = await db.Sessions
            .Include(s => s.Schedule)
            .Include(s => s.Patient)
            .Include(s => s.Psychologist!).ThenInclude(p => p.User)
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (session is null)
        {
            return SessionErrors.NotFound;
        }

        return currentUser.CanManagePsychologist(session.PsychologistId) ? session : SessionErrors.CannotAccess;
    }

    /// <summary>Paciente ativo da empresa (RN-35).</summary>
    public async Task<Result<Patient>> FindActivePatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        Patient? patient = await db.Patients.SingleOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        return patient.Status == PatientStatus.Active ? patient : SessionErrors.PatientInactive;
    }

    public async Task<(CompanySettings Settings, DateTime LocalNow)> GetClinicContextAsync(CancellationToken cancellationToken)
    {
        CompanySettings settings = await db.GetCurrentSettingsAsync(currentUser, cancellationToken);
        return (settings, clock.LocalNow(settings.TimeZone));
    }

    public SessionResponse ToResponse(Session session) => SessionResponse.From(session, CanSeeClinicalData(session));

    public static Result<TimeSlot> BuildSlot(DateOnly date, TimeOnly start, int? durationMinutes, int defaultDuration) =>
        TimeSlot.FromDuration(date, start, durationMinutes ?? defaultDuration);
}
