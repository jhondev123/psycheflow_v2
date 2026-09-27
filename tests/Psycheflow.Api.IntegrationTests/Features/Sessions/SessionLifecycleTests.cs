using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Sessions;

public sealed class SessionLifecycleTests(ApiFactory factory) : SchedulingTest(factory)
{
    [Fact]
    public async Task Reschedule_MovesSessionAndRecordsReason_IgnoringItsOwnSlot()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage response = await PostActionAsync(account, session.Id, "reschedule", new
        {
            date = Tomorrow,
            startTime = "14:30",
            reason = "Paciente pediu para atrasar",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse moved = await ReadAsync<SessionResponse>(response);
        moved.StartTime.ShouldBe(new TimeOnly(14, 30));
        moved.EndTime.ShouldBe(new TimeOnly(15, 20));
        moved.RescheduleReason.ShouldBe("Paciente pediu para atrasar");
    }

    [Fact]
    public async Task Reschedule_WithoutReason_Returns422_AndIntoConflict_Returns409()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        await CreateSessionAsync(account, patientId, Tomorrow, "16:00");

        HttpResponseMessage withoutReason = await PostActionAsync(account, session.Id, "reschedule", new { date = Tomorrow, startTime = "15:00" });
        HttpResponseMessage conflict = await PostActionAsync(account, session.Id, "reschedule", new { date = Tomorrow, startTime = "15:30", reason = "Ajuste" });

        withoutReason.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(withoutReason)).Errors.ShouldContainKey("reason");
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Cancel_RequiresReason_MarksSessionAndScheduleCancelled()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage withoutReason = await PostActionAsync(account, session.Id, "cancel", new { reason = "" });
        HttpResponseMessage cancelled = await PostActionAsync(account, session.Id, "cancel", new { reason = "Imprevisto" });
        HttpResponseMessage cancelledAgain = await PostActionAsync(account, session.Id, "cancel", new { reason = "De novo" });

