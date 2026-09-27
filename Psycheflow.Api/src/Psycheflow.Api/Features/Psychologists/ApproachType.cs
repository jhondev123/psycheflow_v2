namespace Psycheflow.Api.Features.Psychologists;

/// <summary>Abordagem teórica do psicólogo.</summary>
public enum ApproachType
{
    NotInformed = 0,
    CognitiveBehavioral = 1,
    Psychoanalysis = 2,
    Behavioral = 3,
    Humanistic = 4,
    Gestalt = 5,
    Systemic = 6,
    Other = 7,
}

public static class ApproachLabels
{
    public static string Of(ApproachType approach) => approach switch
    {
        ApproachType.NotInformed => "Não informada",
        ApproachType.CognitiveBehavioral => "Cognitivo-comportamental",
        ApproachType.Psychoanalysis => "Psicanálise",
        ApproachType.Behavioral => "Comportamental",
        ApproachType.Humanistic => "Humanista",
        ApproachType.Gestalt => "Gestalt-terapia",
        ApproachType.Systemic => "Sistêmica",
        ApproachType.Other => "Outra",
        _ => approach.ToString(),
    };
}
