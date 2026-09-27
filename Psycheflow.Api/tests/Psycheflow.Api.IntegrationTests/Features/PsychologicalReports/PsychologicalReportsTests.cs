using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.PsychologicalReports;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.PsychologicalReports;

public sealed class PsychologicalReportsTests(ApiFactory factory) : SchedulingTest(factory)
{
    private static object DraftPayload(Guid patientId) => new
    {
        patientId,
        template = "PsychologicalReport",
        purpose = "Solicitação da escola para acompanhamento pedagógico",
        demand = "Dificuldade de concentração relatada pelos responsáveis.",
        includeSessionSummary = true,
    };

    private async Task<PsychologicalReportResponse> CreateDraftAsync(TestAccount account, Guid patientId)
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/psychological-reports", DraftPayload(patientId), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return await ReadAsync<PsychologicalReportResponse>(response);
    }

    [Fact]
    public async Task Create_ValidDraft_Returns201()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account, "Paciente Laudo");

        PsychologicalReportResponse report = await CreateDraftAsync(account, patientId);

        report.Status.ShouldBe(PsychologicalReportStatus.Draft);
        report.Template.ShouldBe(PsychologicalReportTemplate.PsychologicalReport);
        report.PatientName.ShouldBe("Paciente Laudo");
        report.PsychologistId.ShouldBe(account.PsychologistId!.Value);
    }

    [Fact]
    public async Task Create_WithoutPatientPurposeOrTemplate_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/psychological-reports", new { }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.Keys.ShouldBe(["patientId", "template", "purpose"], ignoreOrder: true);
    }

    [Fact]
    public async Task Finalize_RequiresAllSections_ThenLocksTheReport()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        PsychologicalReportResponse draft = await CreateDraftAsync(account, patientId);
        HttpClient client = CreateClient(account);
        string url = $"/api/v1/psychological-reports/{draft.Id}";

        HttpResponseMessage incomplete = await client.PostAsync($"{url}/finalize", null, Ct);
        HttpResponseMessage updated = await client.PutAsJsonAsync(url, new
        {
            template = "PsychologicalReport",
            purpose = draft.Purpose,
            demand = draft.Demand,
            procedure = "Entrevistas e 6 sessões de psicoterapia.",
            analysis = "Os dados indicam ansiedade situacional.",
            conclusion = "Recomenda-se continuidade do acompanhamento.",
            includeSessionSummary = true,
        }, Ct);
        HttpResponseMessage finalized = await client.PostAsync($"{url}/finalize", null, Ct);
        HttpResponseMessage editAfter = await client.PutAsJsonAsync(url, new { template = "PsychologicalReport", purpose = "x" }, Ct);

        incomplete.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(incomplete)).Errors.ShouldContainKey("procedure");
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        finalized.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await ReadAsync<PsychologicalReportResponse>(finalized)).Status.ShouldBe(PsychologicalReportStatus.Finalized);
        editAfter.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Pdf_IsGeneratedForDraftsAndFinalizedReports()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        PsychologicalReportResponse draft = await CreateDraftAsync(account, patientId);

        HttpResponseMessage response = await CreateClient(account).GetAsync($"/api/v1/psychological-reports/{draft.Id}/pdf", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
    }

    [Fact]
    public async Task OnlyTheAuthorPsychologist_CanAccessTheReport()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        Guid patientId = await CreatePatientAsync(owner);
        PsychologicalReportResponse draft = await CreateDraftAsync(owner, patientId);

        HttpResponseMessage asColleague = await CreateClient(colleague).GetAsync($"/api/v1/psychological-reports/{draft.Id}", Ct);
        HttpResponseMessage asManager = await CreateClient(manager).GetAsync($"/api/v1/psychological-reports/{draft.Id}/pdf", Ct);
        HttpResponseMessage managerCreates = await CreateClient(manager).PostAsJsonAsync("/api/v1/psychological-reports", DraftPayload(patientId), Ct);
        List<PsychologicalReportResponse> colleagueList = (await CreateClient(colleague).GetFromJsonAsync<List<PsychologicalReportResponse>>(
            $"/api/v1/psychological-reports?patientId={patientId}", Json, Ct))!;

        asColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        asManager.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        managerCreates.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        colleagueList.ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_OnlyDrafts()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        PsychologicalReportResponse draft = await CreateDraftAsync(account, patientId);

        HttpResponseMessage deleted = await CreateClient(account).DeleteAsync($"/api/v1/psychological-reports/{draft.Id}", Ct);

        deleted.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await CreateClient(account).GetAsync($"/api/v1/psychological-reports/{draft.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
