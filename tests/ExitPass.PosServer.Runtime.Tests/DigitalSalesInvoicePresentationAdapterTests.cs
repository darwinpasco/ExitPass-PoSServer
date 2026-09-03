using System.Reflection;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class DigitalSalesInvoicePresentationAdapterTests
{
    [Fact]
    public void PresentsAssignedNumberedFiscalDocument()
    {
        var render = ValidRenderModel(assignedNumber: true);
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        Assert.Equal("digital-sales-invoice-presentation-json-v1", result.Presentation.PresentationVersion);
        Assert.Equal("digital-sales-invoice-json-v1", result.Presentation.SourceTemplateContractVersion);
        Assert.Equal("PH_DIGITAL_SALES_INVOICE", result.Presentation.FiscalTemplateFamily);
        Assert.Equal("application/json", result.Presentation.RenderFormat);
        Assert.Equal("assigned", result.Presentation.NumberingState);
        Assert.Equal("Digital Sales Invoice", result.Presentation.DocumentTitle);
        Assert.Empty(result.Presentation.Notices);

        var fiscalNumbering = Section(result.Presentation, "fiscalNumbering");
        Assert.Contains(fiscalNumbering.Rows, row =>
            row.Key == "fiscalNumbering.fiscalDocumentNumber" &&
            row.DisplayValue == "SI-00000001-A" &&
            row.Posture == "optional");
        Assert.Contains(Section(result.Presentation, "documentIdentity").Rows, row =>
            row.Key == "documentIdentity.fiscalDocumentStatusCodeKey" &&
            row.DisplayValue == "recorded");
        Assert.Contains(Section(result.Presentation, "tenders").Rows, row =>
            row.Key == "tenders[0000].tenderTypeCodeKey" &&
            row.DisplayValue == "cash");
    }

    [Fact]
    public void PresentsNotAssignedFiscalNumberWithWarningAndUnavailableNumber()
    {
        var render = ValidRenderModel(assignedNumber: false);
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        Assert.Equal("not_assigned", result.Presentation.NumberingState);
        Assert.Contains(result.Presentation.Notices, notice =>
            notice.Code == "fiscal_number_not_assigned" &&
            notice.Severity == "warning");

        var fiscalNumbering = Section(result.Presentation, "fiscalNumbering");
        Assert.Contains(fiscalNumbering.Rows, row =>
            row.Key == "fiscalNumbering.fiscalDocumentNumber" &&
            row.Posture == "not_available" &&
            row.RawValue is null);
    }

    [Fact]
    public void RequiredSectionsAppearInDeterministicOrder()
    {
        var render = ValidRenderModel(assignedNumber: true);
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        Assert.Equal(
            [
                "header",
                "salesInvoiceHeaderSnapshot",
                "sellerSitePosIdentity",
                "documentIdentity",
                "fiscalNumbering",
                "parkingPaymentReferences",
                "lineItems",
                "discounts",
                "taxes",
                "tenders",
                "totals",
                "auditHashStatus",
                "footerDisclaimers",
                "deferredPlaceholders"
            ],
            result.Presentation.Sections.Select(section => section.Name).ToArray());
    }

    [Fact]
    public void RepeatingRowsAppearInDeterministicOrder()
    {
        var render = ValidRenderModel(assignedNumber: true) with
        {
            Lines =
            [
                Line(2, "Second line", 5000),
                Line(1, "First line", 7500)
            ],
            Tenders =
            [
                Tender("payment-finality-b", "provider-b", 5000),
                Tender("payment-finality-a", "provider-a", 7500)
            ],
            TaxDetails =
            [
                TaxDetail(Guid.Parse("20000000-0000-0000-0000-000000000001")),
                TaxDetail(Guid.Parse("10000000-0000-0000-0000-000000000001"))
            ],
            Totals =
            [
                Total(Guid.Parse("90000000-0000-0000-0000-000000000001"), 5000),
                Total(Guid.Parse("10000000-0000-0000-0000-000000000001"), 7500)
            ]
        };
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        Assert.Equal(
            ["First line", "Second line"],
            Section(result.Presentation, "lineItems")
                .Rows
                .Where(row => row.Key.EndsWith(".description", StringComparison.Ordinal))
                .Select(row => row.DisplayValue ?? string.Empty)
                .ToArray());
        Assert.Equal(
            ["provider-a", "provider-b"],
            Section(result.Presentation, "tenders")
                .Rows
                .Where(row => row.Key.EndsWith(".providerRef", StringComparison.Ordinal))
                .Select(row => row.DisplayValue ?? string.Empty)
                .ToArray());
        Assert.Equal(
            [
                "10000000-0000-0000-0000-000000000001",
                "20000000-0000-0000-0000-000000000001"
            ],
            Section(result.Presentation, "taxes")
                .Rows
                .Where(row => row.Key.EndsWith(".taxTypeCodeId", StringComparison.Ordinal))
                .Select(row => row.DisplayValue ?? string.Empty)
                .ToArray());
        Assert.Equal(
            [
                "10000000-0000-0000-0000-000000000001",
                "90000000-0000-0000-0000-000000000001"
            ],
            Section(result.Presentation, "totals")
                .Rows
                .Where(row => row.Key.EndsWith(".totalTypeCodeId", StringComparison.Ordinal))
                .Select(row => row.DisplayValue ?? string.Empty)
                .ToArray());
    }

    [Fact]
    public void PlaceholderAndDeferredFieldsAreMarkedSafely()
    {
        var render = ValidRenderModel(assignedNumber: true);
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        Assert.All(Section(result.Presentation, "footerDisclaimers").Rows, row => Assert.Equal("placeholder", row.Posture));
        Assert.All(Section(result.Presentation, "deferredPlaceholders").Rows, row =>
        {
            Assert.Equal("deferred", row.Posture);
            Assert.Equal("deferred", row.ValueKind);
        });
    }

    [Fact]
    public void PresentationUsesImmutableSalesInvoiceHeaderSnapshotWhenPresent()
    {
        var snapshot = new SalesInvoiceHeaderSnapshot(
            Guid.Parse("dddddddd-0000-0000-0000-000000000001"),
            Guid.Parse("cccccccc-0000-0000-0000-000000000001"),
            "v1",
            "GOVERNED TEST BUSINESS NAME",
            "GOVERNED TEST ADDRESS",
            "TEST-TIN-0001",
            "TEST-SERIAL-0001",
            "TEST-MIN-0001",
            "GOVERNED TEST PARKING LOCATION",
            "RUNTIME-TERMINAL-001",
            "TEST-BIR-ACCREDITATION-0001",
            new DateOnly(2026, 1, 15),
            new DateOnly(2027, 1, 15),
            "TEST-PTU-0001",
            new DateOnly(2026, 2, 10),
            "THIS SERVES AS YOUR SALES INVOICE",
            "CUSTOMER SERVICE TEST FOOTER",
            SalesInvoiceHeaderProfileVersions.TemplateVersion,
            SalesInvoiceHeaderProfileVersions.PresentationVersion,
            DateTimeOffset.Parse("2026-07-18T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-18T08:00:01Z"),
            "GOVERNED TEST SOFTWARE SUPPLIER",
            "GOVERNED TEST SOFTWARE ADDRESS",
            "TEST-SUPPLIER-TIN-0001");
        var render = ValidRenderModel(assignedNumber: true) with { SalesInvoiceHeaderSnapshot = snapshot };
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Presentation);
        var rows = Section(result.Presentation, "salesInvoiceHeaderSnapshot").Rows;
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.registeredBusinessName" && row.DisplayValue == "GOVERNED TEST BUSINESS NAME");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.registeredBusinessAddress" && row.DisplayValue == "GOVERNED TEST ADDRESS");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.supplierDeveloperRegisteredName" && row.DisplayValue == "GOVERNED TEST SOFTWARE SUPPLIER");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.supplierDeveloperAddress" && row.DisplayValue == "GOVERNED TEST SOFTWARE ADDRESS");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.supplierDeveloperTin" && row.DisplayValue == "TEST-SUPPLIER-TIN-0001");
        Assert.DoesNotContain(rows, row =>
            row.Key.StartsWith("salesInvoiceHeaderSnapshot.supplierDeveloper", StringComparison.Ordinal) &&
            row.Posture == "not_available");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.birAccreditationIssuedDate" && row.DisplayValue == "2026-01-15");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.birAccreditationValidUntil" && row.DisplayValue == "2027-01-15");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.ptuIssuedDate" && row.DisplayValue == "2026-02-10");
        Assert.Contains(rows, row => row.Key == "salesInvoiceHeaderSnapshot.terminalId" && row.DisplayValue == "RUNTIME-TERMINAL-001");
    }

    [Fact]
    public void AdapterDoesNotMutateRenderFactsOrAllocateNumbering()
    {
        var render = ValidRenderModel(assignedNumber: false);
        var originalUpdatedAt = render.UpdatedAt;
        var originalLineCount = render.Lines.Count;
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var result = adapter.Adapt(DigitalSalesInvoiceRenderResult.Success(render), DigitalSalesInvoiceTemplateContract.Create());

        Assert.True(result.Succeeded);
        Assert.Equal(originalUpdatedAt, render.UpdatedAt);
        Assert.Equal(originalLineCount, render.Lines.Count);
        Assert.Null(render.FiscalDocumentNumber);
        Assert.Null(render.FiscalSequenceValue);
        Assert.Null(render.FiscalNumberAssignedAt);
    }

    [Fact]
    public void PresentationModelDoesNotExposeGeneratedDocumentOutputs()
    {
        var forbiddenNames = new[] { "PdfDocument", "HtmlDocument", "QrCode" };
        var responseTypes = new[]
        {
            typeof(DigitalSalesInvoicePresentationModel),
            typeof(DigitalSalesInvoicePresentationSectionModel),
            typeof(DigitalSalesInvoicePresentationRowModel),
            typeof(DigitalSalesInvoicePresentationNoticeModel)
        };

        foreach (var type in responseTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var forbiddenName in forbiddenNames)
                {
                    Assert.DoesNotContain(forbiddenName, member.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static DigitalSalesInvoicePresentationSectionModel Section(
        DigitalSalesInvoicePresentationModel presentation,
        string name) =>
        presentation.Sections.Single(section => section.Name == name);

    private static DigitalSalesInvoiceRenderModel ValidRenderModel(bool assignedNumber) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            assignedNumber ? Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") : null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "sales_invoice",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "recorded",
            assignedNumber ? "assigned" : "not_assigned",
            assignedNumber ? Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") : null,
            assignedNumber ? 1 : null,
            assignedNumber ? "SI-00000001-A" : null,
            assignedNumber ? "sales_invoice_policy" : null,
            assignedNumber ? "SI-" : null,
            assignedNumber ? "-A" : null,
            assignedNumber ? DateTimeOffset.Parse("2026-07-01T08:15:00Z") : null,
            assignedNumber ? "pos-server:system" : null,
            new DateOnly(2026, 7, 1),
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new string('a', 64),
            "sha256:v1",
            "matched",
            null,
            null,
            null,
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:01:00Z"),
            [Line(1, "Parking fee", 12500)],
            [
                new DigitalSalesInvoiceDiscountRenderModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    12500,
                    1000,
                    0,
                    "PHP",
                    "beneficiary-ref-001",
                    "evidence-ref-001",
                    "discount-validation-001")
            ],
            [TaxDetail(Guid.Parse("33333333-3333-3333-3333-333333333333"))],
            [Tender("central-finality-001", "provider-ref-001", 12500)],
            [Total(Guid.Parse("66666666-6666-6666-6666-666666666666"), 12500)],
            new DigitalSalesInvoiceFooterRenderModel(
                "placeholder_only",
                [
                    "Digital Sales Invoice rendering foundation.",
                    "Final statutory footer text and accredited template remain subject to compliance approval."
                ]));

    private static DigitalSalesInvoiceLineRenderModel Line(int sequence, string description, long grossAmountMinorUnits) =>
        new(
            sequence,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            null,
            description,
            1,
            grossAmountMinorUnits,
            grossAmountMinorUnits,
            0,
            0,
            grossAmountMinorUnits,
            "PHP",
            $"line-source-{sequence:000}");

    private static DigitalSalesInvoiceTenderRenderModel Tender(
        string paymentFinalityRef,
        string providerRef,
        long amountMinorUnits) =>
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "cash",
            amountMinorUnits,
            "PHP",
            $"payment-attempt-{providerRef}",
            $"payment-confirmation-{providerRef}",
            paymentFinalityRef,
            providerRef);

    private static DigitalSalesInvoiceTaxDetailRenderModel TaxDetail(Guid taxTypeCodeId) =>
        new(
            Guid.Parse("10000000-0000-0000-0000-000000000201"),
            taxTypeCodeId,
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            0,
            12500,
            0,
            "PHP");

    private static DigitalSalesInvoiceTotalRenderModel Total(Guid totalTypeCodeId, long amountMinorUnits) =>
        new(totalTypeCodeId, amountMinorUnits, "PHP");
}
