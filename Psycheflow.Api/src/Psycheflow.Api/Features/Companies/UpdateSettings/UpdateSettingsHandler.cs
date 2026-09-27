using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Companies.UpdateSettings;

public sealed class UpdateSettingsHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<Result<SettingsResponse>> Handle(UpdateSettingsRequest request, CancellationToken cancellationToken)
    {
        Company company = await db.FindCurrentAsync(currentUser, cancellationToken)
            ?? throw new InvalidOperationException("Empresa do usuário logado não encontrada.");

        Result updated = company.Settings.Update(request.SessionDurationMinutes, request.SessionDefaultPrice, request.TimeZone);
        if (updated.IsFailure)
        {
            return updated.Error;
        }

        await db.SaveChangesAsync(cancellationToken);
        return SettingsResponse.From(company.Settings);
    }
}
