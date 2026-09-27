using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Recurrences;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Recurrences;

public sealed class RecurrencesTests(ApiFactory factory) : SchedulingTest(factory)
{
    private Task<HttpResponseMessage> PostRecurrenceAsync(TestAccount account, object body) =>
        CreateClient(account).PostAsJsonAsync("/api/v1/recurrences", body, Ct);

    private async Task<RecurrenceGenerationResponse> CreateRecurrenceAsync(TestAccount account, object body)
    {
        HttpResponseMessage response = await PostRecurrenceAsync(account, body);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return await ReadAsync<RecurrenceGenerationResponse>(response);
    }

    private async Task<List<SessionListItem>> ListSessionsAsync(TestAccount account, Guid patientId, string? status = null)
    {
        string url = $"/api/v1/sessions?from={Today}&patientId={patientId}&pageSize=100" + (status is null ? string.Empty : $"&status={status}");
        return [.. (await CreateClient(account).GetFromJsonAsync<PagedResponse<SessionListItem>>(url, Json, Ct))!.Items];
    }

    [Fact]
    public async Task CreateWeekly_Generates3MonthsOfSessionsWithTheRecurrencePrice()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        RecurrenceGenerationResponse result = await CreateRecurrenceAsync(account, new
        {
            patientId,
            type = "Weekly",
            startDate = Tomorrow,
            startTime = "14:00",
            price = 170m,
        });

        result.Recurrence.Type.ShouldBe(RecurrenceType.Weekly);
        result.Recurrence.GeneratedUntil.ShouldBe(new DateOnly(2027, 1, 5));
        result.Scheduled.Count.ShouldBe(14);
        result.Scheduled.ShouldAllBe(date => date.DayOfWeek == DayOfWeek.Tuesday);
        result.Skipped.ShouldBeEmpty();

        List<SessionListItem> sessions = await ListSessionsAsync(account, patientId);
        sessions.Count.ShouldBe(14);
        SessionResponse first = (await CreateClient(account).GetFromJsonAsync<SessionResponse>($"/api/v1/sessions/{sessions[0].Id}", Json, Ct))!;
        first.RecurrenceId.ShouldBe(result.Recurrence.Id);
        first.Payment!.Amount.ShouldBe(170m);
        first.Payment.Status.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Create_SkipsConflictsAndBlockedDays_ReportingEachSkippedDate()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        await CreateSessionAsync(account, patientId, "2026-10-13", "14:30");
        (await CreateClient(account).PostAsJsonAsync("/api/v1/schedule-blocks", new { startDate = "2026-10-20", reason = "Congresso" }, Ct))
            .EnsureSuccessStatusCode();

        RecurrenceGenerationResponse result = await CreateRecurrenceAsync(account, new
        {
            patientId,
            type = "Weekly",
            startDate = Tomorrow,
            startTime = "14:00",
            endDate = "2026-10-27",
        });

        result.Scheduled.ShouldBe([new DateOnly(2026, 10, 6), new DateOnly(2026, 10, 27)]);
        result.Skipped.Select(s => (s.Date, s.Code)).ShouldBe(
        [
            (new DateOnly(2026, 10, 13), "scheduling.conflict"),
            (new DateOnly(2026, 10, 20), "scheduling.conflict"),
        ]);
        result.Recurrence.GeneratedUntil.ShouldBe(new DateOnly(2026, 10, 27));
    }

    [Fact]
    public async Task CreateMonthly_KeepsTheDayOfMonth_AndSkipsDaysOutsideWorkingHours()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        RecurrenceGenerationResponse result = await CreateRecurrenceAsync(account, new
        {
            patientId,
            type = "Monthly",
            startDate = "2026-10-15",
            startTime = "09:00",
        });

        // 15/10 (qui) e 15/12 (ter) são dias úteis; 15/11/2026 é domingo.
        result.Scheduled.ShouldBe([new DateOnly(2026, 10, 15), new DateOnly(2026, 12, 15)]);
        result.Skipped.Single().ShouldBe(new SkippedOccurrence(new DateOnly(2026, 11, 15), "scheduling.outside_working_hours", "O horário está fora do expediente do psicólogo."));
    }

    [Fact]
    public async Task Extend_GeneratesTheNext3Months()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        RecurrenceGenerationResponse created = await CreateRecurrenceAsync(account, new
        {
            patientId,
            type = "Weekly",
            startDate = Tomorrow,
            startTime = "14:00",
        });

        HttpResponseMessage response = await CreateClient(account).PostAsync($"/api/v1/recurrences/{created.Recurrence.Id}/extend", null, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        RecurrenceGenerationResponse extended = await ReadAsync<RecurrenceGenerationResponse>(response);
        extended.Scheduled[0].ShouldBe(new DateOnly(2027, 1, 12));
        extended.Scheduled.Count.ShouldBe(12);
        extended.Recurrence.GeneratedUntil.ShouldBe(new DateOnly(2027, 4, 5));
        (await ListSessionsAsync(account, patientId)).Count.ShouldBe(26);
    }

    [Fact]
    public async Task End_CancelsFutureSessionsAndPayments_KeepingPastOnes()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        RecurrenceGenerationResponse created = await CreateRecurrenceAsync(account, new
        {
            patientId,
            type = "Weekly",
            startDate = Tomorrow,
            startTime = "14:00",
            endDate = "2026-10-27",
            price = 100m,
        });
        SessionResponse firstSession = await GetSessionAsync(account,
            (await ListSessionsAsync(account, patientId)).First(s => s.Date == new DateOnly(2026, 10, 6)).Id);
        account = await CompleteSessionAsync(account, firstSession);

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync(
            $"/api/v1/recurrences/{created.Recurrence.Id}/end", new { reason = "Alta do paciente" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<RecurrenceResponse>(response)).IsActive.ShouldBeFalse();
        (await ListSessionsAsync(account, patientId, "Cancelled")).Count.ShouldBe(3);
        (await ListSessionsAsync(account, patientId, "Completed")).Single().Id.ShouldBe(firstSession.Id);
        HttpResponseMessage extendEnded = await CreateClient(account).PostAsync($"/api/v1/recurrences/{created.Recurrence.Id}/extend", null, Ct);
        extendEnded.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_ReturnsRecurrencesOfThePatient()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        await CreateRecurrenceAsync(account, new { patientId, type = "Weekly", startDate = Tomorrow, startTime = "14:00", endDate = "2026-10-06" });

        List<RecurrenceResponse> list = (await CreateClient(account).GetFromJsonAsync<List<RecurrenceResponse>>(
            $"/api/v1/recurrences?patientId={patientId}", Json, Ct))!;

        list.Single().PatientId.ShouldBe(patientId);
    }

    [Fact]
    public async Task Create_ForColleague_AsPsychologist_Returns403()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        Guid patientId = await CreatePatientAsync(owner);

        HttpResponseMessage response = await PostRecurrenceAsync(colleague, new
        {
            patientId,
            psychologistId = owner.PsychologistId,
            type = "Weekly",
            startDate = Tomorrow,
            startTime = "14:00",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("2026-10-01", null, "startDate")]
    [InlineData(Tomorrow, "2026-10-01", "endDate")]
    public async Task Create_InvalidDates_Returns422(string startDate, string? endDate, string field)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostRecurrenceAsync(account, new
        {
            patientId,
            type = "Weekly",
            startDate,
            endDate,
            startTime = "14:00",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }
}
