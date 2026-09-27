using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Recurrences.CreateRecurrence;

/// <summary>UC10 / RF010: cria a recorrência e gera as sessões dos próximos 3 meses, pulando datas indisponíveis.</summary>
public sealed class CreateRecurrenceHandler(
    AppDbContext db, ScheduleAvailability availability, SessionAccess sessions, RecurrenceGenerator generator)
{
    public async Task<Result<RecurrenceGenerationResponse>> Handle(CreateRecurrenceRequest request, CancellationToken cancellationToken)
    {
        Result<Psychologist> psychologist = await availability.ResolvePsychologistAsync(request.PsychologistId, cancellationToken);
        if (psychologist.IsFailure)
        {
            return psychologist.Error;
        }

        Result<Patient> patient = await sessions.FindActivePatientAsync(request.PatientId, cancellationToken);
        if (patient.IsFailure)
        {
            return patient.Error;
        }

        (CompanySettings settings, DateTime localNow) = await sessions.GetClinicContextAsync(cancellationToken);
        DateOnly startDate = request.StartDate!.Value;
        if (startDate < DateOnly.FromDateTime(localNow))
        {
            return RecurrenceErrors.StartInThePast;
        }

        int duration = request.DurationMinutes ?? settings.SessionDurationMinutes;
        Result<TimeSlot> firstSlot = TimeSlot.FromDuration(startDate, request.StartTime!.Value, duration);
        if (firstSlot.IsFailure)
        {
            return firstSlot.Error;
        }

        var recurrence = Recurrence.Create(
            psychologist.Value.Id,
            patient.Value.Id,
            request.Type!.Value,
            startDate,
            request.EndDate,
            request.StartTime.Value,
            duration,
            SessionAccess.ResolvePrice(request.Price, settings));
        db.Recurrences.Add(recurrence);

        (IReadOnlyList<DateOnly> scheduled, IReadOnlyList<SkippedOccurrence> skipped) =
            await generator.GenerateNextWindowAsync(recurrence, psychologist.Value, localNow, cancellationToken);

        return new RecurrenceGenerationResponse(RecurrenceResponse.From(recurrence), scheduled, skipped);
    }
}
