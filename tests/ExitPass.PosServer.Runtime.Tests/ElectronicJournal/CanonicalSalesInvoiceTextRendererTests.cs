using System.Text;
using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;
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
            new(invoice.FiscalDocumentId, invoice.FiscalDocumentNumber!, invoice.BusinessDayDate!.Value, invoice.FiscalNumberAssignedAt!.Value, printed.Text)
        ]);

        Assert.Equal(printed.Bytes, journal.Bytes);
        Assert.Equal("text/plain; charset=utf-8", journal.ContentType);
        Assert.EndsWith(".txt", journal.FileName, StringComparison.Ordinal);
        Assert.Equal(printed.Text, Encoding.UTF8.GetString(journal.Bytes));
    }

    [Fact]
    public void RendererMatchesApprovedSalesInvoiceReceiptStructureAndLabels()
    {
        var text = new CanonicalSalesInvoiceTextRenderer().Render(Invoice()).Text;
        var requiredStructure = new[]
        {
            "ExitPass Parking Corporation", "VAT REG TIN", "MIN", "S/N", "Branch / Site", "Parking Location",
            "SALES INVOICE", "ORIGINAL", "SI No", "Issued Date", "PARKING DETAILS", "Ticket Number",
            "Plate Number", "Entry Time", "Payment", "Duration", "ITEMS", "Description", "Subtotal",
            "DISCOUNTS", "Discount Reason", "Discount Amount", "VAT BREAKDOWN", "VATable Sales", "VAT Amount",
            "VAT Exempt Sales", "Zero Rated Sales", "Total Amount", "PAYMENT DETAILS", "GCASH", "PayMongo", "Total Paid",
            "THIS SERVES AS YOUR SALES INVOICE", "Customer Information",
            "POS SOFTWARE SUPPLIER / DEVELOPER", "THANK YOU FOR CHOOSING OUR SERVICE", "NOTHING FOLLOWS"
        };

        var prior = -1;
        foreach (var value in requiredStructure)
        {
            var index = text.IndexOf(value, prior + 1, StringComparison.Ordinal);
            Assert.True(index > prior, $"Expected '{value}' after the prior canonical field.");
            prior = index;
        }
        Assert.True(Count(text, "------------------------------------------------") >= 6);
        Assert.DoesNotContain("Print Date", text, StringComparison.Ordinal);
        Assert.All(text.Split("\r\n", StringSplitOptions.RemoveEmptyEntries), line => Assert.True(line.Length <= 48, line));
    }

    [Fact]
    public void RequiredCustomerAndVatFieldsRemainOrderedAndZeroValuesStayVisible()
    {
        var text = new CanonicalSalesInvoiceTextRenderer().Render(Invoice()).Text;
        var required = new[]
        {
            "VATable Sales",
            "PHP 100.00",
            "VAT Amount",
            "PHP 12.00",
            "VAT Exempt Sales",
            "PHP 0.00",
            "Zero Rated Sales",
            "Customer Information",
            "NAME",
            "Juan Dela Cruz",
            "ADDRESS",
            "123 Sample Street",
            "TIN",
            "123-456-789-000",
            "BUS. STYLE",
            "Retail",
            "OSCA ID No.",
            "OSCA-0001",
            "Customer Sign : __________________________"
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
    public void OriginalAndGovernedReprintLabelsRemainDistinct()
    {
        var invoice = Invoice();
        var renderer = new SalesInvoicePrinterPayloadRenderer(new CanonicalSalesInvoiceTextRenderer());
        var original = renderer.RenderVisibleText(invoice).Text;
        var reprintRecord = new FiscalDocumentReprintRecord(
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000101"), "REPRINT-0001", invoice.FiscalDocumentId,
            invoice.FiscalDocumentNumber!, 1, Guid.Parse("aaaaaaaa-0000-4000-8000-000000000102"),
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000103"), "PHP",
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000104"), Guid.Parse("aaaaaaaa-0000-4000-8000-000000000105"),
            invoice.BusinessDayDate!.Value, 1, "fiscal_document_copy", "committed", "customer_request", "output-001",
            "print", true, invoice.FiscalNumberAssignedAt!.Value, invoice.FiscalNumberAssignedAt.Value, "operator", "pos-server", "correlation", "operation",
            "EJ-REPRINT-001");
        var reprint = renderer.RenderReprintVisibleText(invoice, reprintRecord).Text;

        Assert.Contains("ORIGINAL", original, StringComparison.Ordinal);
        Assert.DoesNotContain("REPRINT", original, StringComparison.Ordinal);
        Assert.Contains("REPRINT", reprint, StringComparison.Ordinal);
        Assert.DoesNotContain("ORIGINAL", reprint, StringComparison.Ordinal);
        Assert.Throws<InvalidOperationException>(() =>
            renderer.RenderReprintVisibleText(invoice, reprintRecord with { ReprintLabelApplied = false }));
    }

    [Fact]
    public void ChronologicalExportIsStableAndDoesNotDuplicateAReplayedInvoice()
    {
        var renderer = new ElectronicJournalInvoiceTextRenderer(new CanonicalSalesInvoiceTextRenderer());
        var later = Invoice() with
        {
            FiscalDocumentId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000002"),
            FiscalDocumentNumber = "SI-00000002",
            FiscalNumberAssignedAt = DateTimeOffset.Parse("2026-09-10T02:00:00Z")
        };
        var first = renderer.Render([later, Invoice(), Invoice()]);
        var second = renderer.Render([Invoice(), later]);

        Assert.Equal(first.Bytes, second.Bytes);
        Assert.True(first.Text.IndexOf("SI-00000001", StringComparison.Ordinal) < first.Text.IndexOf("SI-00000002", StringComparison.Ordinal));
        Assert.Equal(1, Count(first.Text, "SI-00000001"));
        Assert.Equal(2, CountLines(first.Text, "SALES INVOICE"));
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
            FiscalNumberAssignedAt = DateTimeOffset.Parse("2026-09-10T02:00:00Z")
        };
        var firstPrint = printer.RenderVisibleText(firstInvoice);
        var secondPrint = printer.RenderVisibleText(secondInvoice);
        var journal = new ElectronicJournalInvoiceTextRenderer(renderer).RenderPersisted([
            new(secondInvoice.FiscalDocumentId, secondInvoice.FiscalDocumentNumber!, secondInvoice.BusinessDayDate!.Value, secondInvoice.FiscalNumberAssignedAt!.Value, secondPrint.Text),
            new(firstInvoice.FiscalDocumentId, firstInvoice.FiscalDocumentNumber!, firstInvoice.BusinessDayDate!.Value, firstInvoice.FiscalNumberAssignedAt!.Value, firstPrint.Text),
            new(firstInvoice.FiscalDocumentId, firstInvoice.FiscalDocumentNumber!, firstInvoice.BusinessDayDate!.Value, firstInvoice.FiscalNumberAssignedAt!.Value, firstPrint.Text)
        ]);
        var evidenceDirectory = Path.Combine(AppContext.BaseDirectory, "g4-evidence");
        Directory.CreateDirectory(evidenceDirectory);
        var evidencePath = Path.Combine(evidenceDirectory, journal.FileName);
        File.WriteAllBytes(evidencePath, journal.Bytes);

        var strictUtf8 = new UTF8Encoding(false, true);
        var expected = Encoding.UTF8.GetBytes(firstPrint.Text + "\r\n" + secondPrint.Text);
        Assert.Equal(expected, File.ReadAllBytes(evidencePath));
        Assert.Equal(firstPrint.Text + "\r\n" + secondPrint.Text, strictUtf8.GetString(File.ReadAllBytes(evidencePath)));
        Assert.Equal(2, CountLines(journal.Text, "SALES INVOICE"));
        Assert.Equal(1, Count(journal.Text, "SI-00000001"));
        Assert.Equal(1, Count(journal.Text, "SI-00000002"));
        Assert.False(File.ReadAllBytes(evidencePath).AsSpan().StartsWith(Encoding.UTF8.Preamble));
    }

    [Fact]
    public async Task HistoricalCanonicalInvoiceWithoutPersistedTextFailsClosedWithoutReconstruction()
    {
        var invoice = Invoice();
        var append = new ElectronicJournalAppendRequest(
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000010"),
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000011"),
            "PHP",
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000012"),
            "fiscal_document_committed",
            "historical:invoice:1",
            "v1",
            invoice.FiscalNumberAssignedAt!.Value,
            "actor",
            "service",
            "correlation",
            new SortedDictionary<string, string?> { ["fiscal_document_number"] = invoice.FiscalDocumentNumber },
            invoice.FiscalDocumentId,
            BusinessDayDate: invoice.BusinessDayDate!.Value,
            PrintableSalesInvoiceText: null);
        var semantic = ElectronicJournalCanonicalizer.ComputeSemanticHash(
            append, ElectronicJournalContract.LegacySemanticHashVersion);
        var historical = new ElectronicJournalEvent(
            "EJ-HISTORICAL-1", append.EventType, ElectronicJournalContract.EventSchemaVersion,
            append.SitePosServerId, append.FiscalIdentityId, append.CurrencyCode, append.FiscalReportingPeriodId,
            append.FiscalDocumentId, null, null, null, null, append.BusinessDayDate, 1, append.EffectiveAt,
            append.EffectiveAt.AddSeconds(1), append.ActorReference, append.ServiceIdentityReference,
            append.CorrelationReference, append.SourceTransitionReference, append.SourceTransitionVersion,
            null, ElectronicJournalContract.LegacySemanticHashVersion, semantic, ElectronicJournalContract.IntegrityHashVersion,
            ElectronicJournalContract.GenesisHash, new string('a', 64), "fiscal_reconstruction_hold", append.Facts,
            PrintableSalesInvoiceText: null);
        var repository = new HistoricalRepository(historical);
        var service = new ElectronicJournalInvoiceTextService(
            repository, new ElectronicJournalInvoiceTextRenderer(new CanonicalSalesInvoiceTextRenderer()));

        var result = await service.ReadAsync(
            new ElectronicJournalQuery(append.SitePosServerId, append.FiscalIdentityId, append.CurrencyCode),
            createExport: true);

        Assert.Equal(ElectronicJournalOutcome.IntegrityFailure, result.Outcome);
        Assert.Null(result.Export);
        Assert.Contains("no immutable canonical printable text payload", result.SafeMessage, StringComparison.Ordinal);
        Assert.Equal("fiscal_document_committed", repository.LastQuery?.EventType);
    }

    private static DigitalSalesInvoiceRenderModel Invoice()
    {
        var issuedAt = DateTimeOffset.Parse("2026-09-10T01:00:00Z");
        var fiscalIdentityId = Guid.Parse("aaaaaaaa-0000-4000-8000-000000000103");
        var statutory = new AppliedStatutoryFiscalFactsSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SENIOR_CITIZEN", "STATUTORY_DISCOUNT", new("NATIONAL_LAW"), Guid.NewGuid(), Guid.NewGuid(),
            12000, 10000, 1200, "VATABLE", 800, 11200, "PHP", issuedAt, "WEBPAY");
        var header = new SalesInvoiceHeaderSnapshot(
            fiscalIdentityId, Guid.NewGuid(), "v1", "ExitPass Parking Corporation", "PITX, Paranaque City",
            "123-456-789-000", "POS-SN-001", "MIN-001", "PITX Parking Facility", "TERMINAL-01",
            "BIR-ACC-001", new DateOnly(2026, 1, 15), new DateOnly(2031, 1, 15), "PTU-001",
            new DateOnly(2026, 2, 10), "THIS SERVES AS YOUR SALES INVOICE",
            "Customer service: support@example.test", "digital-sales-invoice-json-v1",
            "digital-sales-invoice-presentation-json-v1", issuedAt, issuedAt, "ExitPass Software Inc.",
            "Supplier Address, Philippines", "987-654-321-000");

        return new DigitalSalesInvoiceRenderModel(
            Guid.Parse("aaaaaaaa-0000-4000-8000-000000000001"), Guid.NewGuid(), Guid.NewGuid(), fiscalIdentityId,
            Guid.NewGuid(), "sales_invoice", Guid.NewGuid(), "issued", "assigned", Guid.NewGuid(), 1,
            "SI-00000001", "PITX-SI", "SI-", null, issuedAt, "pos-server", new DateOnly(2026, 9, 10),
            "PARKING-SESSION-001", "PAYMENT-ATTEMPT-001", "PAYMENT-CONFIRMATION-001", "FINALITY-001", null,
            null, null, null, "not_voided", null, null, issuedAt, issuedAt,
            [new(1, Guid.NewGuid(), null, "Parking fee - regular parking", 1m, 11200, 12000, 800, 0, 11200, "PHP", null)],
            [new(null, Guid.NewGuid(), 10000, 800, 1200, "PHP", "OSCA-0001", null, null,
                "statutory_discount_and_vat_privilege", "Senior Discount")],
            [new(null, Guid.NewGuid(), Guid.NewGuid(), 12m, 10000, 1200, "PHP", "vatable")],
            [new(Guid.NewGuid(), "gcash", 11200, "PHP", null, null, "FINALITY-001", "PayMongo")],
            [new(Guid.NewGuid(), 11200, "PHP", "payable_total")],
            new("canonical", [], ["THANK YOU FOR CHOOSING OUR SERVICE", "===== NOTHING FOLLOWS ====="]),
            header, statutory, InvoiceCustomerInformation: new("Juan Dela Cruz", "123 Sample Street",
                "123-456-789-000", "Retail", "OSCA-0001"),
            FiscalContent: new("PITX", "TICKET-0001", "ABC-1234", "2026-09-10 00:00:00 UTC",
                "2026-09-10 01:00:00 UTC", "01:00:00", "GCASH", 12000, 800, 10000, 1200, 0, 0,
                11200, 11200, 12000, 800, "PHP", true));
    }

    private static int Count(string text, string value) =>
        (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    private static int CountLines(string text, string value) =>
        text.Split("\r\n", StringSplitOptions.None).Count(line => line.Trim().Equals(value, StringComparison.Ordinal));

    private sealed class HistoricalRepository(ElectronicJournalEvent historical) : IElectronicJournalRepository
    {
        public ElectronicJournalQuery? LastQuery { get; private set; }

        public Task<ElectronicJournalPageResult> ReadAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default)
        {
            LastQuery = query;
            return Task.FromResult(new ElectronicJournalPageResult(
                ElectronicJournalOutcome.Success,
                new ElectronicJournalPage([historical], null, 1, ElectronicJournalContract.ChronologyVersion)));
        }

        public Task<ElectronicJournalIntegrityOutcome> VerifyIntegrityAsync(ElectronicJournalQuery query, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RecordAccessAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, string action,
            string result, string actorReference, string serviceIdentityReference, string correlationReference,
            string supportReference, int eventCount, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
