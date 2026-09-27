namespace Psycheflow.Api.Features.Payments.PayPayment;

/// <param name="Method">Cartão de crédito, débito ou Pix (RN-52).</param>
/// <param name="PaidAt">Data do pagamento; padrão = hoje. Não pode ser anterior a hoje (RN-53).</param>
/// <param name="Amount">Valor recebido; padrão = valor atual do pagamento.</param>
/// <param name="Notes">Informações extras.</param>
public sealed record PayPaymentRequest(PaymentMethod? Method, DateOnly? PaidAt = null, decimal? Amount = null, string? Notes = null);
