using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Ai.Providers;

namespace Psycheflow.Api.Features.Ai.UpdateAiSettings;

public sealed class UpdateAiSettingsHandler(AppDbContext db, ICurrentUser currentUser, AiProviderCatalog providers, TimeProvider timeProvider)
{
    public async Task<Result<AiSettingsResponse>> Handle(UpdateAiSettingsRequest request, CancellationToken cancellationToken)
    {
        AiSettings? settings = await db.AiSettings.SingleOrDefaultAsync(cancellationToken);
        if (settings is null)
        {
            settings = AiSettings.CreateDisabled();
            db.AiSettings.Add(settings);
        }

        var sharing = new AiDataSharing(request.ShareSessionNotes, request.ShareFeedbacks, request.ShareMedicalRecords);
        if (request.IsEnabled)
        {
            if (!providers.IsAvailable(request.Provider))
            {
                return AiErrors.ProviderNotConfigured;
            }

            Result enabled = settings.Enable(request.Provider, sharing, currentUser.UserId, timeProvider.GetUtcNow());
            if (enabled.IsFailure)
            {
                return enabled.Error;
            }
        }
        else
        {
            settings.Disable(request.Provider, sharing);
        }

        await db.SaveChangesAsync(cancellationToken);
        return AiSettingsResponse.From(settings, providers.Available);
    }
}
