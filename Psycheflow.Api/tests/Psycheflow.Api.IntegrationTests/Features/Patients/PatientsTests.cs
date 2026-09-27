using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Endpoints;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Patients;

public sealed class PatientsTests(ApiFactory factory) : IntegrationTest(factory)
{
    private async Task<PatientResponse> CreatePatientAsync(TestAccount account, object? payload = null)
    {
        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/patients", payload ?? TestPatients.Payload(), Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await ReadAsync<PatientResponse>(response);
    }

    [Fact]
    public async Task Create_ValidData_Returns201ActiveWithNormalizedData()
    {
        TestAccount account = await RegisterAccountAsync();
        string cpf = TestPatients.Cpf();
        string maskedCpf = $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}";

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync(
            "/api/v1/patients", TestPatients.Payload(cpf: maskedCpf, fullName: "  Maria Clara  "), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location!.ToString().ShouldStartWith("/api/v1/patients/");
        PatientResponse patient = await ReadAsync<PatientResponse>(response);
        patient.FullName.ShouldBe("Maria Clara");
        patient.Cpf.ShouldBe(cpf);
        patient.Phone.ShouldBe("45988887777");
        patient.Status.ShouldBe(PatientStatus.Active);
        patient.BirthDate.ShouldBe(new DateOnly(1990, 5, 17));
        patient.Address!.ZipCode.ShouldBe("85810000");
        patient.Address.State.ShouldBe("PR");
    }

