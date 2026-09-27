using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Companies;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Users;

namespace Psycheflow.Api.Features.Auth.Register;

/// <summary>
/// UC25: cria empresa (com configurações padrão), usuário responsável (Admin + Psychologist) e perfil de psicólogo
/// numa única transação — qualquer falha desfaz tudo.
/// </summary>
public sealed class RegisterHandler(AppDbContext db, UserManager<User> userManager, AccessTokenIssuer tokenIssuer)
{
    public async Task<Result<AuthResponse>> Handle(RegisterRequest request, CancellationToken cancellationToken)
    {
        Result<Company> company = Company.Create(request.CompanyName);
        if (company.IsFailure)
        {
            return company.Error;
        }

        Result<LicenseNumber> licenseNumber = LicenseNumber.Create(request.LicenseNumber);
        if (licenseNumber.IsFailure)
        {
            return licenseNumber.Error;
        }

        Result<Phone>? phone = request.Phone is null ? null : Phone.Create(request.Phone);
        if (phone is { IsFailure: true })
        {
            return phone.Error;
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return AuthErrors.EmailInUse;
        }

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        db.Companies.Add(company.Value);
        await db.SaveChangesAsync(cancellationToken);

        var user = User.Create(request.FullName, request.Email, company.Value.Id, mustChangePassword: false);
        IdentityResult created = await userManager.CreateAsync(user, request.Password);
        if (!created.Succeeded)
        {
            return AuthErrors.FromIdentity(created);
        }

        IdentityResult rolesAdded = await userManager.AddToRolesAsync(user, [Roles.Admin, Roles.Psychologist]);
        if (!rolesAdded.Succeeded)
        {
            return AuthErrors.FromIdentity(rolesAdded);
        }

        db.Psychologists.Add(Psychologist.Create(user.Id, company.Value.Id, licenseNumber.Value, request.Approach, phone?.Value));
        await db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await tokenIssuer.IssueAsync(user, cancellationToken);
    }
}