        withoutReason.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        cancelled.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse result = await ReadAsync<SessionResponse>(cancelled);
        result.Status.ShouldBe(SessionStatus.Cancelled);
        result.ScheduleStatus.ShouldBe(ScheduleStatus.Cancelled);
        result.CancellationReason.ShouldBe("Imprevisto");
        cancelledAgain.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Confirm_SetsScheduleConfirmed()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage response = await PostActionAsync(account, session.Id, "confirm");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<SessionResponse>(response)).ScheduleStatus.ShouldBe(ScheduleStatus.Confirmed);
    }

    [Fact]
    public async Task Complete_AfterStart_SavesNotesAndScore_ThenSessionIsLocked()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        TravelTo(new DateOnly(2026, 10, 6), new TimeOnly(14, 55));
        account = await RefreshAsync(account);

        HttpResponseMessage response = await PostActionAsync(account, session.Id, "complete", new
        {
            notes = "## Evolução\nPaciente relatou melhora.",
            feedbackScore = 8,
            feedbackComment = "Sessão produtiva",
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse completed = await ReadAsync<SessionResponse>(response);
        completed.Status.ShouldBe(SessionStatus.Completed);
        completed.Notes.ShouldBe("## Evolução\nPaciente relatou melhora.");
        completed.FeedbackScore.ShouldBe(8);

        HttpResponseMessage reschedule = await PostActionAsync(account, session.Id, "reschedule", new { date = "2026-10-07", startTime = "14:00", reason = "x" });
        HttpResponseMessage cancel = await PostActionAsync(account, session.Id, "cancel", new { reason = "x" });
        reschedule.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        cancel.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Theory]
    [InlineData(null, 8, "notes")]
    [InlineData("Anotações", 11, "feedbackScore")]
    [InlineData("Anotações", -1, "feedbackScore")]
    public async Task Complete_InvalidData_Returns422(string? notes, int score, string field)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        TravelTo(new DateOnly(2026, 10, 6), new TimeOnly(15, 0));
        account = await RefreshAsync(account);

        HttpResponseMessage response = await PostActionAsync(account, session.Id, "complete", new { notes, feedbackScore = score });

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }

    [Fact]
    public async Task Complete_BeforeSessionStarts_Returns409()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage response = await PostActionAsync(account, session.Id, "complete", new { notes = "x", feedbackScore = 5 });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task NoShow_OnlyAfterSessionStart()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage early = await PostActionAsync(account, session.Id, "no-show");
        TravelTo(new DateOnly(2026, 10, 6), new TimeOnly(14, 20));
        account = await RefreshAsync(account);
        HttpResponseMessage onTime = await PostActionAsync(account, session.Id, "no-show");

        early.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        onTime.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<SessionResponse>(onTime)).Status.ShouldBe(SessionStatus.NoShow);
    }

    [Fact]
    public async Task Get_ClinicalNotesVisibleOnlyToTheSessionPsychologist()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        Guid patientId = await CreatePatientAsync(owner);
        SessionResponse session = await CreateSessionAsync(owner, patientId, Tomorrow, "14:00");
        TravelTo(new DateOnly(2026, 10, 6), new TimeOnly(15, 0));
        owner = await RefreshAsync(owner);
        manager = await RefreshAsync(manager);
        colleague = await RefreshAsync(colleague);
        (await PostActionAsync(owner, session.Id, "complete", new { notes = "Sigiloso", feedbackScore = 9 })).EnsureSuccessStatusCode();

        SessionResponse asOwner = await GetSessionAsync(owner, session.Id);
        SessionResponse asManager = await GetSessionAsync(manager, session.Id);
        HttpResponseMessage asColleague = await CreateClient(colleague).GetAsync($"/api/v1/sessions/{session.Id}", Ct);

        asOwner.Notes.ShouldBe("Sigiloso");
        asOwner.ClinicalNotesVisible.ShouldBeTrue();
        asManager.Notes.ShouldBeNull();
        asManager.FeedbackScore.ShouldBeNull();
        asManager.ClinicalNotesVisible.ShouldBeFalse();
        asManager.Status.ShouldBe(SessionStatus.Completed);
        asColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_ChangesNotesAndPatient_ButManagerCannotWriteNotes()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        Guid patientA = await CreatePatientAsync(owner);
        Guid patientB = await CreatePatientAsync(owner, "Paciente B");
        SessionResponse session = await CreateSessionAsync(owner, patientA, Tomorrow, "14:00");

        HttpResponseMessage byOwner = await CreateClient(owner).PutAsJsonAsync($"/api/v1/sessions/{session.Id}", new { patientId = patientB, notes = "Preparar escala" }, Ct);
        HttpResponseMessage managerNotes = await CreateClient(manager).PutAsJsonAsync($"/api/v1/sessions/{session.Id}", new { patientId = patientA, notes = "x" }, Ct);
        HttpResponseMessage managerPatient = await CreateClient(manager).PutAsJsonAsync($"/api/v1/sessions/{session.Id}", new { patientId = patientA }, Ct);

        byOwner.StatusCode.ShouldBe(HttpStatusCode.OK);
        SessionResponse updated = await ReadAsync<SessionResponse>(byOwner);
        updated.PatientName.ShouldBe("Paciente B");
        updated.Notes.ShouldBe("Preparar escala");
        managerNotes.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        managerPatient.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetSessionAsync(owner, session.Id)).Notes.ShouldBe("Preparar escala");
    }

    [Fact]
    public async Task Delete_SoftDeletesAndFreesTheSlot_ButCompletedSessionCannotBeDeleted()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse toDelete = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        SessionResponse toComplete = await CreateSessionAsync(account, patientId, Today, "10:00");

        HttpResponseMessage deleted = await CreateClient(account).DeleteAsync($"/api/v1/sessions/{toDelete.Id}", Ct);
        TravelTo(new DateOnly(2026, 10, 5), new TimeOnly(11, 0));
        account = await RefreshAsync(account);
        (await PostActionAsync(account, toComplete.Id, "complete", new { notes = "ok", feedbackScore = 7 })).EnsureSuccessStatusCode();
        HttpResponseMessage deleteCompleted = await CreateClient(account).DeleteAsync($"/api/v1/sessions/{toComplete.Id}", Ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CreateClient(account).GetAsync($"/api/v1/sessions/{toDelete.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await PostSessionAsync(account, patientId, Tomorrow, "14:00")).StatusCode.ShouldBe(HttpStatusCode.Created);
        deleteCompleted.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task List_FiltersByPeriodPatientAndStatus_AndPsychologistSeesOnlyOwnSessions()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await SetWorkingHoursAsync(colleague, colleague.PsychologistId!.Value);
        Guid patientA = await CreatePatientAsync(owner);
        Guid patientB = await CreatePatientAsync(owner);
        SessionResponse s1 = await CreateSessionAsync(owner, patientA, Tomorrow, "08:00");
        SessionResponse s2 = await CreateSessionAsync(owner, patientB, Tomorrow, "09:00");
        SessionResponse s3 = await CreateSessionAsync(owner, patientA, "2026-10-08", "08:00");
        await CreateSessionAsync(colleague, patientA, Tomorrow, "08:00");
        (await PostActionAsync(owner, s2.Id, "cancel", new { reason = "x" })).EnsureSuccessStatusCode();
        HttpClient client = CreateClient(owner);

        PagedResponse<SessionListItem> all = (await client.GetFromJsonAsync<PagedResponse<SessionListItem>>($"/api/v1/sessions?from={Tomorrow}&psychologistId={owner.PsychologistId}", Json, Ct))!;
        PagedResponse<SessionListItem> oneDay = (await client.GetFromJsonAsync<PagedResponse<SessionListItem>>($"/api/v1/sessions?from={Tomorrow}&to={Tomorrow}&psychologistId={owner.PsychologistId}", Json, Ct))!;
        PagedResponse<SessionListItem> byPatient = (await client.GetFromJsonAsync<PagedResponse<SessionListItem>>($"/api/v1/sessions?from={Tomorrow}&patientId={patientA}&psychologistId={owner.PsychologistId}", Json, Ct))!;
        PagedResponse<SessionListItem> cancelled = (await client.GetFromJsonAsync<PagedResponse<SessionListItem>>($"/api/v1/sessions?from={Tomorrow}&status=Cancelled", Json, Ct))!;
        PagedResponse<SessionListItem> asColleague = (await CreateClient(colleague).GetFromJsonAsync<PagedResponse<SessionListItem>>($"/api/v1/sessions?from={Tomorrow}", Json, Ct))!;
        HttpResponseMessage colleagueForOwner = await CreateClient(colleague).GetAsync($"/api/v1/sessions?from={Tomorrow}&psychologistId={owner.PsychologistId}", Ct);
        HttpResponseMessage withoutFrom = await client.GetAsync("/api/v1/sessions", Ct);

        all.Items.Select(i => i.Id).ShouldBe([s1.Id, s2.Id, s3.Id]);
        oneDay.TotalCount.ShouldBe(2);
        byPatient.Items.Select(i => i.Id).ShouldBe([s1.Id, s3.Id]);
        cancelled.Items.Single().Id.ShouldBe(s2.Id);
        asColleague.Items.ShouldAllBe(i => i.PsychologistId == colleague.PsychologistId);
        asColleague.TotalCount.ShouldBe(1);
        colleagueForOwner.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        withoutFrom.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
