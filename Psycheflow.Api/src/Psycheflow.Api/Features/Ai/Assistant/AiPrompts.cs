using System.Globalization;
using System.Text;
using Psycheflow.Api.Features.Ai.Providers;
using Psycheflow.Api.Features.Psychologists;

namespace Psycheflow.Api.Features.Ai.Assistant;

/// <summary>
/// Prompt padrão, o mesmo para os três provedores: instruções fixas de sistema (papel, ética, formato)
/// e uma mensagem com os dados clínicos em Markdown seguidos da tarefa de cada tipo de sugestão.
/// </summary>
public static class AiPrompts
{
    public const string SystemPrompt = """
        Você é um assistente de apoio clínico para psicólogos(as) registrados(as) no Conselho Federal de Psicologia (CFP), integrado ao Psycheflow, um sistema de gestão de clínicas de psicologia.
        Seu papel é sugerir: a avaliação, as decisões e a responsabilidade técnica são sempre do(a) psicólogo(a) que usa o sistema.

        Regras:
        - Responda em português do Brasil, em Markdown, de forma objetiva e com linguagem técnica adequada a um prontuário psicológico.
        - Use somente as informações fornecidas: não invente fatos, falas, datas ou histórico. Quando faltar informação relevante, diga o que falta.
        - Não feche diagnóstico nem sugira medicação. Hipóteses devem aparecer como hipóteses a investigar, sempre ligadas aos dados.
        - Respeite o Código de Ética Profissional do Psicólogo e o sigilo: os dados foram pseudonimizados e o paciente aparece como [paciente]. Não tente identificá-lo nem peça dados pessoais.
        - Se houver indícios de risco (ideação suicida, autolesão, violência ou negligência), destaque isso no início da resposta e recomende avaliação imediata pelo profissional.
        """;

    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static AiPrompt Build(AiSuggestionKind kind, ClinicalContext context, SessionDraft? draft = null)
    {
        var user = new StringBuilder();
        AppendProfile(user, context);
        AppendSessions(user, context.Sessions);
        AppendRecords(user, context.Records);

        if (draft is not null)
        {
            user.AppendLine(PtBr, $"## Rascunho da sessão de {Format(draft.Date)}")
                .AppendLine(draft.Text)
                .AppendLine();
        }

        user.AppendLine("## Tarefa").Append(TaskFor(kind));
        return new AiPrompt(SystemPrompt, user.ToString());
    }

    private static void AppendProfile(StringBuilder user, ClinicalContext context)
    {
        user.AppendLine("## Paciente")
            .AppendLine(context.Age is { } age ? $"- Idade: {age} anos" : "- Idade: não informada");

        if (context.Approach != ApproachType.NotInformed)
        {
            user.AppendLine(PtBr, $"- Abordagem do psicólogo: {ApproachLabels.Of(context.Approach)}");
        }

        user.AppendLine(PtBr, $"- Sessões concluídas: {context.Counts.Completed} · Faltas: {context.Counts.NoShows} · Cancelamentos: {context.Counts.Cancelled}")
            .AppendLine();
    }

    private static void AppendSessions(StringBuilder user, IReadOnlyList<SessionEntry> sessions)
    {
        if (sessions.Count == 0)
        {
            return;
        }

        user.AppendLine("## Sessões concluídas (da mais antiga para a mais recente)");
        foreach (SessionEntry session in sessions)
        {
            user.AppendLine(PtBr, $"### Sessão de {Format(session.Date)}");
            if (session.Notes is not null)
            {
                user.AppendLine(session.Notes);
            }

            if (session.FeedbackScore is { } score)
            {
                string comment = session.FeedbackComment is null ? string.Empty : $" — \"{session.FeedbackComment}\"";
                user.AppendLine(PtBr, $"Feedback do paciente: {score}/10{comment}");
            }

            user.AppendLine();
        }
    }

    private static void AppendRecords(StringBuilder user, IReadOnlyList<RecordEntry> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        user.AppendLine("## Prontuário (registros do psicólogo)");
        foreach (RecordEntry record in records)
        {
            user.AppendLine(PtBr, $"### {Format(record.Date)} — {record.Title}")
                .AppendLine(record.Content)
                .AppendLine();
        }
    }

    private static string TaskFor(AiSuggestionKind kind) => kind switch
    {
        AiSuggestionKind.SessionNotes =>
            "Reescreva o rascunho acima como um registro de evolução da sessão, claro e profissional, organizado em: "
            + "**Demanda/tema**, **Observações clínicas**, **Intervenções**, **Evolução** (em relação às sessões anteriores, se houver) "
            + "e **Combinados**. Preserve o conteúdo do rascunho e não acrescente fatos.",
        AiSuggestionKind.PatientAnalysis =>
            "Faça uma análise do acompanhamento: temas recorrentes, evolução ao longo das sessões (incluindo a tendência do feedback, se houver), "
            + "pontos de atenção e lacunas de informação que valeria investigar.",
        AiSuggestionKind.NextSteps =>
            "Sugira próximos passos para o acompanhamento: objetivos para as próximas sessões, intervenções ou técnicas coerentes com a abordagem "
            + "do psicólogo, instrumentos de avaliação que poderiam ser considerados e se há indicação de encaminhamento. "
            + "Justifique cada sugestão com base nos dados.",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Tipo de sugestão desconhecido."),
    };

    private static string Format(DateOnly date) => date.ToString("dd/MM/yyyy", PtBr);
}
