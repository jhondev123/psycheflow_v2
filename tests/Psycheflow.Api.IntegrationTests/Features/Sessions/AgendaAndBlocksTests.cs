using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Scheduling.GetAgenda;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Sessions;

public sealed class AgendaAndBlocksTests(ApiFactory factory) : SchedulingTest(factory)
{
    private Task<HttpResponseMessage> PostBlockAsync(TestAccount account, object body) =>
        CreateClient(account).PostAsJsonAsync("/api/v1/schedule-blocks", body, Ct);

    [Fact]
    public async Task CreateBlock_ByTime_OutsideWorkingHours_IsAllowed()
    {
        TestAccount account = await CreatePracticeAsync();

        HttpResponseMessage response = await PostBlockAsync(account, new
        {
            startDate = Tomorrow,
            startTime = "12:00",
            endTime = "13:00",
            reason = "Almoço com equipe",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        List<BlockResponse> blocks = await ReadAsync<List<BlockResponse>>(response);
        blocks.Single().Reason.ShouldBe("Almoço com equipe");
        blocks.Single().PsychologistId.ShouldBe(account.PsychologistId!.Value);
    }

    [Fact]
    public async Task CreateBlock_WholeDays_CreatesOneBlockPerDay_AndPreventsSessions()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostBlockAsync(account, new { startDate = "2026-10-07", endDate = "2026-10-09", reason = "Congresso" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        List<BlockResponse> blocks = await ReadAsync<List<BlockResponse>>(response);
        blocks.Select(b => b.Date).ShouldBe([new DateOnly(2026, 10, 7), new DateOnly(2026, 10, 8), new DateOnly(2026, 10, 9)]);
        blocks.ShouldAllBe(b => b.StartTime == TimeOnly.MinValue);
        (await PostSessionAsync(account, patientId, "2026-10-08", "14:00")).StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateBlock_ConflictingWithSession_Returns409_AndCreatesNothing()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        await CreateSessionAsync(account, patientId, "2026-10-08", "14:00");

        HttpResponseMessage response = await PostBlockAsync(account, new { startDate = "2026-10-07", endDate = "2026-10-09" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        AgendaResponse agenda = await GetAgendaAsync(account, "2026-10-07", "2026-10-09");
        agenda.Items.ShouldNotContain(i => i.Type == ScheduleType.Block);
    }

    [Theory]
    [InlineData("2026-10-09", "2026-10-07", null, null, "endDate")]
    [InlineData(Tomorrow, null, "13:00", null, "endTime")]
    [InlineData(Tomorrow, null, "13:00", "12:00", "endTime")]
    [InlineData("2026-10-01", "2026-12-31", null, null, "endDate")]
    public async Task CreateBlock_InvalidRange_Returns422(string startDate, string? endDate, string? startTime, string? endTime, string field)
    {
        TestAccount account = await CreatePracticeAsync();

        HttpResponseMessage response = await PostBlockAsync(account, new { startDate, endDate, startTime, endTime });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }

    [Fact]
    public async Task DeleteBlock_FreesTheSlot()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        HttpResponseMessage created = await PostBlockAsync(account, new { startDate = Tomorrow, startTime = "14:00", endTime = "16:00" });
        BlockResponse block = (await ReadAsync<List<BlockResponse>>(created)).Single();

        HttpResponseMessage deleted = await CreateClient(account).DeleteAsync($"/api/v1/schedule-blocks/{block.Id}", Ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await PostSessionAsync(account, patientId, Tomorrow, "14:00")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Agenda_ReturnsSessionsBlocksAndWorkingHoursInThePeriod()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account, "Paciente Agenda");
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        await CreateSessionAsync(account, patientId, "2026-10-13", "14:00");
        await PostBlockAsync(account, new { startDate = Tomorrow, startTime = "12:00", endTime = "13:00", reason = "Almoço" });

        AgendaResponse agenda = await GetAgendaAsync(account, Today, "2026-10-11");

        agenda.WorkingHours.Single().PsychologistId.ShouldBe(account.PsychologistId!.Value);
        agenda.WorkingHours.Single().Hours.Count.ShouldBe(10);
        agenda.Items.Count.ShouldBe(2);
        AgendaItem sessionItem = agenda.Items.Single(i => i.Type == ScheduleType.Session);
        sessionItem.SessionId.ShouldBe(session.Id);
        sessionItem.PatientName.ShouldBe("Paciente Agenda");
        agenda.Items.Single(i => i.Type == ScheduleType.Block).BlockReason.ShouldBe("Almoço");
    }

    [Fact]
    public async Task Agenda_PsychologistCannotSeeColleague_ManagerSeesEveryone()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        await SetWorkingHoursAsync(colleague, colleague.PsychologistId!.Value);
        Guid patientId = await CreatePatientAsync(owner);
        await CreateSessionAsync(owner, patientId, Tomorrow, "14:00");
        await CreateSessionAsync(colleague, patientId, Tomorrow, "14:00");

        HttpResponseMessage colleagueView = await CreateClient(colleague).GetAsync($"/api/v1/agenda?from={Today}&to={Tomorrow}&psychologistId={owner.PsychologistId}", Ct);
        AgendaResponse colleagueOwn = (await CreateClient(colleague).GetFromJsonAsync<AgendaResponse>($"/api/v1/agenda?from={Today}&to={Tomorrow}", Json, Ct))!;
        AgendaResponse managerView = (await CreateClient(manager).GetFromJsonAsync<AgendaResponse>($"/api/v1/agenda?from={Today}&to={Tomorrow}", Json, Ct))!;

        colleagueView.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        colleagueOwn.Items.ShouldAllBe(i => i.PsychologistId == colleague.PsychologistId);
        managerView.Items.Count.ShouldBe(2);
        managerView.WorkingHours.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Agenda_PeriodLongerThan62Days_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();

        HttpResponseMessage response = await CreateClient(account).GetAsync("/api/v1/agenda?from=2026-10-01&to=2026-12-31", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ListPatients_FiltersByLastCompletedSession()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid withSession = await CreatePatientAsync(account, "Com Sessão");
        await CreatePatientAsync(account, "Sem Sessão");
        SessionResponse session = await CreateSessionAsync(account, withSession, Today, "10:00");
        TravelTo(new DateOnly(2026, 10, 5), new TimeOnly(11, 0));
        account = await RefreshAsync(account);
        (await PostActionAsync(account, session.Id, "complete", new { notes = "ok", feedbackScore = 6 })).EnsureSuccessStatusCode();
        HttpClient client = CreateClient(account);

        PagedResponse<PatientListItem> inPeriod = (await client.GetFromJsonAsync<PagedResponse<PatientListItem>>(
            "/api/v1/patients?lastSessionFrom=2026-10-01&lastSessionTo=2026-10-31", Json, Ct))!;
        PagedResponse<PatientListItem> all = (await client.GetFromJsonAsync<PagedResponse<PatientListItem>>("/api/v1/patients", Json, Ct))!;

        inPeriod.Items.Single().FullName.ShouldBe("Com Sessão");
        all.Items.Single(p => p.FullName == "Com Sessão").LastSessionDate.ShouldBe(new DateOnly(2026, 10, 5));
        all.Items.Single(p => p.FullName == "Sem Sessão").LastSessionDate.ShouldBeNull();
    }

    private async Task<AgendaResponse> GetAgendaAsync(TestAccount account, string from, string to) =>
        (await CreateClient(account).GetFromJsonAsync<AgendaResponse>($"/api/v1/agenda?from={from}&to={to}", Json, Ct))!;
}
