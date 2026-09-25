using ExitPass.PosServer.Runtime.ElectronicJournal;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.ElectronicJournal;

public sealed class CanonicalSalesInvoiceConsistencyTests
{
    [Fact]
    public void NormalPaidSalesInvoiceUsesTheSameCanonicalValuesForDigitalSiAndEj()
    {
        var canonical = Invoice(statutory: false);
        var presentation = Present(canonical);
        var ej = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        AssertParity(canonical, presentation, ej);
        Assert.Equal("CARD", canonical.FiscalContent!.PaymentMethod);
        Assert.Contains("CARD", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("OSCA ID No.", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("PWD ID No.", ej, StringComparison.Ordinal);
        Assert.Null(Row(presentation, "customerInformation.statutoryIdNumber").RawValue);
    }

    [Fact]
    public void ZeroPayableSeniorSalesInvoicePreservesEveryApplicableConditionalFiscalValue()
    {
        var canonical = Invoice(statutory: true);
        var presentation = Present(canonical);
        var ej = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        AssertParity(canonical, presentation, ej);
        Assert.Equal(0, canonical.FiscalContent!.TotalAmountMinorUnits);
        Assert.Equal("OSCA-1234567", canonical.InvoiceCustomerInformation!.StatutoryIdNumber);
        Assert.Equal("OSCA-1234567", Row(presentation, "customerInformation.statutoryIdNumber").DisplayValue);
        Assert.Contains("OSCA ID No.", ej, StringComparison.Ordinal);
        Assert.Contains("OSCA-1234567", ej, StringComparison.Ordinal);
        Assert.Equal("Senior Citizen Discount", canonical.Discounts.Single().Reason);
        Assert.Equal("Senior Citizen Discount", Row(presentation, "discounts[0000].reason").DisplayValue);
        Assert.Contains("Senior Citizen Discount", ej, StringComparison.Ordinal);
        Assert.Contains("ZERO_PAYABLE_STATUTORY_FINALITY", ej, StringComparison.Ordinal);
        Assert.Contains("PHP 0.00", ej, StringComparison.Ordinal);
    }

    [Fact]
    public void NonStatutorySalesInvoiceCannotFabricateAStatutoryIdInDigitalSiOrEj()
    {
        var canonical = Invoice(statutory: false) with
        {
            InvoiceCustomerInformation = new InvoiceCustomerInformationSnapshot(
                "Ordinary Customer", null, null, null, "must-not-be-exposed")
        };
        var presentation = Present(canonical);
        var ej = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        Assert.Null(Row(presentation, "customerInformation.statutoryIdNumber").RawValue);
        Assert.DoesNotContain("must-not-be-exposed", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("OSCA ID No.", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("PWD ID No.", ej, StringComparison.Ordinal);
    }

    private static void AssertParity(
        DigitalSalesInvoiceRenderModel canonical,
        DigitalSalesInvoicePresentationModel presentation,
        string ej)
    {
        var fiscal = canonical.FiscalContent!;
        Assert.Equal(canonical.FiscalDocumentNumber, Row(presentation, "fiscalNumbering.fiscalDocumentNumber").DisplayValue);
        Assert.Contains(canonical.FiscalDocumentNumber!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.TicketNumber, Row(presentation, "parkingPaymentReferences.ticketNumber").DisplayValue);
        Assert.Contains(fiscal.TicketNumber!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.PlateNumber, Row(presentation, "parkingPaymentReferences.plateNumber").DisplayValue);
        Assert.Contains(fiscal.PlateNumber!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.EntryTimeText, Row(presentation, "parkingPaymentReferences.entryTime").DisplayValue);
        Assert.Contains(fiscal.EntryTimeText!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.ParkingDurationText, Row(presentation, "parkingPaymentReferences.parkingDuration").DisplayValue);
        Assert.Contains(fiscal.ParkingDurationText!, ej, StringComparison.Ordinal);
        Assert.Equal(canonical.CompletionBasis, Row(presentation, "parkingPaymentReferences.completionBasis").DisplayValue);
        Assert.Contains(canonical.CompletionBasis, ej, StringComparison.Ordinal);
        Assert.Equal(canonical.CompletionAuthorityRef, Row(presentation, "parkingPaymentReferences.completionAuthorityRef").DisplayValue);
        Assert.Contains(canonical.CompletionAuthorityRef!, ej, StringComparison.Ordinal);
        Assert.Equal(canonical.Lines.Single().Description, Row(presentation, "lineItems[0000].description").DisplayValue);
        Assert.Equal(canonical.Lines.Single().Quantity, Row(presentation, "lineItems[0000].quantity").RawValue);
        Assert.Equal(canonical.Lines.Single().UnitAmountMinorUnits, Row(presentation, "lineItems[0000].unitAmount").RawValue);
        Assert.Equal(fiscal.VatableSalesMinorUnits, Row(presentation, "totals.vatableSales").RawValue);
        Assert.Equal(fiscal.VatAmountMinorUnits, Row(presentation, "totals.vatAmount").RawValue);
        Assert.Equal(fiscal.VatExemptSalesMinorUnits, Row(presentation, "totals.vatExemptSales").RawValue);
        Assert.Equal(fiscal.SubtotalAmountMinorUnits, Row(presentation, "totals.summary.subtotal").RawValue);
        Assert.Equal(fiscal.DiscountAmountMinorUnits, Row(presentation, "totals.summary.discountAmount").RawValue);
        Assert.Equal(fiscal.TotalAmountMinorUnits, Row(presentation, "totals.summary.totalAmount").RawValue);
        Assert.Equal(fiscal.TotalPaidMinorUnits, Row(presentation, "totals.summary.totalPaid").RawValue);
        Assert.Contains(Money(fiscal.VatableSalesMinorUnits), ej, StringComparison.Ordinal);
        Assert.Contains(Money(fiscal.VatAmountMinorUnits), ej, StringComparison.Ordinal);
        Assert.Contains($"Total Amount{new string(' ', 36 - Money(fiscal.TotalAmountMinorUnits).Length)}{Money(fiscal.TotalAmountMinorUnits)}", ej, StringComparison.Ordinal);
        Assert.Contains(canonical.Lines.Single().Description, ej, StringComparison.Ordinal);
        var customer = canonical.InvoiceCustomerInformation!;
        Assert.Equal(customer.CustomerName, Row(presentation, "customerInformation.customerName").DisplayValue);
        Assert.Equal(customer.Address, Row(presentation, "customerInformation.address").DisplayValue);
        Assert.Equal(customer.Tin, Row(presentation, "customerInformation.tin").DisplayValue);
        Assert.Equal(customer.BusinessStyle, Row(presentation, "customerInformation.businessStyle").DisplayValue);
        Assert.Contains(customer.CustomerName!, ej, StringComparison.Ordinal);
        Assert.Contains(customer.Address!, ej, StringComparison.Ordinal);
        var header = canonical.SalesInvoiceHeaderSnapshot!;
        Assert.Equal(header.SupplierDeveloperRegisteredName, Row(presentation, "salesInvoiceHeaderSnapshot.supplierDeveloperRegisteredName").DisplayValue);
        Assert.Equal(header.SupplierDeveloperAddress, Row(presentation, "salesInvoiceHeaderSnapshot.supplierDeveloperAddress").DisplayValue);
        Assert.Equal(header.SupplierDeveloperTin, Row(presentation, "salesInvoiceHeaderSnapshot.supplierDeveloperTin").DisplayValue);
        Assert.Equal(header.BirAccreditationNumber, Row(presentation, "salesInvoiceHeaderSnapshot.birAccreditationNumber").DisplayValue);
        Assert.Equal(header.PtuNumber, Row(presentation, "salesInvoiceHeaderSnapshot.ptuNumber").DisplayValue);
        Assert.Contains(header.SupplierDeveloperRegisteredName, ej, StringComparison.Ordinal);
        Assert.Contains(header.BirAccreditationNumber, ej, StringComparison.Ordinal);
        Assert.Contains(header.PtuNumber, ej, StringComparison.Ordinal);
        Assert.Equal(canonical.Footer.ClosingTextLines![0], Row(presentation, "footerDisclaimers.closingText[0000]").DisplayValue);
        Assert.Contains(canonical.Footer.ClosingTextLines[0], ej, StringComparison.Ordinal);
    }

    private static DigitalSalesInvoicePresentationModel Present(DigitalSalesInvoiceRenderModel canonical)
    {
        var result = new DigitalSalesInvoicePresentationAdapter().Adapt(
            DigitalSalesInvoiceRenderResult.Success(canonical),
            DigitalSalesInvoiceTemplateContract.Create());
        return Assert.IsType<DigitalSalesInvoicePresentationModel>(result.Presentation);
    }

    private static DigitalSalesInvoicePresentationRowModel Row(
        DigitalSalesInvoicePresentationModel presentation,
        string key) =>
        presentation.Sections.SelectMany(section => section.Rows).Single(row => row.Key == key);

    private static string Money(long minorUnits) => $"PHP {minorUnits / 100m:N2}";

    private static DigitalSalesInvoiceRenderModel Invoice(bool statutory)
    {
        var issuedAt = DateTimeOffset.Parse("2026-09-25T01:30:00Z");
        var fiscalIdentityId = Guid.Parse("10000000-0000-4000-8000-000000000001");
        var header = new SalesInvoiceHeaderSnapshot(
            fiscalIdentityId, Guid.Parse("10000000-0000-4000-8000-000000000002"), "v1",
            "ExitPass Parking Corporation", "PITX, Paranaque City", "123-456-789-000", "POS-SN-001",
            "MIN-001", "PITX Parking Facility", "TERMINAL-01", "BIR-ACC-001",
            new DateOnly(2026, 1, 15), new DateOnly(2031, 1, 15), "PTU-001", new DateOnly(2026, 2, 10),
            "THIS SERVES AS YOUR SALES INVOICE", "Customer service: support@example.test",
            "digital-sales-invoice-json-v1", "digital-sales-invoice-presentation-json-v1", issuedAt, issuedAt,
            "ExitPass Software Inc.", "Supplier Address, Philippines", "987-654-321-000");
        var statutoryFacts = statutory
            ? new AppliedStatutoryFiscalFactsSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                "SENIOR_CITIZEN", "FREE_PARKING", new("NATIONAL_LAW", NationalLawReference: "RA 9994"),
                Guid.NewGuid(), Guid.NewGuid(), 3000, 2679, 321, "VAT_EXCLUSIVE", 2679, 0, "PHP", issuedAt,
                "WEBPAY")
            : null;
        var total = statutory ? 0 : 11200;
        var discounts = statutory
            ? new[] { new DigitalSalesInvoiceDiscountRenderModel(null, Guid.NewGuid(), 2679, 2679, 321, "PHP",
                "OSCA-1234567", null, null, "statutory_discount_and_vat_privilege", "Senior Citizen Discount") }
            : [];
        var tenders = statutory
            ? []
            : new[] { new DigitalSalesInvoiceTenderRenderModel(Guid.NewGuid(), "card", 11200, "PHP", "ATTEMPT-49",
                "CONFIRMATION-49", "FINALITY-49", "PayMongo") };
        var completionBasis = statutory ? FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality : FiscalCompletionBasisCodes.PaymentFinality;

        var incomplete = new DigitalSalesInvoiceRenderModel(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), fiscalIdentityId, Guid.NewGuid(), "sales_invoice",
            Guid.NewGuid(), "issued", "assigned", Guid.NewGuid(), statutory ? 44 : 49,
            statutory ? "SI-00000044" : "SI-00000049", "PITX-SI", "SI-", null, issuedAt, "pos-server",
            new DateOnly(2026, 9, 25), "PARKING-SESSION", statutory ? null : "ATTEMPT-49",
            statutory ? null : "CONFIRMATION-49", statutory ? null : "FINALITY-49", null,
            null, null, null, "not_voided", null, null, issuedAt, issuedAt,
            [new(1, Guid.NewGuid(), null, "Parking fee", 1m, statutory ? 2679 : 11200,
                statutory ? 2679 : 11200, statutory ? 2679 : 0, 0, total, "PHP", null)],
            discounts,
            [new(null, Guid.NewGuid(), Guid.NewGuid(), 12m, statutory ? 2679 : 10000,
                statutory ? 321 : 1200, "PHP", "vatable")],
            tenders,
            [new(Guid.NewGuid(), total, "PHP", "payable_total")],
            new("canonical", [], ["THANK YOU FOR CHOOSING OUR SERVICE", "===== NOTHING FOLLOWS ====="]),
            header, statutoryFacts, completionBasis, statutory ? "STATUTORY-COMPLETION-44" : "FINALITY-49",
            new("Maria Santos", "100 Sample Street", "123-456-789", "Sample Trading",
                statutory ? "OSCA-1234567" : null),
            "{\"reference_context\":{\"branch_site\":\"PITX\",\"ticket_number\":\"TICKET-001\",\"plate_number\":\"ABC-1234\",\"entry_time\":\"2026-09-25 08:00 PHT\",\"payment_time\":\"2026-09-25 09:30 PHT\",\"parking_duration\":\"01:30:00\",\"payment_method\":\"CARD\"}}");

        return DigitalSalesInvoiceRenderModelFactory.Complete(incomplete);
    }
}
