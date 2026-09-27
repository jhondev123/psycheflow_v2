using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Psychologists.UpdateProfile;

/// <summary>UC28 / RC-04: nome, CRP, abordagem e telefone. O próprio psicólogo ou Admin/Manager.</summary>
public sealed class UpdateProfileHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<PsychologistResponse>> Handle(Guid id, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        Psychologist? psychologist = await db.FindPsychologistAsync(id, cancellationToken);
        if (psychologist is null)
        {
            return PsychologistErrors.NotFound;
        }

        if (!currentUser.CanManagePsychologist(psychologist.Id))
        {
            return PsychologistErrors.CannotManage;
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

        psychologist.UpdateProfile(licenseNumber.Value, request.Approach, phone?.Value);
        psychologist.User!.Rename(request.FullName);
        await db.SaveChangesAsync(cancellationToken);

        return PsychologistResponse.From(psychologist);
    }
}
