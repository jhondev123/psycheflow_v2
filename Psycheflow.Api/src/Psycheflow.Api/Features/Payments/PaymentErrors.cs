using Psycheflow.Api.Common.Domain;

namespace Psycheflow.Api.Features.Payments;

public static class PaymentErrors
{
    public static readonly Error NotFound = Error.NotFound("payment.not_found", "Pagamento não encontrado.");

    public static readonly Error NotPending = Error.Conflict(
        "payment.not_pending", "Somente pagamentos pendentes podem ser alterados ou lançados.");

    public static readonly Error AlreadyCancelled = Error.Conflict(
        "payment.already_cancelled", "O pagamento já está cancelado.");

    public static readonly Error SessionNotCompleted = Error.Conflict(
        "payment.session_not_completed", "O pagamento só pode ser lançado depois que a sessão for concluída.");

    public static readonly Error PaidAtInThePast = Error.Validation(
        "payment.paid_at_in_past", "A data do pagamento não pode ser anterior a hoje.", "paidAt");

    public static readonly Error InvalidAmount = Error.Validation(
        "payment.invalid_amount", "O valor não pode ser negativo.", "amount");

    public static readonly Error ReasonRequired = Error.Validation(
        "payment.reason_required", "Informe o motivo.", "reason");

    public static readonly Error PaidBlocksSessionChange = Error.Conflict(
        "payment.paid_blocks_session_change", "O pagamento desta sessão já foi recebido. Cancele (estorne) o pagamento antes.");
}
