using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Psycheflow.Api.Features.Documents.SessionsReport;

/// <param name="Notes">Observações (anotações) — só preenchidas quando o solicitante é o psicólogo da sessão.</param>
public sealed record SessionsReportRow(
    DateOnly Date,
    TimeOnly Start,
    string PatientName,
    SessionStatus Status,
    string? Notes,
    decimal? Amount,
    PaymentStatus? PaymentStatus,
    PaymentMethod? PaymentMethod);

public sealed record SessionsReportModel(
    DocumentHeaderData Header,
    DateOnly From,
    DateOnly? To,
    string SessionStatusFilter,
    string PaymentStatusFilter,
    IReadOnlyList<SessionsReportRow> Rows);

/// <summary>RF015: sessões do período com dados de pagamento e totais.</summary>
public sealed class SessionsReportDocument(SessionsReportModel model) : IDocument
{
    private static readonly string[] ColumnTitles = ["Data", "Hora", "Paciente", "Sessão", "Valor", "Pagamento"];

    public void Compose(IDocumentContainer container) =>
        container.Page(page =>
        {
            PdfLayout.ApplyPage(page);
            page.Header().Element(c => PdfLayout.Header(c, model.Header, "Relatório de sessões", Subtitle()));
            page.Content().Column(column =>
            {
                column.Spacing(12);
                column.Item().Element(Summary);
                column.Item().Element(Table);
            });
            page.Footer().Element(PdfLayout.Footer);
        });

    private string Subtitle()
    {
        string period = model.To is { } to
            ? $"{PdfLayout.Date(model.From)} a {PdfLayout.Date(to)}"
            : $"a partir de {PdfLayout.Date(model.From)}";
        return $"Período: {period} · Sessões: {model.SessionStatusFilter} · Pagamentos: {model.PaymentStatusFilter}";
    }

    private void Summary(IContainer container)
    {
        int completed = model.Rows.Count(r => r.Status == SessionStatus.Completed);
        int cancelled = model.Rows.Count(r => r.Status == SessionStatus.Cancelled);
        decimal paid = model.Rows.Where(r => r.PaymentStatus == Payments.PaymentStatus.Paid).Sum(r => r.Amount ?? 0);
        decimal pending = model.Rows.Where(r => r.PaymentStatus == Payments.PaymentStatus.Pending).Sum(r => r.Amount ?? 0);

        container.Border(1).BorderColor(PdfLayout.Border).Padding(10).Row(row =>
        {
            SummaryItem(row, "Sessões", model.Rows.Count.ToString(PdfLayout.Culture));
            SummaryItem(row, "Concluídas", completed.ToString(PdfLayout.Culture));
            SummaryItem(row, "Canceladas", cancelled.ToString(PdfLayout.Culture));
            SummaryItem(row, "Recebido", PdfLayout.Money(paid));
            SummaryItem(row, "A receber", PdfLayout.Money(pending));
        });
    }

    private static void SummaryItem(RowDescriptor row, string label, string value) =>
        row.RelativeItem().Column(column =>
        {
            column.Item().Text(label).FontSize(8).FontColor(PdfLayout.Muted);
            column.Item().Text(value).SemiBold();
        });

    private void Table(IContainer container)
    {
        if (model.Rows.Count == 0)
        {
            container.PaddingTop(20).AlignCenter().Text("Nenhuma sessão encontrada com os filtros informados.").FontColor(PdfLayout.Muted);
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(62);
                columns.ConstantColumn(38);
                columns.RelativeColumn(3);
                columns.ConstantColumn(62);
                columns.ConstantColumn(64);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                foreach (string title in ColumnTitles)
                {
                    header.Cell().Background(PdfLayout.Primary).Padding(4).Text(title).FontColor(Colors.White).FontSize(9).SemiBold();
                }
            });

            foreach (SessionsReportRow row in model.Rows)
            {
                table.Cell().Element(Cell).Text(PdfLayout.Date(row.Date));
                table.Cell().Element(Cell).Text(PdfLayout.Time(row.Start));
                table.Cell().Element(Cell).Text(row.PatientName);
                table.Cell().Element(Cell).Text(DocumentLabels.Of(row.Status));
                table.Cell().Element(Cell).Text(row.Amount is { } amount ? PdfLayout.Money(amount) : "—");
                table.Cell().Element(Cell).Text(Payment(row));

                if (!string.IsNullOrWhiteSpace(row.Notes))
                {
                    table.Cell().ColumnSpan(6).Element(NotesCell).Text($"Observações: {row.Notes}").FontSize(8.5f).Italic();
                }
            }
        });
    }

    private static string Payment(SessionsReportRow row) => row.PaymentStatus switch
    {
        null => "—",
        Payments.PaymentStatus.Paid when row.PaymentMethod is { } method => $"Pago ({DocumentLabels.Of(method)})",
        { } status => DocumentLabels.Of(status),
    };

    private static IContainer Cell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(PdfLayout.Border).PaddingVertical(4).PaddingHorizontal(3).DefaultTextStyle(s => s.FontSize(9));

    private static IContainer NotesCell(IContainer container) =>
        container.BorderBottom(0.5f).BorderColor(PdfLayout.Border).PaddingBottom(5).PaddingHorizontal(3).DefaultTextStyle(s => s.FontColor(PdfLayout.Muted));
}
