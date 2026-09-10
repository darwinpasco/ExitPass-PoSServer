using System.Text;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.ElectronicJournal;

public sealed class CanonicalSalesInvoiceTextRendererTests
{
    [Fact]
    public void PrintAndElectronicJournalUseTheSameCanonicalUtf8Bytes()
    {
        var renderer = new CanonicalSalesInvoiceTextRenderer();
        var invoice = Invoice();

        var printed = new SalesInvoicePrinterPayloadRenderer(renderer).RenderVisibleText(invoice);
        var journal = new ElectronicJournalInvoiceTextRenderer(renderer).RenderPersisted([
            new(invoice.FiscalDocumentId, invoice.FiscalDocumentNumber, invoice.BusinessDayDate, invoice.IssuedAt, printed.Text)
        ]);

        Assert.Equal(printed.Bytes, journal.Bytes);
        Assert.Equal("text/plain; charset=utf-8", journal.ContentType);
        Assert.EndsWith(".txt", journal.FileName, StringComparison.Ordinal);
        Assert.Equal(printed.Text, Encoding.UTF8.GetString(journal.Bytes));
    }

    [Fact]
    public void RendererCoversRecoveredReferenceSampleStructureAndCurrentApprovedLabels()
    {
        var text = new CanonicalSalesInvoiceTextRenderer().Render(Invoice()).Text;
        var requiredStructure = new[]
        {
            "SALES INVOICE", "ExitPass Parking Corporation", "TIN", "POS Serial No.", "MIN", "PTU No.",
            "BIR Accreditation", "Parking Location", "Terminal", "SI No.", "Issued At", "Parking Details",
            "Parking Ref", "Ticket No.", "Plate No.", "Entry Time", "Payment Time", "Duration",
            "Qty  Description", "Subtotal", "Senior Discount", "VATable Sales", "VAT Amount",
            "VAT Exempt Sales", "Zero Rated Sales", "Payment Details", "CASH", "Total Paid",
            "Sales Invoice declaration", "Print / Issued At", "Customer Information", "Software Supplier"
        };

        foreach (var value in requiredStructure) Assert.Contains(value, text, StringComparison.Ordinal);
        Assert.True(Count(text, "------------------------------------------------") >= 6);
    }

    [Fact]
    public void RequiredCustomerAndVatFieldsRemainOrderedAndZeroValuesStayVisible()
    {
        var text = new CanonicalSalesInvoiceTextRenderer().Render(Invoice()).Text;
        var required = new[]
        {
            "Customer Name      : Juan Dela Cruz",
            "Address            : 123 Sample Street",
            "TIN                : 123-456-789-000",
            "Business Style     : Retail",
            "OSCA ID No.        : OSCA-0001",
            "Customer Signature : ____________________",
            "VATable Sales      : PHP 100.00",
            "VAT Amount         : PHP 12.00",
            "VAT Exempt Sales   : PHP 0.00",
            "Zero Rated Sales   : PHP 0.00"
        };

        var prior = -1;
        foreach (var value in required)
        {
            var index = text.IndexOf(value, prior + 1, StringComparison.Ordinal);
            Assert.True(index > prior, $"Expected '{value}' after the prior canonical field.");
            prior = index;
        }
    }

