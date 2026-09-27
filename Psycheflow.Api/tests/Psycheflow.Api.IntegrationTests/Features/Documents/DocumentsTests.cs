using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Documents;

public sealed class DocumentsTests(ApiFactory factory) : SchedulingTest(factory)
{
    private static async Task ShouldBePdfAsync(HttpResponseMessage response)
    {
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(Ct);
        bytes.Length.ShouldBeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 4).ShouldBe("%PDF");
    }

    private async Task<(TestAccount Account, SessionResponse Session)> CompletedSessionAsync()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account, "Paciente Documento");
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "14:00", price: 180m);
        account = await CompleteSessionAsync(account, session);
        return (account, session);
    }

    [Fact]
    public async Task Receipt_OnlyForPaidPayments()
    {
        (TestAccount account, SessionResponse session) = await CompletedSessionAsync();
        HttpClient client = CreateClient(account);
        string url = $"/api/v1/documents/receipts/{session.Payment!.Id}";

        HttpResponseMessage pending = await client.GetAsync(url, Ct);
        (await client.PostAsJsonAsync($"/api/v1/payments/{session.Payment.Id}/pay", new { method = "Pix" }, Ct)).EnsureSuccessStatusCode();
        HttpResponseMessage paid = await client.GetAsync(url, Ct);

        pending.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await ShouldBePdfAsync(paid);
        paid.Content.Headers.ContentDisposition?.FileName.ShouldNotBeNull();
    }

    [Fact]
    public async Task Receipt_OfAnotherCompany_Returns404()
    {
        (_, SessionResponse session) = await CompletedSessionAsync();
        TestAccount other = await CreatePracticeAsync();

        HttpResponseMessage response = await CreateClient(other).GetAsync($"/api/v1/documents/receipts/{session.Payment!.Id}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AttendanceDeclaration_OnlyForCompletedSessions()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse scheduled = await CreateSessionAsync(account, patientId, Tomorrow, "10:00");
        SessionResponse completed = await CreateSessionAsync(account, patientId, Tomorrow, "14:00");
        account = await CompleteSessionAsync(account, completed);
        HttpClient client = CreateClient(account);

        HttpResponseMessage notCompleted = await client.GetAsync($"/api/v1/documents/attendance/{scheduled.Id}", Ct);
        HttpResponseMessage ok = await client.GetAsync($"/api/v1/documents/attendance/{completed.Id}", Ct);

        notCompleted.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        await ShouldBePdfAsync(ok);
    }

    [Fact]
    public async Task SessionsReport_RequiresStartDate_AndGeneratesPdf()
    {
        (TestAccount account, _) = await CompletedSessionAsync();
        HttpClient client = CreateClient(account);

        HttpResponseMessage withoutFrom = await client.GetAsync("/api/v1/documents/sessions-report", Ct);
        HttpResponseMessage report = await client.GetAsync(
            $"/api/v1/documents/sessions-report?from={Today}&to=2026-10-31&sessionStatus=Completed&paymentStatus=Pending", Ct);

        withoutFrom.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        await ShouldBePdfAsync(report);
    }

    [Fact]
    public async Task SessionsReport_ForColleagueAsPsychologist_Returns403()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);

        HttpResponseMessage response = await CreateClient(colleague).GetAsync(
            $"/api/v1/documents/sessions-report?from={Today}&psychologistId={owner.PsychologistId}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FeedbackReport_RequiresPatient_AndIsOnlyForPsychologists()
    {
        (TestAccount account, SessionResponse session) = await CompletedSessionAsync();
        TestAccount manager = await CreateStaffAsync(account, Roles.Manager);
        string url = $"/api/v1/documents/feedback-report?patientId={session.PatientId}&from={Today}";

        HttpResponseMessage withoutPatient = await CreateClient(account).GetAsync($"/api/v1/documents/feedback-report?from={Today}", Ct);
        HttpResponseMessage asManager = await CreateClient(manager).GetAsync(url, Ct);
        HttpResponseMessage asPsychologist = await CreateClient(account).GetAsync(url, Ct);

        withoutPatient.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        asManager.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        await ShouldBePdfAsync(asPsychologist);
    }
}
