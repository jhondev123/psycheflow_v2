using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.MedicalRecords;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.MedicalRecords;

/// <summary>DT-24 / RD002: leituras, downloads e tentativas negadas ficam registrados e o autor consulta o histórico.</summary>
public sealed class MedicalRecordAccessLogTests(ApiFactory factory) : SchedulingTest(factory)
{
    private async Task<(MedicalRecordResponse Record, Guid AttachmentId)> RecordWithAttachmentAsync(TestAccount owner)
    {
        Guid patientId = await CreatePatientAsync(owner);
        HttpResponseMessage created = await CreateClient(owner).PostAsJsonAsync(
            "/api/v1/medical-records", new { patientId, title = "Anamnese", content = "Queixa principal." }, Ct);
        MedicalRecordResponse record = await ReadAsync<MedicalRecordResponse>(created);

        var form = new MultipartFormDataContent();
        var file = new ByteArrayContent([.. "%PDF-1.7\n"u8.ToArray(), .. new byte[256], .. "\n%%EOF"u8.ToArray()]);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(file, "file", "exame.pdf");
        HttpResponseMessage uploaded = await CreateClient(owner).PostAsync($"/api/v1/medical-records/{record.Id}/attachments", form, Ct);
        AttachmentResponse attachment = await ReadAsync<AttachmentResponse>(uploaded);

        return (record, attachment.Id);
    }

    [Fact]
    public async Task ReadsDownloadsAndDeniedAttempts_AreLogged_AndOnlyTheAuthorSeesTheLog()
    {
        TestAccount owner = await CreatePracticeAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        (MedicalRecordResponse record, Guid attachmentId) = await RecordWithAttachmentAsync(owner);

        (await CreateClient(owner).GetAsync($"/api/v1/medical-records/{record.Id}", Ct)).EnsureSuccessStatusCode();
        (await CreateClient(owner).GetAsync($"/api/v1/medical-records/{record.Id}/attachments/{attachmentId}", Ct)).EnsureSuccessStatusCode();
        (await CreateClient(colleague).GetAsync($"/api/v1/medical-records/{record.Id}", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        HttpResponseMessage colleagueReadsLog = await CreateClient(colleague).GetAsync($"/api/v1/medical-records/{record.Id}/access-log", Ct);

        HttpResponseMessage response = await CreateClient(owner).GetAsync($"/api/v1/medical-records/{record.Id}/access-log", Ct);

        colleagueReadsLog.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MedicalRecordAccessEntry> log = await ReadAsync<List<MedicalRecordAccessEntry>>(response);
        log.Select(e => (e.UserId, e.Action)).ShouldBe(
        [
            (owner.UserId, MedicalRecordAccessAction.Viewed),
            (owner.UserId, MedicalRecordAccessAction.AttachmentDownloaded),
            (colleague.UserId, MedicalRecordAccessAction.Denied),
            (colleague.UserId, MedicalRecordAccessAction.Denied),
        ], ignoreOrder: true);
        log.Single(e => e.Action == MedicalRecordAccessAction.AttachmentDownloaded).AttachmentId.ShouldBe(attachmentId);
        log.ShouldAllBe(e => e.At == ApiFactory.DefaultNow);
        log.ShouldAllBe(e => !string.IsNullOrWhiteSpace(e.UserName));
    }
}
