using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Payments;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.Documents.Receipt;

/// <param name="City">Cidade exibida antes da data (opcional).</param>
public sealed record ReceiptModel(
    DocumentHeaderData Header,
    string PatientName,
    string PatientCpf,
    DateOnly SessionDate,
    TimeOnly SessionStart,
    decimal Amount,
    PaymentMethod Method,
    DateOnly PaidAt,
    string? City);

/// <summary>RF014: recibo de atendimento psicológico (valor por extenso, forma e data de pagamento).</summary>
public sealed class ReceiptDocument(ReceiptModel model) : IDocument
{
    public void Compose(IDocumentContainer container) =>
        container.Page(page =>
        {
            PdfLayout.ApplyPage(page);
            page.Header().Element(c => PdfLayout.Header(c, model.Header, "Recibo", PdfLayout.Money(model.Amount)));
            page.Content().PaddingTop(20).Column(column =>
            {
                column.Spacing(14);
                column.Item().Text(text =>
                {
                    text.Justify();
                    text.Span("Recebi de ");
                    text.Span(model.PatientName).SemiBold();
                    text.Span($", inscrito(a) no CPF {DocumentLabels.FormatCpf(model.PatientCpf)}, a importância de ");
                    text.Span($"{PdfLayout.Money(model.Amount)} ({AmountInWords.ToPortuguese(model.Amount)})").SemiBold();
                    text.Span(", referente a atendimento psicológico realizado em ");
                    text.Span($"{PdfLayout.Date(model.SessionDate)} às {PdfLayout.Time(model.SessionStart)}");
                    text.Span($", pago via {DocumentLabels.Of(model.Method)} em {PdfLayout.Date(model.PaidAt)}.");
                });
                column.Item().Text("Para clareza, firmo o presente recibo.");
                column.Item().PaddingTop(10).AlignRight().Text(
                    model.City is null ? PdfLayout.LongDate(model.PaidAt) : $"{model.City}, {PdfLayout.LongDate(model.PaidAt)}.");
                column.Item().Element(c => PdfLayout.Signature(c, model.Header));
            });
            page.Footer().Element(PdfLayout.Footer);
        });
}
