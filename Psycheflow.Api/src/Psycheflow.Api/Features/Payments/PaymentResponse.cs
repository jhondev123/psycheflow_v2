using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Payments;

public sealed record PaymentResponse(
    Guid Id,
    Guid SessionId,
    Guid PatientId,
    string PatientName,
    Guid PsychologistId,
    DateOnly SessionDate,
    TimeOnly SessionStartTime,
    SessionStatus SessionStatus,
    decimal Amount,
    PaymentStatus Status,
    PaymentMethod? Method,
    DateOnly? PaidAt,
    string? Notes,
    string? CancellationReason)
{
    /// <summary>Requer <c>Session.Schedule</c> e <c>Session.Patient</c> carregados.</summary>
    public static PaymentResponse From(Payment payment)
    {
        Session session = payment.Session!;
        return new PaymentResponse(
            payment.Id,
            session.Id,
            session.PatientId,
            session.Patient!.FullName,
            session.PsychologistId,
            session.Schedule.Date,
            session.Schedule.StartTime,
            session.Status,
            payment.Amount,
            payment.Status,
            payment.Method,
            payment.PaidAt,
            payment.Notes,
            payment.CancellationReason);
    }
}

/// <summary>Resumo do pagamento exibido junto com a sessão.</summary>
public sealed record SessionPaymentSummary(Guid Id, decimal Amount, PaymentStatus Status)
{
    public static SessionPaymentSummary? From(Payment? payment) =>
        payment is null ? null : new SessionPaymentSummary(payment.Id, payment.Amount, payment.Status);
}
