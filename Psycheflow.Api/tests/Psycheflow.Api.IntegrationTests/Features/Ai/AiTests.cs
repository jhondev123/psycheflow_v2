using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Ai;
using Psycheflow.Api.Features.Ai.Providers;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Sessions;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Ai;

public sealed class AiTests(ApiFactory factory) : SchedulingTest(factory)
{
    private const string PatientName = "Mariana Souza Lima";
    private const string PatientCpf = "52998224725";

    private FakeAiTextGenerator Ai => Factory.Ai;

    private static object SettingsPayload(
        bool isEnabled = true,
        string provider = "Claude",
        bool notes = true,
        bool feedbacks = true,
        bool records = true,
        bool acceptTerms = true) => new
    {
        isEnabled,
        provider,
        shareSessionNotes = notes,
        shareFeedbacks = feedbacks,
        shareMedicalRecords = records,
        acceptTerms,
    };

    private async Task EnableAiAsync(TestAccount admin, bool notes = true, bool feedbacks = true, bool records = true)
    {
        HttpResponseMessage response = await CreateClient(admin).PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(notes: notes, feedbacks: feedbacks, records: records), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
    }

    private async Task<Guid> CreateIdentifiedPatientAsync(TestAccount account)
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/patients", TestPatients.Payload(PatientCpf, PatientName), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return (await ReadAsync<PatientResponse>(response)).Id;
    }

    /// <summary>Consultório com um paciente identificável e uma sessão concluída com as anotações e o feedback informados.</summary>
    private async Task<(TestAccount Account, Guid PatientId)> PracticeWithCompletedSessionAsync(string notes, int score = 7, string? comment = null)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreateIdentifiedPatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "10:00");

        TravelTo(session.Date, session.EndTime);
        account = await RefreshAsync(account);
        HttpResponseMessage completed = await PostActionAsync(account, session.Id, "complete", new { notes, feedbackScore = score, feedbackComment = comment });
        completed.StatusCode.ShouldBe(HttpStatusCode.OK, await completed.Content.ReadAsStringAsync(Ct));

        return (account, patientId);
    }

    private async Task CreateMedicalRecordAsync(TestAccount account, Guid patientId, string title, string content)
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/medical-records", new { patientId, title, content }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
    }

    private Task<HttpResponseMessage> SuggestForPatientAsync(TestAccount account, string kind, Guid patientId) =>
        CreateClient(account).PostAsJsonAsync($"/api/v1/ai/suggestions/{kind}", new { patientId }, Ct);

    private Task<HttpResponseMessage> SuggestSessionNotesAsync(TestAccount account, Guid sessionId, string? draft) =>
        CreateClient(account).PostAsJsonAsync("/api/v1/ai/suggestions/session-notes", new { sessionId, draft }, Ct);

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await ReadAsync<ProblemDetails>(response)).Extensions["code"]?.ToString();

    private Task<int> CountUsageLogsAsync() =>
        QueryDbAsync(db => db.AiUsageLogs.IgnoreQueryFilters().CountAsync());

    [Fact]
    public async Task GetSettings_ByDefault_IsDisabledAndListsTheConfiguredProviders()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        AiSettingsResponse settings = (await CreateClient(psychologist).GetFromJsonAsync<AiSettingsResponse>("/api/v1/ai/settings", Json, Ct))!;

        settings.IsEnabled.ShouldBeFalse();
        settings.ConsentAcceptedAt.ShouldBeNull();
        settings.ShareSessionNotes.ShouldBeFalse();
        settings.AvailableProviders.ShouldBe([AiProvider.Claude]);
    }

    [Fact]
    public async Task UpdateSettings_EnableRecordsTheConsent_AndDisableClearsIt()
    {
        TestAccount admin = await RegisterAccountAsync();
        HttpClient client = CreateClient(admin);

        AiSettingsResponse enabled = await ReadAsync<AiSettingsResponse>(
            await client.PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(records: false), Ct));
        AiSettingsResponse disabled = await ReadAsync<AiSettingsResponse>(
            await client.PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(isEnabled: false, records: false, acceptTerms: false), Ct));

        enabled.IsEnabled.ShouldBeTrue();
        enabled.Provider.ShouldBe(AiProvider.Claude);
        enabled.ShareMedicalRecords.ShouldBeFalse();
        enabled.ConsentAcceptedAt.ShouldBe(ApiFactory.DefaultNow);
        disabled.IsEnabled.ShouldBeFalse();
        disabled.ShareSessionNotes.ShouldBeTrue();
        disabled.ConsentAcceptedAt.ShouldBeNull();
    }

    [Fact]
    public async Task UpdateSettings_WithoutTermsOrSharedData_Returns422()
    {
        TestAccount admin = await RegisterAccountAsync();
        HttpClient client = CreateClient(admin);

        HttpResponseMessage withoutTerms = await client.PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(acceptTerms: false), Ct);
        HttpResponseMessage withoutData = await client.PutAsJsonAsync(
            "/api/v1/ai/settings", SettingsPayload(notes: false, feedbacks: false, records: false), Ct);

        withoutTerms.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(withoutTerms)).Errors.Keys.ShouldBe(["acceptTerms"]);
        withoutData.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(withoutData)).ShouldBe(AiErrors.NoDataShared.Code);
    }

    [Fact]
    public async Task UpdateSettings_ProviderWithoutApiKey_Returns422()
    {
        TestAccount admin = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(admin).PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(provider: "OpenAi"), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.Keys.ShouldBe(["provider"]);
    }

    [Fact]
    public async Task UpdateSettings_AsPsychologist_Returns403()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        HttpResponseMessage response = await CreateClient(psychologist).PutAsJsonAsync("/api/v1/ai/settings", SettingsPayload(), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Suggestions_WhenAiIsDisabled_Return403()
    {
        (TestAccount account, Guid patientId) = await PracticeWithCompletedSessionAsync("Relata insônia.");

        HttpResponseMessage response = await SuggestForPatientAsync(account, "patient-analysis", patientId);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CodeOf(response)).ShouldBe(AiErrors.Disabled.Code);
        Ai.Prompts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionNotes_SendsAPseudonymizedPrompt_AndLogsTheUsage()
    {
        TestAccount account = await CreatePracticeAsync();
        await EnableAiAsync(account);
        Guid patientId = await CreateIdentifiedPatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "10:00");
        const string draft = "Mariana relatou insônia. Pediu retorno no (45) 98888-7777; CPF 529.982.247-25. A Sra. Lima trouxe o diário.";

        HttpResponseMessage response = await SuggestSessionNotesAsync(account, session.Id, draft);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        AiSuggestionResponse suggestion = await ReadAsync<AiSuggestionResponse>(response);
        suggestion.Suggestion.ShouldBe(FakeAiTextGenerator.DefaultResponse);
        suggestion.Provider.ShouldBe(AiProvider.Claude);
        suggestion.Model.ShouldBe(FakeAiTextGenerator.Model);
        suggestion.Disclaimer.ShouldNotBeNullOrWhiteSpace();

        AiPrompt prompt = Ai.LastPrompt;
        prompt.User.ShouldContain("[paciente] relatou insônia");
        prompt.User.ShouldContain("Idade: 36 anos");
        foreach (string identifier in new[] { "Mariana", "Souza", "Lima", "98888", "529.982.247-25", PatientCpf })
        {
            prompt.User.ShouldNotContain(identifier, Case.Insensitive);
        }

        (await CountUsageLogsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SessionNotes_WithoutDraftAndNotes_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();
        await EnableAiAsync(account);
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "10:00");

        HttpResponseMessage response = await SuggestSessionNotesAsync(account, session.Id, draft: " ");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).ShouldBe(AiErrors.EmptyDraft.Code);
    }

    [Fact]
    public async Task SessionNotes_OnlyForTheSessionPsychologist()
    {
        TestAccount owner = await CreatePracticeAsync();
        await EnableAiAsync(owner);
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        TestAccount otherCompany = await CreatePracticeAsync();
        await EnableAiAsync(otherCompany);
        Guid patientId = await CreatePatientAsync(owner);
        SessionResponse session = await CreateSessionAsync(owner, patientId, Tomorrow, "10:00");

        HttpResponseMessage asColleague = await SuggestSessionNotesAsync(colleague, session.Id, "rascunho");
        HttpResponseMessage asManager = await SuggestSessionNotesAsync(manager, session.Id, "rascunho");
        HttpResponseMessage fromOtherCompany = await SuggestSessionNotesAsync(otherCompany, session.Id, "rascunho");

        asColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CodeOf(asColleague)).ShouldBe(AiErrors.NotYourSession.Code);
        asManager.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CodeOf(asManager)).ShouldBe(AiErrors.OnlyPsychologists.Code);
        fromOtherCompany.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        Ai.Prompts.ShouldBeEmpty();
    }

    [Fact]
    public async Task SessionNotes_WhenNotesAreNotShared_Returns403()
    {
        TestAccount account = await CreatePracticeAsync();
        await EnableAiAsync(account, notes: false);
        Guid patientId = await CreatePatientAsync(account);
        SessionResponse session = await CreateSessionAsync(account, patientId, Tomorrow, "10:00");

        HttpResponseMessage response = await SuggestSessionNotesAsync(account, session.Id, "rascunho");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await CodeOf(response)).ShouldBe(AiErrors.DataNotShared.Code);
    }

    [Fact]
    public async Task PatientAnalysis_SendsOnlyTheDataTheClinicAllowed()
    {
        (TestAccount account, Guid patientId) = await PracticeWithCompletedSessionAsync("Relata ansiedade no trabalho.", score: 7, comment: "Gostei da conversa");
        await CreateMedicalRecordAsync(account, patientId, "Anamnese", "Histórico familiar de depressão.");
        await EnableAiAsync(account, notes: false, feedbacks: true, records: false);

        HttpResponseMessage response = await SuggestForPatientAsync(account, "patient-analysis", patientId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        string prompt = Ai.LastPrompt.User;
        prompt.ShouldContain("Feedback do paciente: 7/10 — \"Gostei da conversa\"");
        prompt.ShouldContain("análise do acompanhamento");
        prompt.ShouldNotContain("ansiedade no trabalho");
        prompt.ShouldNotContain("Histórico familiar");
    }

    [Fact]
    public async Task NextSteps_UsesOnlyTheRecordsOfTheLoggedPsychologist()
    {
        (TestAccount owner, Guid patientId) = await PracticeWithCompletedSessionAsync("Trabalhamos respiração diafragmática.");
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await CreateMedicalRecordAsync(owner, patientId, "Evolução", "Registro do titular.");
        await CreateMedicalRecordAsync(colleague, patientId, "Avaliação", "Registro do colega.");
        await EnableAiAsync(owner);

        HttpResponseMessage response = await SuggestForPatientAsync(owner, "next-steps", patientId);

        response.StatusCode.ShouldBe(HttpStatusCode.OK, await response.Content.ReadAsStringAsync(Ct));
        string prompt = Ai.LastPrompt.User;
        prompt.ShouldContain("respiração diafragmática");
        prompt.ShouldContain("Registro do titular.");
        prompt.ShouldContain("próximos passos");
        prompt.ShouldNotContain("Registro do colega.");
    }

    [Fact]
    public async Task PatientAnalysis_WithoutClinicalData_Returns422()
    {
        TestAccount account = await CreatePracticeAsync();
        await EnableAiAsync(account);
        Guid patientId = await CreatePatientAsync(account);

        HttpResponseMessage response = await SuggestForPatientAsync(account, "patient-analysis", patientId);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(response)).ShouldBe(AiErrors.InsufficientData.Code);
        Ai.Prompts.ShouldBeEmpty();
    }

    [Fact]
    public async Task PatientAnalysis_PatientFromAnotherCompany_Returns404()
    {
        TestAccount account = await CreatePracticeAsync();
        await EnableAiAsync(account);
        TestAccount other = await CreatePracticeAsync();
        Guid foreignPatient = await CreatePatientAsync(other);

        HttpResponseMessage response = await SuggestForPatientAsync(account, "next-steps", foreignPatient);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Suggestions_ProviderFailure_Returns503_AndRefusal_Returns422_WithoutUsageLog()
    {
        (TestAccount account, Guid patientId) = await PracticeWithCompletedSessionAsync("Relata insônia.");
        await EnableAiAsync(account);

        Ai.Fail = true;
        HttpResponseMessage failed = await SuggestForPatientAsync(account, "patient-analysis", patientId);
        Ai.Fail = false;
        Ai.Refuse = true;
        HttpResponseMessage refused = await SuggestForPatientAsync(account, "patient-analysis", patientId);

        failed.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await CodeOf(failed)).ShouldBe(AiErrors.ProviderFailed.Code);
        refused.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await CodeOf(refused)).ShouldBe(AiErrors.Refused.Code);
        (await CountUsageLogsAsync()).ShouldBe(0);
    }
}
