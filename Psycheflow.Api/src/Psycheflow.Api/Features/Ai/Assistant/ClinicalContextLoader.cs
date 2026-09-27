using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Common.Time;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Ai.Assistant;

public sealed record PatientSnapshot(Guid Id, PatientIdentity Identity, DateOnly? BirthDate);

/// <summary>
/// Lê do banco o que pode ir para a IA: somente sessões e prontuários do psicólogo logado (sigilo, D-02)
/// e somente os tipos de dado autorizados pela clínica. Textos longos são cortados para limitar o tamanho do prompt.
/// </summary>
public sealed class ClinicalContextLoader(AppDbContext db, ICurrentUser currentUser, ClinicClock clock)
{
    public const int MaxEntryLength = 4000;

    public async Task<PatientSnapshot?> FindPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var patient = await db.Patients
            .AsNoTracking()
            .Where(p => p.Id == patientId)
            .Select(p => new { p.Id, p.FullName, p.Cpf, p.Email, p.Phone, p.BirthDate, Street = p.Address == null ? null : p.Address.Street })
            .SingleOrDefaultAsync(cancellationToken);

        return patient is null
            ? null
            : new PatientSnapshot(
                patient.Id,
                new PatientIdentity(patient.FullName, patient.Cpf.Value, patient.Email, patient.Phone.Value, patient.Street),
                patient.BirthDate);
    }

    public async Task<ClinicalContext> LoadAsync(PatientSnapshot patient, AiAccess access, ClinicalScope scope, CancellationToken cancellationToken)
    {
        CompanySettings settings = await db.GetCurrentSettingsAsync(currentUser, cancellationToken);
        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(settings.TimeZone);

        IQueryable<Session> ownSessions = db.Sessions.Where(s => s.PatientId == patient.Id && s.PsychologistId == access.PsychologistId);

        Dictionary<SessionStatus, int> counts = await ownSessions
            .GroupBy(s => s.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Status, g => g.Count, cancellationToken);

        return new ClinicalContext(
            AgeAt(patient.BirthDate, clock.Today(settings.TimeZone)),
            await db.Psychologists.Where(p => p.Id == access.PsychologistId).Select(p => p.Approach).SingleAsync(cancellationToken),
            new SessionCounts(
                counts.GetValueOrDefault(SessionStatus.Completed),
                counts.GetValueOrDefault(SessionStatus.NoShow),
                counts.GetValueOrDefault(SessionStatus.Cancelled)),
            await LoadSessionsAsync(ownSessions, access.Sharing, scope, cancellationToken),
            await LoadRecordsAsync(patient.Id, access, scope, timeZone, cancellationToken));
    }

    private static async Task<IReadOnlyList<SessionEntry>> LoadSessionsAsync(
        IQueryable<Session> ownSessions, AiDataSharing sharing, ClinicalScope scope, CancellationToken cancellationToken)
    {
        if (!sharing.SessionNotes && !sharing.Feedbacks)
        {
            return [];
        }

        var rows = await ownSessions
            .Where(s => s.Status == SessionStatus.Completed && (scope.ExcludeSessionId == null || s.Id != scope.ExcludeSessionId))
            .OrderByDescending(s => s.Schedule.Date).ThenByDescending(s => s.Schedule.StartTime)
            .Take(scope.MaxSessions)
            .Select(s => new { s.Schedule.Date, s.Schedule.StartTime, s.Notes, s.FeedbackScore, s.FeedbackComment })
            .ToListAsync(cancellationToken);

        return [.. rows
            .OrderBy(r => r.Date).ThenBy(r => r.StartTime)
            .Select(r => new SessionEntry(
                r.Date,
                sharing.SessionNotes ? Truncate(r.Notes) : null,
                sharing.Feedbacks ? r.FeedbackScore : null,
                sharing.Feedbacks ? Truncate(r.FeedbackComment) : null))
            .Where(e => e.Notes is not null || e.FeedbackScore is not null)];
    }

    private async Task<IReadOnlyList<RecordEntry>> LoadRecordsAsync(
        Guid patientId, AiAccess access, ClinicalScope scope, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        if (!scope.IncludeRecords || !access.Sharing.MedicalRecords)
        {
            return [];
        }

        var rows = await db.MedicalRecords
            .Where(r => r.PatientId == patientId && r.PsychologistId == access.PsychologistId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(ClinicalScope.MaxRecords)
            .Select(r => new { r.CreatedAt, r.Title, r.Content })
            .ToListAsync(cancellationToken);

        return [.. rows
            .OrderBy(r => r.CreatedAt)
            .Select(r => new RecordEntry(
                DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(r.CreatedAt, timeZone).DateTime),
                r.Title,
                Truncate(r.Content) ?? string.Empty))];
    }

    private static int? AgeAt(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not { } birth)
        {
            return null;
        }

        int age = today.Year - birth.Year;
        return birth > today.AddYears(-age) ? age - 1 : age;
    }

    private static string? Truncate(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null
        : text.Length <= MaxEntryLength ? text.Trim()
        : $"{text[..MaxEntryLength].TrimEnd()}… (texto cortado)";
}
