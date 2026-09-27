using System.Globalization;
using Psycheflow.Api.Common.Domain;
using Psycheflow.Api.Common.Persistence;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Psychologists;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.Features.Documents.Receipt;

/// <summary>UC14 / RF014: recibo em PDF de um pagamento recebido (RN-57).</summary>
public sealed class GetReceiptHandler(AppDbContext db, PaymentAccess payments, DocumentHeaderFactory headers)
{
    public async Task<Result<PdfFile>> Handle(Guid paymentId, CancellationToken cancellationToken)
    {
        Result<Payment> found = await payments.FindManageableAsync(paymentId, cancellationToken);
        if (found.IsFailure)
        {
            return found.Error;
        }

        Payment payment = found.Value;
        if (payment.Status != PaymentStatus.Paid)
        {
            return DocumentErrors.ReceiptRequiresPaidPayment;
        }

        Session session = payment.Session!;
        Psychologist? psychologist = await db.FindPsychologistAsync(session.PsychologistId, cancellationToken);

        var model = new ReceiptModel(
            await headers.CreateAsync(psychologist, cancellationToken),
            session.Patient!.FullName,
            session.Patient.Cpf.Value,
            session.Schedule.Date,
            session.Schedule.StartTime,
            payment.Amount,
            payment.Method!.Value,
            payment.PaidAt!.Value,
            City: null);

        byte[] pdf = new ReceiptDocument(model).GeneratePdf();
        return new PdfFile(pdf, PdfFile.BuildFileName("recibo", payment.PaidAt.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), session.Patient.FullName));
    }
}
