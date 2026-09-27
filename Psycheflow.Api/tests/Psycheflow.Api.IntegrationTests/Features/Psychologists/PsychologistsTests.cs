using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Psychologists;

public sealed class PsychologistsTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task List_ReturnsOnlyPsychologistsOfTheCompany()
    {
        TestAccount owner = await RegisterAccountAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        await CreateStaffAsync(owner, Roles.Manager);
        await RegisterAccountAsync();

        List<PsychologistResponse> list = (await CreateClient(owner).GetFromJsonAsync<List<PsychologistResponse>>("/api/v1/psychologists", Json, Ct))!;

        list.Select(p => p.Id).ShouldBe([owner.PsychologistId!.Value, colleague.PsychologistId!.Value], ignoreOrder: true);
    }

    [Fact]
    public async Task Get_PsychologistOfAnotherCompany_Returns404()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount companyB = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(companyA).GetAsync($"/api/v1/psychologists/{companyB.PsychologistId}", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMe_ReturnsOwnProfile()
    {
        TestAccount account = await RegisterAccountAsync();

        PsychologistResponse me = (await CreateClient(account).GetFromJsonAsync<PsychologistResponse>("/api/v1/psychologists/me", Json, Ct))!;

        me.Id.ShouldBe(account.PsychologistId!.Value);
        me.UserId.ShouldBe(account.UserId);
        me.Email.ShouldBe(account.Email);
        me.Approach.ShouldBe(ApproachType.CognitiveBehavioral);
        me.WorkingHours.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMe_UserWithoutProfile_Returns404()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount manager = await CreateStaffAsync(admin, Roles.Manager);

        HttpResponseMessage response = await CreateClient(manager).GetAsync("/api/v1/psychologists/me", Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProfile_OwnProfile_UpdatesNameAndProfessionalData()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/psychologists/{account.PsychologistId}", new
        {
            fullName = "Dra. Ana Paula",
            licenseNumber = "08/998877",
            approach = "Humanistic",
            phone = "(41) 3333-4444",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PsychologistResponse updated = await ReadAsync<PsychologistResponse>(response);
        updated.FullName.ShouldBe("Dra. Ana Paula");
        updated.LicenseNumber.ShouldBe("08/998877");
        updated.Approach.ShouldBe(ApproachType.Humanistic);
        updated.Phone.ShouldBe("4133334444");
    }

    [Fact]
    public async Task UpdateProfile_OfColleague_AsPsychologist_Returns403_AsAdmin_Returns200()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);
        object payload = new { fullName = "Outro Nome", licenseNumber = "06/11111", approach = "Behavioral" };

        HttpResponseMessage asPsychologist = await CreateClient(psychologist).PutAsJsonAsync($"/api/v1/psychologists/{admin.PsychologistId}", payload, Ct);
        HttpResponseMessage asAdmin = await CreateClient(admin).PutAsJsonAsync($"/api/v1/psychologists/{psychologist.PsychologistId}", payload, Ct);

        asPsychologist.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        asAdmin.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateProfile_InvalidCrp_Returns422()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/psychologists/{account.PsychologistId}", new
        {
            fullName = "Ana",
            licenseNumber = "12345",
            approach = "Behavioral",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey("licenseNumber");
    }
}
