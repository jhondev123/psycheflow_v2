using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Scheduling;

namespace Psycheflow.Api.Features.Sessions.CreateSession;

/// <summary>
/// UC04 / RF004: agenda uma sessão (Schedule + Session numa transação) respeitando RN-30 a RN-35.
/// A duração e o valor vêm da configuração da empresa quando não informados (RN-32, RN-51) e a sessão
/// nasce com pagamento pendente (RN-50).
/// </summary>
public sealed class CreateSessionHandler(AppDbContext db, ScheduleAvailability availability, SessionAccess access)
{
    public async Task<Result<SessionResponse>> Handle(CreateSessionRequest request, CancellationToken cancellationToken)
    {
        Result<Psychologist> psychologist = await availability.ResolvePsychologistAsync(request.PsychologistId, cancellationToken);
        if (psychologist.IsFailure)
        {
            return psychologist.Error;
        }

        Result<Patient> patient = await access.FindActivePatientAsync(request.PatientId, cancellationToken);
        if (patient.IsFailure)
        {
            return patient.Error;
        }

        (CompanySettings settings, DateTime localNow) = await access.GetClinicContextAsync(cancellationToken);
        Result<TimeSlot> slot = SessionAccess.BuildSlot(
            request.Date!.Value, request.StartTime!.Value, request.DurationMinutes, settings.SessionDurationMinutes);
        if (slot.IsFailure)
        {
            return slot.Error;
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await availability.LockAgendaAsync(psychologist.Value.Id, cancellationToken);

        Result available = await availability.CheckSessionSlotAsync(psychologist.Value, slot.Value, localNow, null, cancellationToken);
        if (available.IsFailure)
        {
            return available.Error;
        }

        var session = Session.Book(psychologist.Value.Id, patient.Value.Id, slot.Value, request.Notes);
        access.AddWithPayment(session, SessionAccess.ResolvePrice(request.Price, settings));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return access.ToResponse(session);
    }
}
