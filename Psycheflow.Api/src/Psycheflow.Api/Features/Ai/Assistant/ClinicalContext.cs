using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Ai.Assistant;

/// <summary>Dados clínicos (já filtrados pelo que a clínica autorizou) usados para montar o prompt. Nunca contém identificação.</summary>
public sealed record ClinicalContext(
    int? Age,
    ApproachType Approach,
    SessionCounts Counts,
    IReadOnlyList<SessionEntry> Sessions,
    IReadOnlyList<RecordEntry> Records)
{
    public bool HasClinicalData => Sessions.Count > 0 || Records.Count > 0;
}

public sealed record SessionCounts(int Completed, int NoShows, int Cancelled);

public sealed record SessionEntry(DateOnly Date, string? Notes, int? FeedbackScore, string? FeedbackComment);

public sealed record RecordEntry(DateOnly Date, string Title, string Content);

/// <summary>Rascunho das anotações da sessão que o psicólogo quer organizar.</summary>
public sealed record SessionDraft(DateOnly Date, string Text);

/// <summary>Quanto do histórico entra no prompt.</summary>
public sealed record ClinicalScope(int MaxSessions, bool IncludeRecords, Guid? ExcludeSessionId)
{
    public const int MaxRecords = 10;

    /// <summary>Análise e próximos passos: últimas sessões concluídas e registros do prontuário.</summary>
    public static readonly ClinicalScope FullHistory = new(MaxSessions: 10, IncludeRecords: true, ExcludeSessionId: null);

    /// <summary>Anotações da sessão: só as sessões anteriores mais recentes, para dar continuidade.</summary>
    public static ClinicalScope BeforeSession(Guid sessionId) => new(MaxSessions: 3, IncludeRecords: false, ExcludeSessionId: sessionId);
}
