using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Dashboard;
using Psycheflow.Api.Features.Scheduling;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Dashboard;

public sealed class DashboardTests(ApiFactory factory) : SchedulingTest(factory)
{
    private const string NextMonday = "2026-10-12";

    private async Task<DashboardResponse> GetDashboardAsync(TestAccount account, Guid? psychologistId = null)
    {
        string query = psychologistId is null ? string.Empty : $"?psychologistId={psychologistId}";
        HttpResponseMessage response = await CreateClient(account).GetAsync($"/api/v1/dashboard{query}", Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        return await ReadAsync<DashboardResponse>(response);
    }

    private async Task<(TestAccount Owner, TestAccount Colleague, Guid PatientId)> ClinicWithAgendaAsync()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await SetWorkingHoursAsync(owner, colleague.PsychologistId!.Value);
        Guid patientId = await CreatePatientAsync(owner, "Paciente do Painel");

        SessionResponse confirmed = await CreateSessionAsync(owner, patientId, Today, "10:00");
        (await PostActionAsync(owner, confirmed.Id, "confirm")).EnsureSuccessStatusCode();
        await CreateSessionAsync(owner, patientId, Today, "14:00");
        await CreateSessionAsync(owner, patientId, Tomorrow, "10:00");
        await CreateSessionAsync(owner, patientId, NextMonday, "10:00");
        SessionResponse cancelled = await CreateSessionAsync(owner, patientId, Today, "16:00");
        (await PostActionAsync(owner, cancelled.Id, "cancel", new { reason = "Imprevisto" })).EnsureSuccessStatusCode();
        (await CreateClient(owner).PostAsJsonAsync("/api/v1/schedule-blocks", new { startDate = Today, startTime = "12:00", endTime = "13:00", reason = "Almoço" }, Ct))
            .EnsureSuccessStatusCode();

        await CreateSessionAsync(colleague, patientId, Today, "11:00");
        return (owner, colleague, patientId);
    }

    [Fact]
    public async Task Dashboard_ForOnePsychologist_SummarizesTheirDayAndWeek()
    {
        (TestAccount owner, _, _) = await ClinicWithAgendaAsync();

        DashboardResponse dashboard = await GetDashboardAsync(owner, owner.PsychologistId);

        dashboard.Today.ShouldBe(new DateOnly(2026, 10, 5));
        dashboard.WeekStart.ShouldBe(new DateOnly(2026, 10, 5));
        dashboard.WeekEnd.ShouldBe(new DateOnly(2026, 10, 11));
        dashboard.ActivePatients.ShouldBe(1);
        dashboard.SessionsToday.ShouldBe(2);
        dashboard.ConfirmedToday.ShouldBe(1);
        dashboard.SessionsThisWeek.ShouldBe(3);
        dashboard.PendingConfirmations.ShouldBe(3);

        dashboard.TodayItems.Select(i => (i.StartTime, i.Type)).ShouldBe(
        [
            (new TimeOnly(10, 0), ScheduleType.Session),
            (new TimeOnly(12, 0), ScheduleType.Block),
            (new TimeOnly(14, 0), ScheduleType.Session),
        ]);
        dashboard.TodayItems[1].BlockReason.ShouldBe("Almoço");
        dashboard.TodayItems[0].PatientName.ShouldBe("Paciente do Painel");

        dashboard.Upcoming.Select(u => (u.Date, u.StartTime)).ShouldBe(
        [
            (new DateOnly(2026, 10, 5), new TimeOnly(10, 0)),
            (new DateOnly(2026, 10, 5), new TimeOnly(14, 0)),
            (new DateOnly(2026, 10, 6), new TimeOnly(10, 0)),
            (new DateOnly(2026, 10, 12), new TimeOnly(10, 0)),
        ]);
        dashboard.Upcoming[0].ScheduleStatus.ShouldBe(ScheduleStatus.Confirmed);
    }

    [Fact]
    public async Task Dashboard_UpcomingSkipsSessionsThatAlreadyStarted()
    {
        (TestAccount owner, _, _) = await ClinicWithAgendaAsync();
        TravelTo(new DateOnly(2026, 10, 5), new TimeOnly(10, 30));
        owner = await RefreshAsync(owner);

        DashboardResponse dashboard = await GetDashboardAsync(owner, owner.PsychologistId);

        dashboard.Upcoming[0].StartTime.ShouldBe(new TimeOnly(14, 0));
    }

    [Fact]
    public async Task Dashboard_ManagementSeesTheWholeClinic_PsychologistOnlyTheirOwn()
    {
        (TestAccount owner, TestAccount colleague, _) = await ClinicWithAgendaAsync();

        DashboardResponse clinic = await GetDashboardAsync(owner);
        DashboardResponse own = await GetDashboardAsync(colleague);
        HttpResponseMessage colleagueAsksForOwner = await CreateClient(colleague).GetAsync($"/api/v1/dashboard?psychologistId={owner.PsychologistId}", Ct);

        clinic.SessionsToday.ShouldBe(3);
        own.SessionsToday.ShouldBe(1);
        own.TodayItems.ShouldHaveSingleItem().StartTime.ShouldBe(new TimeOnly(11, 0));
        colleagueAsksForOwner.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dashboard_Finance_ShowsWhatIsPendingAndWhatWasReceivedThisMonth()
    {
        TestAccount owner = await CreatePracticeAsync();
        await SetDefaultPriceAsync(owner, 150);
        Guid patientId = await CreatePatientAsync(owner);
        SessionResponse paid = await CreateSessionAsync(owner, patientId, Today, "10:00");
        SessionResponse unpaid = await CreateSessionAsync(owner, patientId, Today, "11:00", price: 200);
        await CreateSessionAsync(owner, patientId, Tomorrow, "10:00");

        TravelTo(new DateOnly(2026, 10, 5), new TimeOnly(11, 55));
        owner = await RefreshAsync(owner);
        foreach (SessionResponse session in new[] { paid, unpaid })
        {
            (await PostActionAsync(owner, session.Id, "complete", new { notes = "Evolução.", feedbackScore = 8 })).EnsureSuccessStatusCode();
        }

        Guid paymentId = (await GetSessionAsync(owner, paid.Id)).Payment!.Id;
        (await CreateClient(owner).PostAsJsonAsync($"/api/v1/payments/{paymentId}/pay", new { method = "Pix" }, Ct)).EnsureSuccessStatusCode();

        DashboardResponse dashboard = await GetDashboardAsync(owner);

        dashboard.Finance.PendingPayments.ShouldBe(1);
        dashboard.Finance.PendingAmount.ShouldBe(200m);
        dashboard.Finance.ReceivedThisMonth.ShouldBe(150m);
    }
}
