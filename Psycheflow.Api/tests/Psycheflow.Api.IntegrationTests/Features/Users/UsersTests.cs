using System.Net;
using System.Net.Http.Json;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Users;
using Psycheflow.Api.Features.Users.CreateUser;
using Psycheflow.Api.IntegrationTests.Infrastructure;

namespace Psycheflow.Api.IntegrationTests.Features.Users;

public sealed class UsersTests(ApiFactory factory) : IntegrationTest(factory)
{
    [Fact]
    public async Task CreateUser_AsAdmin_ReturnsTemporaryPasswordThatForcesChange()
    {
        TestAccount admin = await RegisterAccountAsync();
        string email = TestAccounts.UniqueEmail();

        HttpResponseMessage response = await CreateClient(admin).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = "Carla Mendes",
            email,
            role = Roles.Psychologist,
            licenseNumber = "06/54321",
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        CreateUserResponse created = await ReadAsync<CreateUserResponse>(response);
        created.PsychologistId.ShouldNotBeNull();
        created.TemporaryPassword.Length.ShouldBeGreaterThanOrEqualTo(12);

        AuthResponse firstLogin = await LoginAsync(email, created.TemporaryPassword);
        firstLogin.MustChangePassword.ShouldBeTrue();

        // Enquanto não troca a senha, só consegue ver o próprio perfil e trocar a senha.
        HttpClient pending = CreateClient(firstLogin.AccessToken);
        (await pending.GetAsync("/api/v1/settings", Ct)).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await pending.GetAsync("/api/v1/auth/me", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_AfterPasswordChange_GetsFullAccess()
    {
        TestAccount admin = await RegisterAccountAsync();

        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        psychologist.CompanyId.ShouldBe(admin.CompanyId);
        psychologist.PsychologistId.ShouldNotBeNull();
        (await CreateClient(psychologist).GetAsync("/api/v1/settings", Ct)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_ManagerCreatingAdmin_Returns403()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount manager = await CreateStaffAsync(admin, Roles.Manager);

        HttpResponseMessage response = await CreateClient(manager).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = "Novo Admin",
            email = TestAccounts.UniqueEmail(),
            role = Roles.Admin,
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateUser_AsPsychologist_Returns403()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount psychologist = await CreateStaffAsync(admin, Roles.Psychologist);

        HttpResponseMessage response = await CreateClient(psychologist).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = "Alguém",
            email = TestAccounts.UniqueEmail(),
            role = Roles.Manager,
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("Psychologist", null, "licenseNumber")]
    [InlineData("Patient", null, "role")]
    [InlineData("SuperUser", null, "role")]
    public async Task CreateUser_InvalidRoleOrMissingLicense_Returns422(string role, string? licenseNumber, string field)
    {
        TestAccount admin = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(admin).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = "Fulano",
            email = TestAccounts.UniqueEmail(),
            role,
            licenseNumber,
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await ReadProblemAsync(response)).Errors.ShouldContainKey(field);
    }

    [Fact]
    public async Task CreateUser_EmailAlreadyInUse_Returns409()
    {
        TestAccount admin = await RegisterAccountAsync();
        TestAccount other = await RegisterAccountAsync();

        HttpResponseMessage response = await CreateClient(admin).PostAsJsonAsync("/api/v1/users", new
        {
            fullName = "Duplicado",
            email = other.Email,
            role = Roles.Manager,
        }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ListUsers_ReturnsOnlyUsersOfTheSameCompany()
    {
        TestAccount companyA = await RegisterAccountAsync();
        TestAccount managerA = await CreateStaffAsync(companyA, Roles.Manager);
        await RegisterAccountAsync();

        List<UserResponse> users = (await CreateClient(companyA).GetFromJsonAsync<List<UserResponse>>("/api/v1/users", Json, Ct))!;

        users.Select(u => u.Id).ShouldBe([companyA.UserId, managerA.UserId], ignoreOrder: true);
        users.Single(u => u.Id == managerA.UserId).Roles.ShouldBe([Roles.Manager]);
    }
}
