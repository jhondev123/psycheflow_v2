using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Persistence;

namespace Psycheflow.Api.Features.Companies.GetSettings;

public sealed class GetSettingsHandler(AppDbContext db, ICurrentUser currentUser)
{
    public async Task<SettingsResponse> Handle(CancellationToken cancellationToken) =>
        SettingsResponse.From(await db.GetCurrentSettingsAsync(currentUser, cancellationToken));
}