    [Fact]
    public void ChronologicalExportIsStableAndDoesNotDuplicateAReplayedInvoice()
    {
        var renderer = new ElectronicJournalInvoiceTextRenderer(new CanonicalSalesInvoiceTextRenderer());
        var later = Invoice() with
        {
            FiscalDocumentId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002"),
            FiscalDocumentNumber = "SI-00000002",
            IssuedAt = DateTimeOffset.Parse("2026-09-10T02:00:00Z")
        };
        var first = renderer.Render([later, Invoice(), Invoice()]);
        var second = renderer.Render([Invoice(), later]);

        Assert.Equal(first.Bytes, second.Bytes);
        Assert.True(first.Text.IndexOf("SI-00000001", StringComparison.Ordinal) < first.Text.IndexOf("SI-00000002", StringComparison.Ordinal));
        Assert.Equal(1, Count(first.Text, "SI-00000001"));
        Assert.Equal(2, Count(first.Text, "SALES INVOICE"));
        Assert.Contains("\r\n\r\n", first.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void GeneratedEvidenceArtifactIsStrictUtf8AndMatchesPrintedInvoiceBytes()
    {
        var renderer = new CanonicalSalesInvoiceTextRenderer();
        var printer = new SalesInvoicePrinterPayloadRenderer(renderer);
        var firstInvoice = Invoice();
        var secondInvoice = firstInvoice with
        {
            FiscalDocumentId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002"),
            FiscalDocumentNumber = "SI-00000002",
            IssuedAt = DateTimeOffset.Parse("2026-09-10T02:00:00Z"),
            Presentation = firstInvoice.Presentation! with { PresentationTimestamp = DateTimeOffset.Parse("2026-09-10T02:00:00Z") }
        };
        var firstPrint = printer.RenderVisibleText(firstInvoice);
        var secondPrint = printer.RenderVisibleText(secondInvoice);
        var journal = new ElectronicJournalInvoiceTextRenderer(renderer).RenderPersisted([
            new(secondInvoice.FiscalDocumentId, secondInvoice.FiscalDocumentNumber, secondInvoice.BusinessDayDate, secondInvoice.IssuedAt, secondPrint.Text),
            new(firstInvoice.FiscalDocumentId, firstInvoice.FiscalDocumentNumber, firstInvoice.BusinessDayDate, firstInvoice.IssuedAt, firstPrint.Text),
            new(firstInvoice.FiscalDocumentId, firstInvoice.FiscalDocumentNumber, firstInvoice.BusinessDayDate, firstInvoice.IssuedAt, firstPrint.Text)
        ]);
        var evidenceDirectory = Path.Combine(AppContext.BaseDirectory, "g4-evidence");
        Directory.CreateDirectory(evidenceDirectory);
        var evidencePath = Path.Combine(evidenceDirectory, journal.FileName);
        File.WriteAllBytes(evidencePath, journal.Bytes);

        var strictUtf8 = new UTF8Encoding(false, true);
        var expected = Encoding.UTF8.GetBytes(firstPrint.Text + "\r\n" + secondPrint.Text);
        Assert.Equal(expected, File.ReadAllBytes(evidencePath));
        Assert.Equal(firstPrint.Text + "\r\n" + secondPrint.Text, strictUtf8.GetString(File.ReadAllBytes(evidencePath)));
        Assert.Equal(2, Count(journal.Text, "SALES INVOICE"));
        Assert.Equal(1, Count(journal.Text, "SI-00000001"));
        Assert.Equal(1, Count(journal.Text, "SI-00000002"));
        Assert.False(File.ReadAllBytes(evidencePath).AsSpan().StartsWith(Encoding.UTF8.Preamble));
    }

    private static CanonicalSalesInvoiceText Invoice() => new(
        Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"),
        "SI-00000001",
        DateTimeOffset.Parse("2026-09-10T01:00:00Z"),
        new DateOnly(2026, 9, 10),
        "ExitPass Parking Corporation",
        "PITX, Paranaque City",
        "123-456-789-000",
        "POS-SN-001",
        "MIN-001",
        "PTU-001",
        "BIR-ACC-001",
        new("Juan Dela Cruz", "123 Sample Street", "123-456-789-000", "Retail", "OSCA ID No.", "OSCA-0001", true),
        [new(1, "Parking fee", 1m, 11200, "PHP")],
        10000,
        1200,
        0,
        0,
        11200,
        "PHP",
        "Thank you for parking with us.\r\nSoftware Supplier  : ExitPass Software Inc.\r\nBIR Accreditation  : BIR-ACC-001\r\nPTU No.            : PTU-001",
        new(
            "PITX Parking Facility",
            "TERMINAL-01",
            "PARKING-SESSION-001",
            "TICKET-0001",
            "ABC-1234",
            "2026-09-10 00:00:00 UTC",
            "2026-09-10 01:00:00 UTC",
            "01:00:00",
            12000,
            [new("Senior Discount", 800)],
            [new("CASH", 11200)],
            11200,
            12000,
            800,
            "This document serves as the official Sales Invoice declaration.",
            DateTimeOffset.Parse("2026-09-10T01:00:00Z")));

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
}
