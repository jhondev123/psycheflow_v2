using Psycheflow.Api.Features.Documents.Pdf;
using Psycheflow.Api.Features.Documents.Receipt;
using Psycheflow.Api.Features.Documents.SessionsReport;
using Psycheflow.Api.Features.Payments;
using Psycheflow.Api.Features.Sessions;
using QuestPDF.Fluent;

namespace Psycheflow.Api.UnitTests.Features.Documents;

/// <summary>Garante que os layouts QuestPDF renderizam (sem estouro de layout) com dados típicos e extremos.</summary>
public sealed class PdfRenderingTests
{
    private static readonly DocumentHeaderData Header = new("Clínica Viver", "Ana Souza", "06/12345", new DateTimeOffset(2026, 10, 6, 15, 0, 0, TimeSpan.FromHours(-3)));

    static PdfRenderingTests() => PdfSetup.Configure();

    private static void ShouldRenderPdf(byte[] pdf)
    {
        pdf.Length.ShouldBeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(pdf, 0, 4).ShouldBe("%PDF");
    }

    [Fact]
    public void Receipt_Renders()
    {
        var model = new ReceiptModel(
            Header, "Maria Clara", "529.982.247-25", new DateOnly(2026, 10, 6), new TimeOnly(14, 0),
            180m, PaymentMethod.Pix, new DateOnly(2026, 10, 6), "Cascavel");

        ShouldRenderPdf(new ReceiptDocument(model).GeneratePdf());
    }

    [Fact]
    public void SessionsReport_WithManyRows_RendersMultiplePages()
    {
        List<SessionsReportRow> rows =
        [
            .. Enumerable.Range(0, 120).Select(i => new SessionsReportRow(
                new DateOnly(2026, 10, 6).AddDays(i / 5), new TimeOnly(8 + (i % 5), 0), $"Paciente {i}",
                SessionStatus.Completed, i % 2 == 0 ? "Evolução registrada com bastante texto para quebrar linha." : null,
                150m, PaymentStatus.Paid, PaymentMethod.Pix)),
        ];
        var model = new SessionsReportModel(Header, new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31), "Todos", "Todos", rows);

        ShouldRenderPdf(new SessionsReportDocument(model).GeneratePdf());
    }

    [Fact]
    public void SessionsReport_Empty_Renders() =>
        ShouldRenderPdf(new SessionsReportDocument(
            new SessionsReportModel(Header, new DateOnly(2026, 10, 1), null, "Todos", "Todos", [])).GeneratePdf());
}
