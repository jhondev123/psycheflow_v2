using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Ai.Assistant;
using Psycheflow.Api.Features.Patients;

namespace Psycheflow.Api.Features.Ai.Suggestions.PatientSuggestions;

/// <summary>
/// Análise do paciente e próximos passos: mesmo contexto (sessões e prontuário do psicólogo logado, conforme os dados
/// autorizados pela clínica), tarefas diferentes no prompt.
/// </summary>
public sealed class SuggestForPatientHandler(AiAssistant assistant, ClinicalContextLoader loader)
{
    public async Task<Result<AiSuggestionResponse>> Handle(AiSuggestionKind kind, PatientSuggestionRequest request, CancellationToken cancellationToken)
    {
        Result<AiAccess> access = await assistant.AuthorizeAsync(cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        PatientSnapshot? patient = await loader.FindPatientAsync(request.PatientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        ClinicalContext context = await loader.LoadAsync(patient, access.Value, ClinicalScope.FullHistory, cancellationToken);
        if (!context.HasClinicalData)
        {
            return AiErrors.InsufficientData;
        }

        return await assistant.SuggestAsync(access.Value, kind, patient, AiPrompts.Build(kind, context), cancellationToken);
    }
}
