using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Ai;

/// <summary>Provedores de IA suportados. A chave de API de cada um fica só no servidor (configuração "Ai").</summary>
public enum AiProvider
{
    Claude = 0,
    OpenAi = 1,
    Gemini = 2,
}

/// <summary>Tipos de dado clínico que a clínica autorizou enviar à IA (sempre pseudonimizados).</summary>
public sealed record AiDataSharing(bool SessionNotes, bool Feedbacks, bool MedicalRecords)
{
    public static readonly AiDataSharing None = new(false, false, false);

    public bool Any => SessionNotes || Feedbacks || MedicalRecords;
}

/// <summary>
/// Uso da IA pela clínica (RF021, D-07): desligado por padrão; ao habilitar, um Admin/Manager escolhe o provedor,
/// os dados que podem ser enviados e registra o aceite dos termos (quem e quando) — base para a LGPD.
/// </summary>
public sealed class AiSettings : Entity, ITenantEntity
{
    private AiSettings()
    {
    }

    public Guid CompanyId { get; private set; }

    public bool IsEnabled { get; private set; }

    public AiProvider Provider { get; private set; }

    public bool ShareSessionNotes { get; private set; }

    public bool ShareFeedbacks { get; private set; }

    public bool ShareMedicalRecords { get; private set; }

    public DateTimeOffset? ConsentAcceptedAt { get; private set; }

    public Guid? ConsentAcceptedByUserId { get; private set; }

    public AiDataSharing Sharing => new(ShareSessionNotes, ShareFeedbacks, ShareMedicalRecords);

    public static AiSettings CreateDisabled() => new() { Provider = AiProvider.Claude };

    public Result Enable(AiProvider provider, AiDataSharing sharing, Guid acceptedByUserId, DateTimeOffset now)
    {
        if (!sharing.Any)
        {
            return AiErrors.NoDataShared;
        }

        Apply(provider, sharing);
        IsEnabled = true;
        ConsentAcceptedAt = now;
        ConsentAcceptedByUserId = acceptedByUserId;
        return Result.Success();
    }

    /// <summary>Desliga a IA e revoga o aceite; as escolhas ficam salvas para uma futura reativação.</summary>
    public void Disable(AiProvider provider, AiDataSharing sharing)
    {
        Apply(provider, sharing);
        IsEnabled = false;
        ConsentAcceptedAt = null;
        ConsentAcceptedByUserId = null;
    }

    private void Apply(AiProvider provider, AiDataSharing sharing)
    {
        Provider = provider;
        ShareSessionNotes = sharing.SessionNotes;
        ShareFeedbacks = sharing.Feedbacks;
        ShareMedicalRecords = sharing.MedicalRecords;
    }
}
