using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Sessions;

public sealed class CreateSessionTests(ApiFactory factory) : SchedulingTest(factory)
{
    [Fact]
    public async Task Create_WithinWorkingHours_UsesDefaultDurationFromSettings()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account, "Paciente Um");

        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        session.PsychologistId.ShouldBe(account.PsychologistId!.Value);
        session.PatientName.ShouldBe("Paciente Um");
        session.Date.ShouldBe(new DateOnly(2026, 10, 6));
        session.StartTime.ShouldBe(new TimeOnly(14, 0));
        session.EndTime.ShouldBe(new TimeOnly(14, 50));
        session.DurationMinutes.ShouldBe(50);
        session.Status.ShouldBe(SessionStatus.Scheduled);
        session.ScheduleStatus.ShouldBe(ScheduleStatus.Pending);
    }

    [Fact]
    public async Task Create_WithExplicitDuration_UsesIt()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "08:00", durationMinutes: 90);

        session.EndTime.ShouldBe(new TimeOnly(9, 30));
    }

    [Theory]
    [InlineData(Tomorrow, "11:30")]
    [InlineData(Tomorrow, "07:00")]
    [InlineData(Saturday, "09:00")]
    public async Task Create_OutsideWorkingHours_Returns422(string date, string startTime)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostSessionAsync(account, patientId, date, startTime);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Extensions["code"]!.ToString().ShouldBe("scheduling.outside_working_hours");
    }

    [Fact]
    public async Task Create_InThePast_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostSessionAsync(account, patientId, Today, "08:00");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Extensions["code"]!.ToString().ShouldBe("scheduling.in_the_past");
    }

    [Fact]
    public async Task Create_LaterToday_IsAllowed()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostSessionAsync(account, patientId, Today, "10:00");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_OverlappingAnotherSession_Returns409_ButCancelledSessionFreesTheSlot()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse first = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");

        HttpResponseMessage conflict = await PostSessionAsync(account, patientId, Tomorrow, "14:30");
        conflict.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await PostActionAsync(account, first.Id, "cancel", new { reason = "Paciente viajou" })).EnsureSuccessStatusCode();
        (await PostSessionAsync(account, patientId, Tomorrow, "14:30")).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_BackToBackSessions_AreAllowed()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        await CreateSessionAsync(account, patientId, Tomorrow, "14:00", durationMinutes: 60);

        (await PostSessionAsync(account, patientId, Tomorrow, "15:00", durationMinutes: 60)).StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(300)]
    public async Task Create_InvalidDuration_Returns422(int minutes)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await PostSessionAsync(account, patientId, Tomorrow, "14:00", minutes);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey("durationMinutes");
    }

    [Fact]
    public async Task Create_PatientOfAnotherCompany_Returns404()
    {
        TestAccount account = await CreatePracticeAsync();
        TestAccount other = await CreatePracticeAsync();
        Guid foreignPatient = await CreatePatientAsync(other);

        HttpResponseMessage response = await PostSessionAsync(account, foreignPatient, Tomorrow, "14:00");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_InactivePatient_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        await CreateClient(account).PutAsJsonAsync($"/api/v1/patients/{patientId}", new
        {
            fullName = "Inativo",
            email = "inativo@email.com",
            phone = "45988887777",
            status = "Inactive",
        }, Ct);

        HttpResponseMessage response = await PostSessionAsync(account, patientId, Tomorrow, "14:00");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Create_ByManagerForPsychologist_Works_ButPsychologistCannotBookForColleague()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await SetWorkingHoursAsync(colleague, colleague.PsychologistId!.Value);
        Guid patientId = await CreatePatientAsync(owner);

        HttpResponseMessage byManager = await PostSessionAsync(manager, patientId, Tomorrow, "14:00", psychologistId: colleague.PsychologistId);
        HttpResponseMessage byColleague = await PostSessionAsync(colleague, patientId, Tomorrow, "15:00", psychologistId: owner.PsychologistId);
        HttpResponseMessage managerWithoutPsychologist = await PostSessionAsync(manager, patientId, Tomorrow, "16:00");

        byManager.StatusCode.ShouldBe(HttpStatusCode.Created);
        byColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        managerWithoutPsychologist.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }
}
