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
        var renderer = new CanonicalSalesInvoiceTextRenderer();
        var printed = new SalesInvoicePrinterPayloadRenderer(renderer).RenderVisibleText(canonical);
        var ej = new ElectronicJournalInvoiceTextRenderer(renderer).RenderPersisted([
            new(canonical.FiscalDocumentId, canonical.FiscalDocumentNumber!, canonical.BusinessDayDate!.Value,
                canonical.FiscalNumberAssignedAt!.Value, printed.Text)
        ]).Text;

        AssertParity(canonical, presentation, ej);
        Assert.Equal(0, canonical.FiscalContent!.TotalAmountMinorUnits);
        Assert.Equal(5000, canonical.Lines.Single().UnitAmountMinorUnits);
        Assert.Equal(5000, canonical.Lines.Single().GrossAmountMinorUnits);
        Assert.Equal(0, canonical.Lines.Single().NetAmountMinorUnits);
        Assert.Equal(5000, canonical.FiscalContent.SubtotalAmountMinorUnits);
        Assert.Equal(4464, canonical.FiscalContent.DiscountAmountMinorUnits);
        Assert.Equal(4464, canonical.FiscalContent.VatableSalesMinorUnits);
        Assert.Equal(536, canonical.FiscalContent.VatAmountMinorUnits);
        Assert.Equal(5000L, Row(presentation, "lineItems[0000].unitAmount").RawValue);
        Assert.Equal(5000L, Row(presentation, "lineItems[0000].grossAmount").RawValue);
        Assert.Equal(5000L, Row(presentation, "totals.summary.subtotal").RawValue);
        Assert.Equal(4464L, Row(presentation, "totals.summary.discountAmount").RawValue);
        Assert.Equal(4464L, Row(presentation, "totals.vatableSales").RawValue);
        Assert.Equal(536L, Row(presentation, "totals.vatAmount").RawValue);
        Assert.Contains(ReceiptRow("1 x PHP 50.00", "50.00"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("SUBTOTAL", "50.00"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("DISCOUNT", "44.64"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("VATABLE SALES", "44.64"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("VAT AMOUNT", "5.36"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("VAT EXEMPT SALES", "0.00"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("ZERO RATED SALES", "0.00"), ej, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("TOTAL", "0.00"), ej, StringComparison.Ordinal);
        Assert.Equal(printed.Text, ej);
        Assert.Equal("OSCA-1234567", canonical.InvoiceCustomerInformation!.StatutoryIdNumber);
        Assert.Equal("OSCA-1234567", Row(presentation, "customerInformation.statutoryIdNumber").DisplayValue);
        Assert.Contains("OSCA ID No.", ej, StringComparison.Ordinal);
        Assert.Contains("OSCA-1234567", ej, StringComparison.Ordinal);
        Assert.Equal("Senior Citizen Discount", canonical.Discounts.Single().Reason);
        Assert.Equal("Senior Citizen Discount", Row(presentation, "discounts[0000].reason").DisplayValue);
        Assert.Contains("DISCOUNT", ej, StringComparison.Ordinal);
        Assert.Equal(FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality, canonical.CompletionBasis);
        Assert.DoesNotContain("ZERO_PAYABLE_STATUTORY_FINALITY", ej, StringComparison.Ordinal);
        Assert.Contains("TOTAL", ej, StringComparison.Ordinal);
        Assert.Contains("0.00", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("TENDERED", ej, StringComparison.Ordinal);
        Assert.DoesNotContain("CHANGE", ej, StringComparison.Ordinal);
        Assert.DoesNotContain(ej.Split("\r\n"), line =>
            line.StartsWith("TYPE", StringComparison.Ordinal) &&
            line.EndsWith("CASH", StringComparison.Ordinal));
    }

    [Fact]
    public void OrdinaryPaidSalesInvoiceKeepsItsExistingNetLinePresentation()
    {
        var canonical = Invoice(statutory: false);
        var text = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        Assert.Contains(ReceiptRow("1 x PHP 112.00", "112.00"), text, StringComparison.Ordinal);
        Assert.Equal(11200, canonical.FiscalContent!.SubtotalAmountMinorUnits);
        Assert.Equal(10000, canonical.FiscalContent.VatableSalesMinorUnits);
        Assert.Equal(1200, canonical.FiscalContent.VatAmountMinorUnits);
        Assert.Equal(11200, canonical.FiscalContent.TotalAmountMinorUnits);
    }

    [Fact]
    public void PercentageStatutorySalesInvoiceKeepsItsExistingNetLinePresentation()
    {
        var canonical = PercentageInvoice();
        var text = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        Assert.Contains(ReceiptRow("1 x PHP 112.00", "89.60"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("SUBTOTAL", "112.00"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("DISCOUNT", "20.00"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("VATABLE SALES", "80.00"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("VAT AMOUNT", "9.60"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("TOTAL", "89.60"), text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SENIOR_CITIZEN")]
    [InlineData("PWD")]
    public void PitxFullFeePresentationSupportsBothSettledEntitlements(string entitlementType)
    {
        var canonical = Invoice(statutory: true, entitlementType);
        var text = new CanonicalSalesInvoiceTextRenderer().Render(canonical).Text;

        Assert.Equal(entitlementType, canonical.AppliedStatutoryFiscalFacts!.EntitlementType);
        Assert.Contains(ReceiptRow("1 x PHP 50.00", "50.00"), text, StringComparison.Ordinal);
        Assert.Contains(ReceiptRow("TOTAL", "0.00"), text, StringComparison.Ordinal);
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
        Assert.Contains("2026-09-25 08:00", ej, StringComparison.Ordinal);
        Assert.DoesNotContain(fiscal.EntryTimeText!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.PaymentTimeText, Row(presentation, "parkingPaymentReferences.paymentTime").DisplayValue);
        Assert.Contains("2026-09-25 09:30", ej, StringComparison.Ordinal);
        Assert.DoesNotContain(fiscal.PaymentTimeText!, ej, StringComparison.Ordinal);
        Assert.Equal(fiscal.ParkingDurationText, Row(presentation, "parkingPaymentReferences.parkingDuration").DisplayValue);
        Assert.Contains(fiscal.ParkingDurationText!, ej, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(canonical.CompletionBasis));
        Assert.False(string.IsNullOrWhiteSpace(canonical.CompletionAuthorityRef));
        Assert.DoesNotContain(presentation.Sections.SelectMany(section => section.Rows), row =>
            row.Key.Contains("completion", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain("Completion Basis", ej, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Completion Authority", ej, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(canonical.CompletionAuthorityRef!, ej, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains(Amount(fiscal.VatableSalesMinorUnits), ej, StringComparison.Ordinal);
        Assert.Contains(Amount(fiscal.VatAmountMinorUnits), ej, StringComparison.Ordinal);
        Assert.Contains("TOTAL", ej, StringComparison.Ordinal);
        Assert.Contains(Amount(fiscal.TotalAmountMinorUnits), ej, StringComparison.Ordinal);
        Assert.Contains("PARKING FEE", ej, StringComparison.Ordinal);
        var customer = canonical.InvoiceCustomerInformation!;
        Assert.Equal(customer.CustomerName, Row(presentation, "customerInformation.customerName").DisplayValue);
        Assert.Equal(customer.Address, Row(presentation, "customerInformation.address").DisplayValue);
        Assert.Equal(customer.Tin, Row(presentation, "customerInformation.tin").DisplayValue);
        Assert.Equal(customer.BusinessStyle, Row(presentation, "customerInformation.businessStyle").DisplayValue);
        Assert.Contains(customer.CustomerName!, ej, StringComparison.Ordinal);
        Assert.Contains(customer.Address!, ej, StringComparison.Ordinal);
        var header = canonical.SalesInvoiceHeaderSnapshot!;
        Assert.Equal(header.TerminalId, Row(presentation, "salesInvoiceHeaderSnapshot.terminalId").DisplayValue);
        Assert.Equal(canonical.ChannelTerminalId, Row(presentation, "sellerSitePosIdentity.channelTerminalId").RawValue);
        Assert.Contains(header.TerminalId!, ej, StringComparison.Ordinal);
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

    private static string Amount(long minorUnits) => $"{minorUnits / 100m:N2}";

    private static string ReceiptRow(string left, string right) =>
        left + new string(' ', 35 - left.Length - right.Length) + right;

    private static DigitalSalesInvoiceRenderModel PercentageInvoice()
    {
        var invoice = Invoice(statutory: false);
        var issuedAt = invoice.FiscalNumberAssignedAt!.Value;
        var facts = new AppliedStatutoryFiscalFactsSnapshot(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SENIOR_CITIZEN", "STATUTORY_DISCOUNT", new("LOCAL_ORDINANCE", PolicyCode: "TEST_PERCENTAGE_POLICY"),
            Guid.NewGuid(), Guid.NewGuid(), 11200, 10000, 960, "VAT_INCLUSIVE_NO_EXEMPTION", 2000, 8960,
            "PHP", issuedAt, "WEBPAY", StatutoryDiscountRateBasisPoints: 2000,
            PercentageDiscountAmountMinorUnits: 2000, PercentageComponentSeparatelyProven: true);

        return DigitalSalesInvoiceRenderModelFactory.Complete(invoice with
        {
            Lines = [invoice.Lines.Single() with
            {
                UnitAmountMinorUnits = 11200,
                GrossAmountMinorUnits = 11200,
                DiscountAmountMinorUnits = 2000,
                TaxAmountMinorUnits = 960,
                NetAmountMinorUnits = 8960
            }],
            Discounts = [invoice.Discounts.FirstOrDefault() is { } discount
                ? discount with { BasisAmountMinorUnits = 10000, DiscountAmountMinorUnits = 2000, VatPrivilegeAmountMinorUnits = 0 }
                : new(null, Guid.NewGuid(), 10000, 2000, 0, "PHP", null, null, null)],
            TaxDetails = [new(null, Guid.NewGuid(), Guid.NewGuid(), 12m, 8000, 960, "PHP", "vatable")],
            Tenders = [invoice.Tenders.Single() with { AmountMinorUnits = 8960 }],
            Totals = [invoice.Totals.Single() with { AmountMinorUnits = 8960 }],
            AppliedStatutoryFiscalFacts = facts,
            FiscalContent = null
        });
    }

    private static DigitalSalesInvoiceRenderModel Invoice(bool statutory, string entitlementType = "SENIOR_CITIZEN")
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
            "ExitPass Software Inc.", "Supplier Address, Philippines", "987-654-321-000", "APT-HARDWARE-001");
        var statutoryFacts = statutory
            ? new AppliedStatutoryFiscalFactsSnapshot(
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
                entitlementType, "FREE_PARKING", new("LOCAL_ORDINANCE", PolicyCode: entitlementType == "PWD"
                        ? "PH_PARANAQUE_PWD_FREE_PARKING"
                        : "PH_PARANAQUE_SENIOR_FREE_PARKING",
                    OrdinanceReference: "City Ordinance No. 48"),
                Guid.NewGuid(), Guid.NewGuid(), 5000, 4464, 0, "VAT_INCLUSIVE_NO_EXEMPTION", 4464, 0, "PHP", issuedAt,
                "WEBPAY", NonPercentageBenefitAmountMinorUnits: 4464)
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
            [new(1, Guid.NewGuid(), null, "Parking fee", 1m, statutory ? 4464 : 11200,
                statutory ? 4464 : 11200, statutory ? 4464 : 0, 0, total, "PHP", null)],
            discounts,
            [new(null, Guid.NewGuid(), Guid.NewGuid(), 12m, statutory ? 0 : 10000,
                statutory ? 0 : 1200, "PHP", "vatable")],
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
