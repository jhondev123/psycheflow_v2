using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Psychologists;

public sealed class WorkingHoursTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task SetWorkingHours_ReplacesAllRanges_AndGetReturnsThemOrdered()
    {
        TestAccount account = await RegisterAccountAsync();
        HttpClient client = CreateClient(account);
        string url = $"/api/v1/psychologists/{account.PsychologistId}/working-hours";
        await client.PutAsJsonAsync(url, new { hours = new[] { new { dayOfWeek = "Friday", startTime = "08:00", endTime = "12:00" } } }, Ct);

        HttpResponseMessage response = await client.PutAsJsonAsync(url, new
        {
            hours = new[]
            {
                new { dayOfWeek = "Tuesday", startTime = "13:00", endTime = "18:00" },
                new { dayOfWeek = "Monday", startTime = "13:00", endTime = "18:00" },
                new { dayOfWeek = "Monday", startTime = "08:00", endTime = "12:00" },
            },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<WorkingHoursDto> hours = (await client.GetFromJsonAsync<List<WorkingHoursDto>>(url, Json, Ct))!;
        hours.ShouldBe(
        [
            new WorkingHoursDto(DayOfWeek.Monday, new TimeOnly(8, 0), new TimeOnly(12, 0)),
            new WorkingHoursDto(DayOfWeek.Monday, new TimeOnly(13, 0), new TimeOnly(18, 0)),
            new WorkingHoursDto(DayOfWeek.Tuesday, new TimeOnly(13, 0), new TimeOnly(18, 0)),
        ]);
    }

    [Fact]
    public async Task SetWorkingHours_EmptyList_ClearsSchedule()
    {
        TestAccount account = await RegisterAccountAsync();
        HttpClient client = CreateClient(account);
        string url = $"/api/v1/psychologists/{account.PsychologistId}/working-hours";
        await client.PutAsJsonAsync(url, new { hours = new[] { new { dayOfWeek = "Monday", startTime = "08:00", endTime = "12:00" } } }, Ct);

        HttpResponseMessage response = await client.PutAsJsonAsync(url, new { hours = Array.Empty<object>() }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await client.GetFromJsonAsync<List<WorkingHoursDto>>(url, Json, Ct))!.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("08:00", "12:00", "11:00", "14:00")]
    [InlineData("08:00", "12:00", "08:00", "12:00")]
    public async Task SetWorkingHours_OverlappingRangesOnSameDay_Returns422(string start1, string end1, string start2, string end2)
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/psychologists/{account.PsychologistId}/working-hours", new
        {
            hours = new[]
            {
                new { dayOfWeek = "Monday", startTime = start1, endTime = end1 },
                new { dayOfWeek = "Monday", startTime = start2, endTime = end2 },
            },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task SetWorkingHours_StartNotBeforeEnd_Returns422()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/psychologists/{account.PsychologistId}/working-hours", new
        {
            hours = new[] { new { dayOfWeek = "Monday", startTime = "12:00", endTime = "12:00" } },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey("hours[0].endTime");
    }

    [Fact]
    public async Task SetWorkingHours_OfColleague_AsPsychologist_Returns403()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        HttpResponseMessage response = await CreateClient(psychologist).PutAsJsonAsync($"/api/v1/psychologists/{admin.PsychologistId}/working-hours", new
        {
            hours = new[] { new { dayOfWeek = "Monday", startTime = "08:00", endTime = "12:00" } },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SetWorkingHours_PsychologistOfAnotherCompany_Returns404()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount companyB = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(companyA).PutAsJsonAsync($"/api/v1/psychologists/{companyB.PsychologistId}/working-hours", new
        {
            hours = Array.Empty<object>(),
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
