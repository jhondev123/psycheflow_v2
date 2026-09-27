using Microsoft.EntityFrameworkCore;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Ai.Assistant;
using Psycheflow.Api.Features.Ai.Providers;
using Psycheflow.Api.Features.Patients;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Ai.Suggestions.SessionNotes;

/// <summary>Organiza o rascunho das anotações de uma sessão em um registro de evolução (só o psicólogo da sessão).</summary>
public sealed class SuggestSessionNotesHandler(AppDbContext db, AiAssistant assistant, ClinicalContextLoader loader)
{
    public async Task<Result<AiSuggestionResponse>> Handle(SuggestSessionNotesRequest request, CancellationToken cancellationToken)
    {
        Result<AiAccess> access = await assistant.AuthorizeAsync(cancellationToken);
        if (access.IsFailure)
        {
            return access.Error;
        }

        if (!access.Value.Sharing.SessionNotes)
        {
            return AiErrors.DataNotShared;
        }

        var session = await db.Sessions
            .Where(s => s.Id == request.SessionId)
            .Select(s => new { s.PsychologistId, s.PatientId, s.Schedule.Date, s.Notes })
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return SessionErrors.NotFound;
        }

        if (session.PsychologistId != access.Value.PsychologistId)
        {
            return AiErrors.NotYourSession;
        }

        string? draft = string.IsNullOrWhiteSpace(request.Draft) ? session.Notes : request.Draft;
        if (string.IsNullOrWhiteSpace(draft))
        {
            return AiErrors.EmptyDraft;
        }

        PatientSnapshot? patient = await loader.FindPatientAsync(session.PatientId, cancellationToken);
        if (patient is null)
        {
            return PatientErrors.NotFound;
        }

        ClinicalContext context = await loader.LoadAsync(patient, access.Value, ClinicalScope.BeforeSession(request.SessionId), cancellationToken);
        AiPrompt prompt = AiPrompts.Build(AiSuggestionKind.SessionNotes, context, new SessionDraft(session.Date, draft.Trim()));

        return await assistant.SuggestAsync(access.Value, AiSuggestionKind.SessionNotes, patient, prompt, cancellationToken);
    }
}
