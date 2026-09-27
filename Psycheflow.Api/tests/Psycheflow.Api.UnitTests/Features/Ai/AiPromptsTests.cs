using Psycheflow.Api.Features.Ai;
using Psycheflow.Api.Features.Ai.Assistant;
using Psycheflow.Api.Features.Ai.Providers;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.UnitTests.Features.Ai;

public sealed class AiPromptsTests
{
    private static readonly ClinicalContext Context = new(
        Age: 36,
        Approach: ApproachType.CognitiveBehavioral,
        Counts: new SessionCounts(Completed: 2, NoShows: 1, Cancelled: 0),
        Sessions:
        [
            new SessionEntry(new DateOnly(2026, 9, 1), "Relata insônia.", FeedbackScore: 6, FeedbackComment: "Ajudou a organizar a semana."),
            new SessionEntry(new DateOnly(2026, 9, 8), Notes: null, FeedbackScore: 8, FeedbackComment: null),
        ],
        Records: [new RecordEntry(new DateOnly(2026, 8, 20), "Anamnese", "Histórico de ansiedade desde a adolescência.")]);

    [Fact]
    public void Build_PatientAnalysis_DescribesTheContextInPortuguese()
    {
        AiPrompt prompt = AiPrompts.Build(AiSuggestionKind.PatientAnalysis, Context);

        prompt.System.ShouldBe(AiPrompts.SystemPrompt);
        prompt.User.ShouldContain("Idade: 36 anos");
        prompt.User.ShouldContain("Abordagem do psicólogo: Cognitivo-comportamental");
        prompt.User.ShouldContain("Sessões concluídas: 2 · Faltas: 1 · Cancelamentos: 0");
        prompt.User.ShouldContain("### Sessão de 01/09/2026");
        prompt.User.ShouldContain("Relata insônia.");
        prompt.User.ShouldContain("Feedback do paciente: 6/10 — \"Ajudou a organizar a semana.\"");
        prompt.User.ShouldContain("Feedback do paciente: 8/10");
        prompt.User.ShouldContain("### 20/08/2026 — Anamnese");
        prompt.User.ShouldContain("Histórico de ansiedade desde a adolescência.");
    }

    [Fact]
    public void Build_WithoutOptionalData_OmitsTheSections()
    {
        var context = new ClinicalContext(null, ApproachType.NotInformed, new SessionCounts(0, 0, 0), [], []);

        AiPrompt prompt = AiPrompts.Build(AiSuggestionKind.NextSteps, context);

        prompt.User.ShouldContain("Idade: não informada");
        prompt.User.ShouldNotContain("- Abordagem do psicólogo:");
        prompt.User.ShouldNotContain("## Sessões");
        prompt.User.ShouldNotContain("## Prontuário");
    }

    [Fact]
    public void Build_EachKind_HasItsOwnTask()
    {
        string analysis = AiPrompts.Build(AiSuggestionKind.PatientAnalysis, Context).User;
        string nextSteps = AiPrompts.Build(AiSuggestionKind.NextSteps, Context).User;

        analysis.ShouldContain("análise do acompanhamento");
        nextSteps.ShouldContain("próximos passos");
        analysis.ShouldNotBe(nextSteps);
    }

    [Fact]
    public void Build_SessionNotes_IncludesTheDraftAfterThePreviousSessions()
    {
        var draft = new SessionDraft(new DateOnly(2026, 10, 6), "Paciente chegou agitado, falou sobre o trabalho.");

        string user = AiPrompts.Build(AiSuggestionKind.SessionNotes, Context, draft).User;

        user.ShouldContain("## Rascunho da sessão de 06/10/2026");
        user.ShouldContain("Paciente chegou agitado, falou sobre o trabalho.");
        user.IndexOf("## Rascunho", StringComparison.Ordinal).ShouldBeGreaterThan(user.IndexOf("## Sessões", StringComparison.Ordinal));
        user.ShouldContain("registro de evolução");
    }

    [Fact]
    public void SystemPrompt_SetsTheEthicalGuardrails()
    {
        AiPrompts.SystemPrompt.ShouldContain(Pseudonymizer.PatientPlaceholder);
        AiPrompts.SystemPrompt.ShouldContain("não invente");
        AiPrompts.SystemPrompt.ShouldContain("diagnóstico");
        AiPrompts.SystemPrompt.ShouldContain("risco");
    }
}
