using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Psycheflow.Api.Common.Auth;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Ai.Providers;

namespace Psycheflow.Api.Features.Ai.Assistant;

/// <summary>Psicólogo logado autorizado a usar a IA, com o provedor e os dados liberados pela clínica.</summary>
public sealed record AiAccess(Guid PsychologistId, AiProvider Provider, AiDataSharing Sharing);

/// <summary>
/// Pipeline comum às sugestões: autoriza (psicólogo + IA habilitada), pseudonimiza o prompt, chama o provedor da clínica
/// com tempo limite, trata recusa/falha e registra o uso para auditoria.
/// </summary>
public sealed class AiAssistant(
    AppDbContext db,
    ICurrentUser currentUser,
    AiProviderCatalog providers,
    TimeProvider timeProvider,
    IOptions<AiOptions> options,
    ILogger<AiAssistant> logger)
{
    public const string Disclaimer =
        "Sugestão gerada por IA a partir de dados pseudonimizados. Revise antes de usar: a avaliação e a responsabilidade técnica são do(a) psicólogo(a).";

    public async Task<Result<AiAccess>> AuthorizeAsync(CancellationToken cancellationToken)
    {
        if (currentUser.PsychologistId is not { } psychologistId)
        {
            return AiErrors.OnlyPsychologists;
        }

        AiSettings? settings = await db.AiSettings.AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        return settings is { IsEnabled: true }
            ? new AiAccess(psychologistId, settings.Provider, settings.Sharing)
            : AiErrors.Disabled;
    }

    public async Task<Result<AiSuggestionResponse>> SuggestAsync(
        AiAccess access, AiSuggestionKind kind, PatientSnapshot patient, AiPrompt prompt, CancellationToken cancellationToken)
    {
        IAiTextGenerator? generator = providers.Find(access.Provider);
        if (generator is null)
        {
            return AiErrors.ProviderUnavailable;
        }

        AiPrompt safePrompt = prompt with { User = Pseudonymizer.Apply(prompt.User, patient.Identity) };

        Result<AiCompletion> generated = await GenerateAsync(generator, safePrompt, cancellationToken);
        if (generated.IsFailure)
        {
            return generated.Error;
        }

        AiCompletion completion = generated.Value;
        db.AiUsageLogs.Add(AiUsageLog.Create(currentUser.UserId, patient.Id, kind, access.Provider, completion.Model, safePrompt.User.Length));
        await db.SaveChangesAsync(cancellationToken);

        return new AiSuggestionResponse(completion.Text.Trim(), access.Provider, completion.Model, timeProvider.GetUtcNow(), Disclaimer);
    }

    private async Task<Result<AiCompletion>> GenerateAsync(IAiTextGenerator generator, AiPrompt prompt, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(options.Value.TimeoutSeconds));

        AiCompletion completion;
        try
        {
            completion = await generator.GenerateAsync(prompt, timeout.Token);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Fronteira com serviço externo: qualquer falha do SDK (rede, chave, limite, tempo) vira 503 para o usuário.
            logger.LogWarning(exception, "Falha ao gerar sugestão de IA com o provedor {Provider}.", generator.Provider);
            return AiErrors.ProviderFailed;
        }

        if (completion.Refused)
        {
            return AiErrors.Refused;
        }

        return string.IsNullOrWhiteSpace(completion.Text) ? AiErrors.ProviderFailed : completion;
    }
}
