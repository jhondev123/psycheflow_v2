namespace Psycheflow.Api.Features.Sessions;

/// <summary>Situação do atendimento (D-04). Só sessões <see cref="Scheduled"/> podem ser alteradas (RN-41).</summary>
public enum SessionStatus
{
    Scheduled = 0,
    Completed = 1,
    Cancelled = 2,
    NoShow = 3,
}