    [Fact]
    public async Task Create_WithoutAddress_IsAllowed()
    {
        TestAccount account = await RegisterAccountAsync();

        PatientResponse patient = await CreatePatientAsync(account, new
        {
            fullName = "João Pedro",
            cpf = TestPatients.Cpf(),
            email = "joao@email.com",
            phone = "45988887777",
        });

        patient.Address.ShouldBeNull();
        patient.BirthDate.ShouldBeNull();
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns422ForEachField()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/patients", new { }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.Keys.ShouldBe(["fullName", "cpf", "email", "phone"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_InvalidCpfAndIncompleteAddress_Returns422()
    {
        TestAccount account = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(account).PostAsJsonAsync("/api/v1/patients", new
        {
            fullName = "Ana",
            cpf = "111.111.111-11",
            email = "ana@email.com",
            phone = "45988887777",
            address = new { zipCode = "123", street = "", number = "1", neighborhood = "Centro", city = "Cascavel", state = "XX" },
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.Keys.ShouldBe(
            ["cpf", "address.zipCode", "address.street", "address.state"], ignoreOrder: true);
    }

    [Fact]
    public async Task Create_CpfAlreadyRegisteredInCompany_Returns409_ButOtherCompanyCanRegisterIt()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount companyB = await RegisterAccountAsync();
        string cpf = TestPatients.Cpf();
        await CreatePatientAsync(companyA, TestPatients.Payload(cpf: cpf));

        HttpResponseMessage duplicated = await CreateClient(companyA).PostAsJsonAsync("/api/v1/patients", TestPatients.Payload(cpf: cpf), Ct);
        HttpResponseMessage otherCompany = await CreateClient(companyB).PostAsJsonAsync("/api/v1/patients", TestPatients.Payload(cpf: cpf), Ct);

        duplicated.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        otherCompany.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task List_FiltersBySearchAndStatus_WithPagination()
    {
        TestAccount account = await RegisterAccountAsync();
        PatientResponse maria = await CreatePatientAsync(account, TestPatients.Payload(fullName: "Maria Souza"));
        await CreatePatientAsync(account, TestPatients.Payload(fullName: "Mariana Lima"));
        PatientResponse carlos = await CreatePatientAsync(account, TestPatients.Payload(fullName: "Carlos Dias"));
        await CreateClient(account).PutAsJsonAsync($"/api/v1/patients/{carlos.Id}", UpdatePayload(carlos, status: "Inactive"), Ct);
        HttpClient client = CreateClient(account);

        PagedResponse<PatientListItem> byName = (await client.GetFromJsonAsync<PagedResponse<PatientListItem>>("/api/v1/patients?search=mari&pageSize=1", Json, Ct))!;
        PagedResponse<PatientListItem> byCpf = (await client.GetFromJsonAsync<PagedResponse<PatientListItem>>($"/api/v1/patients?search={maria.Cpf}", Json, Ct))!;
        PagedResponse<PatientListItem> inactive = (await client.GetFromJsonAsync<PagedResponse<PatientListItem>>("/api/v1/patients?status=Inactive", Json, Ct))!;

        byName.TotalCount.ShouldBe(2);
        byName.Items.Count.ShouldBe(1);
        byName.Items[0].FullName.ShouldBe("Maria Souza");
        byName.TotalPages.ShouldBe(2);
        byCpf.Items.Single().Id.ShouldBe(maria.Id);
        inactive.Items.Single().Id.ShouldBe(carlos.Id);
    }

    [Fact]
    public async Task List_And_Get_OnlySeePatientsOfOwnCompany()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount companyB = await RegisterAccountAsync();
        PatientResponse patientB = await CreatePatientAsync(companyB);

        PagedResponse<PatientListItem> list = (await CreateClient(companyA).GetFromJsonAsync<PagedResponse<PatientListItem>>("/api/v1/patients", Json, Ct))!;
        HttpResponseMessage get = await CreateClient(companyA).GetAsync($"/api/v1/patients/{patientB.Id}", Ct);

        list.TotalCount.ShouldBe(0);
        get.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_ByColleaguePsychologist_IsAllowed()
    {
        TestAccount owner = await RegisterAccountAsync();
        TestAccount colleague = await CreateStaffAsync(owner, Roles.Psychologist);
        PatientResponse patient = await CreatePatientAsync(owner);

        PatientResponse found = (await CreateClient(colleague).GetFromJsonAsync<PatientResponse>($"/api/v1/patients/{patient.Id}", Json, Ct))!;

        found.Id.ShouldBe(patient.Id);
    }

    [Fact]
    public async Task Update_ChangesEditableFields_KeepsCpf_AndRecordsUpdatedAt()
    {
        TestAccount account = await RegisterAccountAsync();
        PatientResponse patient = await CreatePatientAsync(account);
        Factory.Clock.Advance(TimeSpan.FromHours(1));

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/patients/{patient.Id}", new
        {
            fullName = "Nome Atualizado",
            cpf = TestPatients.Cpf(),
            email = "novo@email.com",
            phone = "(41) 3333-2222",
            birthDate = "1985-01-02",
            status = "Inactive",
            notes = (string?)null,
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        PatientResponse updated = await ReadAsync<PatientResponse>(response);
        updated.FullName.ShouldBe("Nome Atualizado");
        updated.Cpf.ShouldBe(patient.Cpf);
        updated.Email.ShouldBe("novo@email.com");
        updated.Status.ShouldBe(PatientStatus.Inactive);
        updated.Address.ShouldBeNull();
        updated.UpdatedAt.ShouldBe(ApiFactory.DefaultNow.AddHours(1));
    }

    [Fact]
    public async Task Update_InvalidEmail_Returns422()
    {
        TestAccount account = await RegisterAccountAsync();
        PatientResponse patient = await CreatePatientAsync(account);

        HttpResponseMessage response = await CreateClient(account).PutAsJsonAsync($"/api/v1/patients/{patient.Id}", new
        {
            fullName = patient.FullName,
            email = "sem-arroba",
            phone = patient.Phone,
            status = "Active",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey("email");
    }

    private static object UpdatePayload(PatientResponse patient, string status) => new
    {
        fullName = patient.FullName,
        email = patient.Email,
        phone = patient.Phone,
        birthDate = patient.BirthDate,
        status,
        address = patient.Address,
        notes = patient.Notes,
    };
}
