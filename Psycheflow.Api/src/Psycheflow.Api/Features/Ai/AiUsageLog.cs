using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Ai;

public enum AiSuggestionKind
{
    SessionNotes = 0,
    PatientAnalysis = 1,
    NextSteps = 2,
}

/// <summary>
/// Trilha de auditoria de cada sugestão gerada: quem pediu, sobre qual paciente, com qual provedor/modelo e quanto texto foi enviado.
/// O conteúdo do prompt e da resposta não é guardado.
/// </summary>
public sealed class AiUsageLog : Entity, ITenantEntity
{
    public const int ModelMaxLength = 100;

    private AiUsageLog()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid UserId { get; private set; }

    public Guid PatientId { get; private set; }

    public AiSuggestionKind Kind { get; private set; }

    public AiProvider Provider { get; private set; }

    public string Model { get; private set; } = string.Empty;

    public int PromptCharacters { get; private set; }

    public static AiUsageLog Create(Guid userId, Guid patientId, AiSuggestionKind kind, AiProvider provider, string model, int promptCharacters) => new()
    {
        UserId = userId,
        PatientId = patientId,
        Kind = kind,
        Provider = provider,
        Model = model.Length > ModelMaxLength ? model[..ModelMaxLength] : model,
        PromptCharacters = promptCharacters,
    };
}
