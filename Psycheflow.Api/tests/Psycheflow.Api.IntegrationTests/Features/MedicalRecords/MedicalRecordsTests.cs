using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.MedicalRecords;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.MedicalRecords;

public sealed class MedicalRecordsTests(ApiFactory factory) : SchedulingTest(factory)
{
    private static readonly byte[] SamplePdf = [.. "%PDF-1.7\n"u8.ToArray(), .. new byte[2048], .. "\n%%EOF"u8.ToArray()];
    private static readonly byte[] SamplePng = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, .. new byte[512]];

    private async Task<MedicalRecordResponse> CreateRecordAsync(TestAccount account, Guid patientId, string title = "Anamnese", string content = "Queixa principal: ansiedade.")
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/medical-records", new { patientId, title, content }, Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created, await response.Content.ReadAsStringAsync(Ct));
        return await ReadAsync<MedicalRecordResponse>(response);
    }

    private Task<HttpResponseMessage> UploadAsync(TestAccount account, Guid recordId, byte[] bytes, string fileName, string contentType)
    {
        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", fileName);
        return CreateClient(account).PostAsync($"/api/v1/medical-records/{recordId}/attachments", form, Ct);
    }

    [Fact]
    public async Task Create_ValidRecord_Returns201WithAuthor()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account, "Paciente Prontuário");

        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);

        record.PatientName.ShouldBe("Paciente Prontuário");
        record.PsychologistId.ShouldBe(account.PsychologistId!.Value);
        record.Content.ShouldBe("Queixa principal: ansiedade.");
        record.Attachments.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_MissingFields_Returns422_AndForeignPatient_Returns404()
    {
        TestAccount account = await CreatePracticeAsync();
        TestAccount other = await CreatePracticeAsync();
        Guid foreignPatient = await CreatePatientAsync(other);
        HttpClient client = CreateClient(account);

        HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/v1/medical-records", new { title = "" }, Ct);
        HttpResponseMessage foreign = await client.PostAsJsonAsync("/api/v1/medical-records", new { patientId = foreignPatient, title = "x", content = "y" }, Ct);

        invalid.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(invalid)).Errors.Keys.ShouldBe(["patientId", "title", "content"], ignoreOrder: true);
        foreign.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OnlyTheAuthorPsychologist_CanReadOrListTheRecord()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        TestAccount manager = await CreateStaffAsync(owner, Roles.Manager);
        Guid patientId = await CreatePatientAsync(owner);
        MedicalRecordResponse record = await CreateRecordAsync(owner, patientId);

        HttpResponseMessage asColleague = await CreateClient(colleague).GetAsync($"/api/v1/medical-records/{record.Id}", Ct);
        HttpResponseMessage managerCreates = await CreateClient(manager).PostAsJsonAsync("/api/v1/medical-records", new { patientId, title = "x", content = "y" }, Ct);
        PagedResponse<MedicalRecordListItem> colleagueList = (await CreateClient(colleague).GetFromJsonAsync<PagedResponse<MedicalRecordListItem>>(
            $"/api/v1/medical-records?patientId={patientId}", Json, Ct))!;

        asColleague.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        managerCreates.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        colleagueList.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Search_ByPatientPeriodAndKeyword()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientA = await CreatePatientAsync(account);
        Guid patientB = await CreatePatientAsync(account);
        await CreateRecordAsync(account, patientA, "Anamnese", "Relata insônia e ansiedade.");
        Factory.Clock.Advance(TimeSpan.FromMinutes(30));
        await CreateRecordAsync(account, patientA, "Evolução", "Melhora do sono.");
        await CreateRecordAsync(account, patientB, "Anamnese", "Luto recente.");
        HttpClient client = CreateClient(account);

        PagedResponse<MedicalRecordListItem> byPatient = (await client.GetFromJsonAsync<PagedResponse<MedicalRecordListItem>>(
            $"/api/v1/medical-records?patientId={patientA}", Json, Ct))!;
        PagedResponse<MedicalRecordListItem> byKeyword = (await client.GetFromJsonAsync<PagedResponse<MedicalRecordListItem>>(
            "/api/v1/medical-records?search=Insônia", Json, Ct))!;
        PagedResponse<MedicalRecordListItem> byPeriod = (await client.GetFromJsonAsync<PagedResponse<MedicalRecordListItem>>(
            "/api/v1/medical-records?from=2026-10-06", Json, Ct))!;

        byPatient.Items.Select(r => r.Title).ShouldBe(["Evolução", "Anamnese"]);
        byKeyword.Items.Single().PatientId.ShouldBe(patientA);
        byPeriod.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Update_ChangesTitleAndContent()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/medical-records/{record.Id}", new
        {
            title = "Anamnese revisada",
            content = "## Queixa\nAnsiedade social.",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MedicalRecordResponse updated = await ReadAsync<MedicalRecordResponse>(response);
        updated.Title.ShouldBe("Anamnese revisada");
        updated.Content.ShouldBe("## Queixa\nAnsiedade social.");
    }

    [Fact]
    public async Task Attachments_UploadDownloadAndDelete()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);

        HttpResponseMessage uploaded = await UploadAsync(account, record.Id, SamplePdf, "exame.pdf", "application/pdf");
        uploaded.StatusCode.ShouldBe(HttpStatusCode.Created, await uploaded.Content.ReadAsStringAsync(Ct));
        AttachmentResponse attachment = await ReadAsync<AttachmentResponse>(uploaded);
        attachment.FileName.ShouldBe("exame.pdf");
        attachment.SizeBytes.ShouldBe(SamplePdf.Length);

        HttpClient client = CreateClient(account);
        string url = $"/api/v1/medical-records/{record.Id}/attachments/{attachment.Id}";
        HttpResponseMessage download = await client.GetAsync(url, Ct);
        download.StatusCode.ShouldBe(HttpStatusCode.OK);
        download.Content.Headers.ContentType?.MediaType.ShouldBe("application/pdf");
        (await download.Content.ReadAsByteArrayAsync(Ct)).ShouldBe(SamplePdf);

        MedicalRecordResponse withAttachment = (await client.GetFromJsonAsync<MedicalRecordResponse>($"/api/v1/medical-records/{record.Id}", Json, Ct))!;
        withAttachment.Attachments.Single().Id.ShouldBe(attachment.Id);

        (await client.DeleteAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.GetAsync(url, Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Attachments_PngIsAccepted()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);

        HttpResponseMessage response = await UploadAsync(account, record.Id, SamplePng, "desenho.png", "image/png");

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("script.exe", "application/octet-stream", false)]
    [InlineData("falso.pdf", "application/pdf", false)]
    [InlineData("grande.pdf", "application/pdf", true)]
    public async Task Attachments_InvalidFiles_Return422(string fileName, string contentType, bool tooLarge)
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);
        byte[] bytes = tooLarge
            ? [.. "%PDF-1.7"u8.ToArray(), .. new byte[(10 * 1024 * 1024) + 1]]
            : "MZ-nao-e-pdf"u8.ToArray();

        HttpResponseMessage response = await UploadAsync(account, record.Id, bytes, fileName, contentType);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey("file");
    }

    [Fact]
    public async Task Delete_HidesTheRecord()
    {
        TestAccount account = await CreatePracticeAsync();
        Guid patientId = await CreatePatientAsync(account);
        MedicalRecordResponse record = await CreateRecordAsync(account, patientId);
        HttpClient client = CreateClient(account);

        (await client.DeleteAsync($"/api/v1/medical-records/{record.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await client.GetAsync($"/api/v1/medical-records/{record.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
