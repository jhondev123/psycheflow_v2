using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Common.Persistence;

/// <summary>
/// Dados de demonstração para desenvolvimento local (executado pelo EF via <c>UseAsyncSeeding</c> ao migrar,
/// somente no ambiente Development). É idempotente: se a empresa demo já existe, não faz nada.
/// </summary>
/// <remarks>
/// Logins de demonstração (senha de todos: <see cref="DemoPassword"/>):
/// admin@psycheflow.dev (Admin), ana@psycheflow.dev (Admin + Psicóloga), bruno@psycheflow.dev (Psicólogo),
/// gestao@psycheflow.dev (Manager).
/// </remarks>
internal static class DevData
{
    public const string DemoPassword = "Psycheflow@123";

    public static async Task SeedAsync(DbContext context, bool storeManagementPerformed, CancellationToken cancellationToken)
    {
        var db = (AppDbContext)context;

        if (await db.Users.AnyAsync(u => u.Email == "admin@psycheflow.dev", cancellationToken))
        {
            return;
        }

        Company company = Company.Create("Clínica Psycheflow (demo)").Value;
        db.Companies.Add(company);

        var hasher = new PasswordHasher<User>();
        AddUser(db, hasher, company.Id, "Administrador Demo", "admin@psycheflow.dev", Roles.Admin);
        User ana = AddUser(db, hasher, company.Id, "Ana Souza", "ana@psycheflow.dev", Roles.Admin, Roles.Psychologist);
        User bruno = AddUser(db, hasher, company.Id, "Bruno Lima", "bruno@psycheflow.dev", Roles.Psychologist);
        AddUser(db, hasher, company.Id, "Gestão Demo", "gestao@psycheflow.dev", Roles.Manager);

        var anaProfile = Psychologist.Create(
            ana.Id, company.Id, LicenseNumber.Create("06/123456").Value, ApproachType.CognitiveBehavioral, Phone.Create("(45) 99911-2233").Value);
        var brunoProfile = Psychologist.Create(
            bruno.Id, company.Id, LicenseNumber.Create("06/654321").Value, ApproachType.Psychoanalysis, Phone.Create("(45) 99944-5566").Value);
        anaProfile.SetWorkingHours(WeekdayHours(new TimeOnly(8, 0), new TimeOnly(12, 0), new TimeOnly(13, 0), new TimeOnly(18, 0)));
        brunoProfile.SetWorkingHours(WeekdayHours(new TimeOnly(9, 0), new TimeOnly(13, 0), new TimeOnly(14, 0), new TimeOnly(19, 0)));
        db.Psychologists.AddRange(anaProfile, brunoProfile);

        // O seed roda sem usuário logado: a empresa dos dados de tenant é definida explicitamente.
        foreach (Patient patient in DemoPatients())
        {
            db.Patients.Add(patient);
            db.Entry(patient).Property(p => p.CompanyId).CurrentValue = company.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Patient> DemoPatients()
    {
        (string Name, string CpfBase, string Phone, int BirthYear)[] people =
        [
            ("Mariana Alves", "123456789", "(45) 99101-0001", 1992),
            ("Carlos Eduardo Santos", "234567891", "(45) 99101-0002", 1985),
            ("Juliana Ferreira", "345678912", "(45) 99101-0003", 2001),
            ("Rafael Oliveira", "456789123", "(45) 99101-0004", 1978),
            ("Beatriz Costa", "567891234", "(45) 99101-0005", 1996),
            ("Lucas Pereira", "678912345", "(45) 99101-0006", 2004),
            ("Fernanda Rodrigues", "789123456", "(45) 99101-0007", 1989),
            ("Gustavo Martins", "891234567", "(45) 99101-0008", 1973),
        ];

        foreach ((string name, string cpfBase, string phone, int birthYear) in people)
        {
            string email = $"{name.Split(' ')[0].ToLowerInvariant()}@email.com";
            Address address = Address.From(new AddressDto("85810-000", "Rua Paraná", "1500", null, "Centro", "Cascavel", "PR"))!;
            yield return Patient.Create(
                name, Cpf.Create(CompleteCpf(cpfBase)).Value, email, Phone.Create(phone).Value, new DateOnly(birthYear, 3, 15), address, notes: null);
        }
    }

    /// <summary>Calcula os dois dígitos verificadores a partir dos 9 primeiros dígitos.</summary>
    private static string CompleteCpf(string nineDigits)
    {
        string digits = nineDigits;
        for (int length = 9; length <= 10; length++)
        {
            int sum = 0;
            for (int i = 0; i < length; i++)
            {
                sum += (digits[i] - '0') * (length + 1 - i);
            }

            int remainder = sum % 11;
            digits += remainder < 2 ? 0 : 11 - remainder;
        }

        return digits;
    }

    /// <summary>Segunda a sexta, manhã e tarde.</summary>
    private static IEnumerable<WorkingHoursRange> WeekdayHours(TimeOnly morningStart, TimeOnly morningEnd, TimeOnly afternoonStart, TimeOnly afternoonEnd) =>
        Enumerable.Range((int)DayOfWeek.Monday, 5).SelectMany(day => new[]
        {
            new WorkingHoursRange((DayOfWeek)day, morningStart, morningEnd),
            new WorkingHoursRange((DayOfWeek)day, afternoonStart, afternoonEnd),
        });

    private static User AddUser(AppDbContext db, PasswordHasher<User> hasher, Guid companyId, string name, string email, params string[] roles)
    {
        var user = User.Create(name, email, companyId, mustChangePassword: false);
        user.NormalizedEmail = user.Email!.ToUpperInvariant();
        user.NormalizedUserName = user.UserName!.ToUpperInvariant();
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.EmailConfirmed = true;
        user.PasswordHash = hasher.HashPassword(user, DemoPassword);
        db.Users.Add(user);

        foreach (string role in roles)
        {
            db.UserRoles.Add(new IdentityUserRole<Guid> { UserId = user.Id, RoleId = IdentityModel.RoleIds[role] });
        }

        return user;
    }
}
