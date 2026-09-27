namespace Psycheflow.Api.Features.Payments.ListPayments;

/// <param name="From">Sessões a partir desta data (opcional).</param>
/// <param name="To">Sessões até esta data (opcional).</param>
public sealed record ListPaymentsQuery(
    DateOnly? From = null,
    DateOnly? To = null,
    Guid? PatientId = null,
    Guid? PsychologistId = null,
    PaymentStatus? Status = null,
    int? Page = null,
    int? PageSize = null);
