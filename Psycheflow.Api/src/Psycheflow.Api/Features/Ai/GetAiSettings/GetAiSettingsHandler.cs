using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Ai.Providers;

namespace Psycheflow.Api.Features.Ai.GetAiSettings;

public sealed class GetAiSettingsHandler(AppDbContext db, AiProviderCatalog providers)
{
    public async Task<AiSettingsResponse> Handle(CancellationToken cancellationToken)
    {
        AiSettings settings = await db.AiSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken) ?? AiSettings.CreateDisabled();
        return AiSettingsResponse.From(settings, providers.Available);
    }
}
