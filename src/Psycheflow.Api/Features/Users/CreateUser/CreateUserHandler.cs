using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Storage;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Auth;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Users.CreateUser;

/// <summary>UC26: Admin/Manager cadastram usuários na própria empresa com senha temporária (troca obrigatória).</summary>
public sealed class CreateUserHandler(AppDbContext db, UserManager<User> userManager, ICurrentUser currentUser)
{
    public async Task<Result<CreateUserResponse>> Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (request.Role == Roles.Admin && !currentUser.IsInRole(Roles.Admin))
        {
            return UserErrors.OnlyAdminCanCreateAdmin;
        }

        LicenseNumber? licenseNumber = null;
        if (request.Role == Roles.Psychologist)
        {
            Result<LicenseNumber> license = LicenseNumber.Create(request.LicenseNumber);
            if (license.IsFailure)
            {
                return license.Error;
            }

            licenseNumber = license.Value;
        }

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            return AuthErrors.EmailInUse;
        }

        string temporaryPassword = TemporaryPassword.Generate();
        var user = User.Create(request.FullName, request.Email, currentUser.CompanyId, mustChangePassword: true);

        await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        IdentityResult created = await userManager.CreateAsync(user, temporaryPassword);
        if (!created.Succeeded)
        {
            return AuthErrors.FromIdentity(created);
        }

        IdentityResult roleAdded = await userManager.AddToRoleAsync(user, request.Role);
        if (!roleAdded.Succeeded)
        {
            return AuthErrors.FromIdentity(roleAdded);
        }

        Psychologist? psychologist = null;
        if (licenseNumber is not null)
        {
            psychologist = Psychologist.Create(
                user.Id, currentUser.CompanyId, licenseNumber, request.Approach ?? ApproachType.NotInformed, phone: null);
            db.Psychologists.Add(psychologist);
            await db.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return new CreateUserResponse(user.Id, user.FullName, user.Email!, request.Role, psychologist?.Id, temporaryPassword);
    }
}
