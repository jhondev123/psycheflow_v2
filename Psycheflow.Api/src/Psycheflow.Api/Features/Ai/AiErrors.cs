using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Ai;

public static class AiErrors
{
    public static readonly Error Disabled = Error.Forbidden(
        "ai.disabled", "O assistente de IA não está habilitado nesta clínica. Um administrador pode habilitá-lo nas configurações.");

    public static readonly Error OnlyPsychologists = Error.Forbidden(
        "ai.only_psychologists", "Somente psicólogos podem usar o assistente de IA.");

    public static readonly Error NotYourSession = Error.Forbidden(
        "ai.not_your_session", "Somente o psicólogo da sessão pode pedir sugestões sobre ela.");

    public static readonly Error DataNotShared = Error.Forbidden(
        "ai.data_not_shared", "A clínica não autorizou o envio das anotações de sessão para a IA.");

    public static readonly Error NoDataShared = Error.Validation(
        "ai.no_data_shared", "Escolha ao menos um tipo de dado que pode ser enviado à IA.");

    public static readonly Error ProviderNotConfigured = Error.Validation(
        "ai.provider_not_configured", "Este provedor de IA não está configurado no servidor.", "provider");

    public static readonly Error EmptyDraft = Error.Validation(
        "ai.empty_draft", "Escreva um rascunho das anotações para receber a sugestão.", "draft");

    public static readonly Error InsufficientData = Error.Validation(
        "ai.insufficient_data",
        "Ainda não há dados clínicos autorizados (anotações, feedbacks ou prontuário) para gerar sugestões sobre este paciente.");

    public static readonly Error Refused = Error.Validation(
        "ai.refused", "O provedor de IA recusou gerar uma sugestão para este conteúdo.");

    public static readonly Error ProviderUnavailable = Error.Unavailable(
        "ai.provider_unavailable", "O provedor de IA escolhido pela clínica não está configurado no servidor.");

    public static readonly Error ProviderFailed = Error.Unavailable(
        "ai.provider_failed", "O provedor de IA não respondeu. Tente novamente em instantes.");
}
