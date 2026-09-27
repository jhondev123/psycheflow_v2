using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Ai;

namespace Psycheflow.Api.UnitTests.Features.Ai;

public sealed class AiSettingsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AdminId = Guid.CreateVersion7();

    [Fact]
    public void CreateDisabled_StartsWithoutConsentOrSharedData()
    {
        AiSettings settings = AiSettings.CreateDisabled();

        settings.IsEnabled.ShouldBeFalse();
        settings.Sharing.ShouldBe(AiDataSharing.None);
        settings.ConsentAcceptedAt.ShouldBeNull();
        settings.ConsentAcceptedByUserId.ShouldBeNull();
    }

    [Fact]
    public void Enable_WithoutAnySharedData_Fails()
    {
        AiSettings settings = AiSettings.CreateDisabled();

        Result result = settings.Enable(AiProvider.Claude, AiDataSharing.None, AdminId, Now);

        result.Error.ShouldBe(AiErrors.NoDataShared);
        settings.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Enable_RecordsProviderSharingAndConsent()
    {
        AiSettings settings = AiSettings.CreateDisabled();
        var sharing = new AiDataSharing(SessionNotes: true, Feedbacks: false, MedicalRecords: true);

        Result result = settings.Enable(AiProvider.Gemini, sharing, AdminId, Now);

        result.IsSuccess.ShouldBeTrue();
        settings.IsEnabled.ShouldBeTrue();
        settings.Provider.ShouldBe(AiProvider.Gemini);
        settings.Sharing.ShouldBe(sharing);
        settings.ConsentAcceptedAt.ShouldBe(Now);
        settings.ConsentAcceptedByUserId.ShouldBe(AdminId);
    }

    [Fact]
    public void Disable_ClearsConsentAndKeepsChoices()
    {
        AiSettings settings = AiSettings.CreateDisabled();
        var sharing = new AiDataSharing(SessionNotes: true, Feedbacks: true, MedicalRecords: false);
        settings.Enable(AiProvider.OpenAi, sharing, AdminId, Now);

        settings.Disable(AiProvider.OpenAi, sharing);

        settings.IsEnabled.ShouldBeFalse();
        settings.Provider.ShouldBe(AiProvider.OpenAi);
        settings.Sharing.ShouldBe(sharing);
        settings.ConsentAcceptedAt.ShouldBeNull();
        settings.ConsentAcceptedByUserId.ShouldBeNull();
    }
}
