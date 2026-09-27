using Psycheflow.Api.Features.Users;
using Psycheflow.Api.Features.Users.CreateUser;

namespace Psycheflow.Api.UnitTests.Features.Users;

public sealed class TemporaryPasswordTests
{
    [Fact]
    public void Generate_MeetsPasswordPolicy()
    {
        for (int i = 0; i < 200; i++)
        {
            string password = TemporaryPassword.Generate();

            password.Length.ShouldBe(TemporaryPassword.Length);
            password.Any(char.IsUpper).ShouldBeTrue();
            password.Any(char.IsLower).ShouldBeTrue();
            password.Any(char.IsDigit).ShouldBeTrue();
            password.Any(c => !char.IsLetterOrDigit(c)).ShouldBeTrue();
        }
    }

    [Fact]
    public void Generate_ProducesDifferentValues() =>
        Enumerable.Range(0, 50).Select(_ => TemporaryPassword.Generate()).Distinct().Count().ShouldBe(50);

    [Fact]
    public void UserCreate_NormalizesEmailAndTrimsName()
    {
        var user = User.Create("  Ana Souza ", " Ana@Clinica.COM ", Guid.CreateVersion7(), mustChangePassword: true);

        user.Email.ShouldBe("ana@clinica.com");
        user.UserName.ShouldBe("ana@clinica.com");
        user.FullName.ShouldBe("Ana Souza");
        user.MustChangePassword.ShouldBeTrue();
        user.LockoutEnabled.ShouldBeTrue();
    }
}
