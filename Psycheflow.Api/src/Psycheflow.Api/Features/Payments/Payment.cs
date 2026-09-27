using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Payments;

/// <summary>
/// Cobrança de uma sessão (RF011–RF013). Nasce pendente junto com a sessão (RN-50, D-08); "lançar o pagamento"
/// é registrá-lo como pago, o que só é possível depois da sessão concluída (RN-55).
/// </summary>
public sealed class Payment : Entity, ITenantEntity, ISoftDeletable
{
    public const int NotesMaxLength = 500;

    private Payment()
    {
    }

    public Guid CompanyId { get; private set; }

    public Guid SessionId { get; private set; }

    public Session? Session { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentStatus Status { get; private set; }

    public PaymentMethod? Method { get; private set; }

    public DateOnly? PaidAt { get; private set; }

    /// <summary>Informações extras (RF011).</summary>
    public string? Notes { get; private set; }

    public string? CancellationReason { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public static Payment ForSession(Guid sessionId, decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        return new Payment { SessionId = sessionId, Amount = amount, Status = PaymentStatus.Pending };
    }

    /// <summary>RN-52, RN-53, RN-55: registra o recebimento.</summary>
    public Result Pay(bool sessionCompleted, PaymentMethod method, DateOnly paidAt, decimal? amount, string? notes, DateOnly today)
    {
        if (Status != PaymentStatus.Pending)
        {
            return PaymentErrors.NotPending;
        }

        if (!sessionCompleted)
        {
            return PaymentErrors.SessionNotCompleted;
        }

        if (paidAt < today)
        {
            return PaymentErrors.PaidAtInThePast;
        }

        if (amount is < 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        Status = PaymentStatus.Paid;
        Method = method;
        PaidAt = paidAt;
        Amount = amount ?? Amount;
        Notes = string.IsNullOrWhiteSpace(notes) ? Notes : notes.Trim();
        return Result.Success();
    }

    /// <summary>RN-56: o valor só muda enquanto o pagamento está pendente.</summary>
    public Result ChangeAmount(decimal amount)
    {
        if (Status != PaymentStatus.Pending)
        {
            return PaymentErrors.NotPending;
        }

        if (amount < 0)
        {
            return PaymentErrors.InvalidAmount;
        }

        Amount = amount;
        return Result.Success();
    }

    /// <summary>Cancela um pendente ou estorna um pago (RN-43/44).</summary>
    public Result Cancel(string reason)
    {
        if (Status == PaymentStatus.Cancelled)
        {
            return PaymentErrors.AlreadyCancelled;
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            return PaymentErrors.ReasonRequired;
        }

        Status = PaymentStatus.Cancelled;
        CancellationReason = reason.Trim();
        return Result.Success();
    }
}
