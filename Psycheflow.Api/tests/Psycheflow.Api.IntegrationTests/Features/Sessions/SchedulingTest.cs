using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Sessions;

/// <summary>
/// Base dos testes de agenda. "Agora" é segunda-feira 05/10/2026 09:00 (São Paulo) e os psicólogos atendem
/// de segunda a sexta, 08:00–12:00 e 13:00–18:00.
/// </summary>
public abstract class SchedulingTest(ApiFactory factory) : IntegrationTest(factory)
{
    protected const string Today = "2026-10-05";
    protected const string Tomorrow = "2026-10-06";
    protected const string Saturday = "2026-10-10";

    protected static readonly object WeekdayHours = new
    {
        hours = Enumerable.Range((int)DayOfWeek.Monday, 5).SelectMany(day => new[]
        {
            new { dayOfWeek = ((DayOfWeek)day).ToString(), startTime = "08:00", endTime = "12:00" },
            new { dayOfWeek = ((DayOfWeek)day).ToString(), startTime = "13:00", endTime = "18:00" },
        }).ToArray(),
    };

    /// <summary>Nova empresa cujo responsável (Admin + Psicólogo) já tem expediente definido.</summary>
    protected async Task<TestAccount> CreatePracticeAsync()
    {
        TestAccount account = await RegisterAccountAsync();
        await SetWorkingHoursAsync(account, account.PsychologistId!.Value);
        return account;
    }

    protected async Task SetWorkingHoursAsync(TestAccount caller, Guid psychologistId)
    {
        HttpResponseMessage response = await CreateClient(caller).PutAsJsonAsync(
            $"/api/v1/psychologists/{psychologistId}/working-hours", WeekdayHours, Ct);
        response.EnsureSuccessStatusCode();
    }

    protected async Task<Guid> CreatePatientAsync(TestAccount account, string? fullName = null)
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync(
            "/api/v1/patients", TestPatients.Payload(fullName: fullName), Ct);
        response.EnsureSuccessStatusCode();
        return (await ReadAsync<PatientResponse>(response)).Id;
    }

    protected Task<HttpResponseMessage> PostSessionAsync(
        TestAccount account, Guid patientId, string date, string startTime, int? durationMinutes = null, Guid? psychologistId = null) =>
        CreateClient(account).PostAsJsonAsync("/api/v1/sessions", new
        {
            patientId,
            psychologistId,
            date,
            startTime,
            durationMinutes,
        }, Ct);

    protected async Task<SessionResponse> CreateSessionAsync(
        TestAccount account, Guid patientId, string date, string startTime, int? durationMinutes = null, Guid? psychologistId = null)
    {
        HttpResponseMessage response = await PostSessionAsync(account, patientId, date, startTime, durationMinutes, psychologistId);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return await ReadAsync<SessionResponse>(response);
    }

    protected Task<HttpResponseMessage> PostActionAsync(TestAccount account, Guid sessionId, string action, object? body = null) =>
        CreateClient(account).PostAsJsonAsync($"/api/v1/sessions/{sessionId}/{action}", body ?? new { }, Ct);

    protected async Task<SessionResponse> GetSessionAsync(TestAccount account, Guid sessionId) =>
        (await CreateClient(account).GetFromJsonAsync<SessionResponse>($"/api/v1/sessions/{sessionId}", Json, Ct))!;
}
