using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Sessions;

namespace Psycheflow.Api.Features.Documents.Pdf;

/// <summary>Rótulos em pt-BR dos enums exibidos nos documentos.</summary>
public static class DocumentLabels
{
    public static string Of(SessionStatus status) => status switch
    {
        SessionStatus.Scheduled => "Agendada",
        SessionStatus.Completed => "Concluída",
        SessionStatus.Cancelled => "Cancelada",
        SessionStatus.NoShow => "Falta",
        _ => status.ToString(),
    };

    public static string Of(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "Pendente",
        PaymentStatus.Paid => "Pago",
        PaymentStatus.Cancelled => "Cancelado",
        _ => status.ToString(),
    };

    public static string Of(PaymentMethod method) => method switch
    {
        PaymentMethod.CreditCard => "Cartão de crédito",
        PaymentMethod.DebitCard => "Cartão de débito",
        PaymentMethod.Pix => "Pix",
        _ => method.ToString(),
    };

    public static string FormatCpf(string cpf) =>
        cpf.Length == 11 ? $"{cpf[..3]}.{cpf[3..6]}.{cpf[6..9]}-{cpf[9..]}" : cpf;
}
