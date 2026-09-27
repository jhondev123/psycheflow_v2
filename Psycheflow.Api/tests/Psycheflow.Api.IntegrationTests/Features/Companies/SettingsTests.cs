using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Companies;

public sealed class SettingsTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task GetSettings_NewCompany_ReturnsDefaults()
    {
        TestAccount account = await RegisterAccountAsync();

        SettingsResponse settings = (await CreateClient(account).GetFromJsonAsync<SettingsResponse>("/api/v1/settings", Json, Ct))!;

        settings.ShouldBe(new SettingsResponse(50, null, "America/Sao_Paulo"));
    }

    [Fact]
    public async Task UpdateSettings_AsAdmin_PersistsOnlyForOwnCompany()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount companyB = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(companyA).PutAsJsonAsync("/api/v1/settings", new
        {
            sessionDurationMinutes = 60,
            sessionDefaultPrice = 180.50m,
            timeZone = "America/Manaus",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await CreateClient(companyA).GetFromJsonAsync<SettingsResponse>("/api/v1/settings", Json, Ct))
            .ShouldBe(new SettingsResponse(60, 180.50m, "America/Manaus"));
        (await CreateClient(companyB).GetFromJsonAsync<SettingsResponse>("/api/v1/settings", Json, Ct))!
            .SessionDurationMinutes.ShouldBe(50);
    }

    [Theory]
    [InlineData(10, "sessionDurationMinutes")]
    [InlineData(300, "sessionDurationMinutes")]
    public async Task UpdateSettings_InvalidDuration_Returns422(int minutes, string field)
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync("/api/v1/settings", new
        {
            sessionDurationMinutes = minutes,
            timeZone = "America/Sao_Paulo",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }

    [Fact]
    public async Task UpdateSettings_AsPsychologistOnly_Returns403()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        HttpResponseMessage response = await CreateClient(psychologist).PutAsJsonAsync("/api/v1/settings", new
        {
            sessionDurationMinutes = 45,
            timeZone = "America/Sao_Paulo",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
