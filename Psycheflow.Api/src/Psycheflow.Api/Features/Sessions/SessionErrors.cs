using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Sessions;

public static class SessionErrors
{
    public static readonly Error NotFound = Error.NotFound("session.not_found", "Sessão não encontrada.");

    public static readonly Error NotOpen = Error.Conflict(
        "session.not_open", "A sessão já foi concluída, cancelada ou marcada como falta e não pode mais ser alterada.");

    public static readonly Error NotStartedYet = Error.Conflict(
        "session.not_started", "A sessão ainda não começou.");

    public static readonly Error NotesRequired = Error.Validation(
        "session.notes_required", "Informe as anotações da sessão.", "notes");

    public static readonly Error InvalidFeedbackScore = Error.Validation(
        "session.invalid_feedback_score", "O feedback deve ser uma nota de 0 a 10.", "feedbackScore");

    public static readonly Error ReasonRequired = Error.Validation(
        "session.reason_required", "Informe o motivo.", "reason");

    public static readonly Error CannotAccess = Error.Forbidden(
        "session.cannot_access", "Você só pode acessar as próprias sessões.");

    public static readonly Error ClinicalDataRestricted = Error.Forbidden(
        "session.clinical_data_restricted", "Somente o psicólogo da sessão pode registrar anotações e feedback.");

    public static readonly Error CompletedCannotBeDeleted = Error.Conflict(
        "session.completed_cannot_be_deleted", "Sessões concluídas fazem parte do registro clínico e não podem ser excluídas.");

    public static readonly Error PatientInactive = Error.Validation(
        "session.patient_inactive", "O paciente está inativo.", "patientId");
}
