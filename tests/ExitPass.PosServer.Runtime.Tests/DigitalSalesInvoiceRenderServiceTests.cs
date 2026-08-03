using System.Reflection;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class DigitalSalesInvoiceRenderServiceTests
{
    [Fact]
    public async Task RendersAssignedNumberedFiscalDocument()
    {
        var document = ValidReadModel(assignedNumber: true);
        var reader = new StubFiscalDocumentReader(document);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(reader));

        var result = await service.RenderAsync(document.FiscalDocumentId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Render);
        Assert.Equal(document.FiscalDocumentId, result.Render.FiscalDocumentId);
        Assert.Equal("assigned", result.Render.FiscalNumberAssignmentState);
        Assert.Equal(document.FiscalDocumentNumber, result.Render.FiscalDocumentNumber);
        Assert.Equal(document.FiscalSeries, result.Render.FiscalSeries);
        Assert.Equal(document.FiscalNumberPrefixText, result.Render.FiscalNumberPrefixText);
        Assert.Equal(document.FiscalNumberSuffixText, result.Render.FiscalNumberSuffixText);
        Assert.Equal(document.FiscalNumberAssignedAt, result.Render.FiscalNumberAssignedAt);
        Assert.Equal(document.FiscalNumberAssignedByRef, result.Render.FiscalNumberAssignedByRef);
        Assert.Equal(document.SitePosServerId, result.Render.SitePosServerId);
        Assert.Equal(document.FiscalIdentityId, result.Render.FiscalIdentityId);
        Assert.Equal(document.BusinessDayDate, result.Render.BusinessDayDate);
        Assert.Equal(document.CentralPmsParkingSessionRef, result.Render.CentralPmsParkingSessionRef);
        Assert.Equal(document.CentralPmsPaymentAttemptRef, result.Render.CentralPmsPaymentAttemptRef);
        Assert.Equal(document.CentralPmsPaymentConfirmationRef, result.Render.CentralPmsPaymentConfirmationRef);
        Assert.Equal(document.PaymentFinalityRef, result.Render.PaymentFinalityRef);
        Assert.Equal(document.SemanticRequestHash, result.Render.SemanticRequestHash);
        Assert.Equal(document.SemanticRequestHashVersion, result.Render.SemanticRequestHashVersion);
        Assert.Equal(document.SemanticRequestHashStatus, result.Render.SemanticRequestHashStatus);
        Assert.Equal("placeholder_only", result.Render.Footer.RenderingStatus);
        Assert.Equal(1, reader.ReadCount);
    }

    [Fact]
    public async Task RendersLinesTendersTaxesDiscountsAndTotals()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var result = await service.RenderAsync(document.FiscalDocumentId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Render);
        var render = result.Render;
        Assert.Single(render.Lines);
        Assert.Equal("Parking fee", render.Lines[0].Description);
        Assert.Equal(11500, render.Lines[0].NetAmountMinorUnits);
        Assert.Single(render.Tenders);
        Assert.Equal("provider-ref-001", render.Tenders[0].ProviderRef);
        Assert.Single(render.TaxDetails);
        Assert.Equal(0, render.TaxDetails[0].TaxAmountMinorUnits);
        Assert.Single(render.Discounts);
        Assert.Equal("discount-validation-001", render.Discounts[0].ApprovalRef);
        Assert.Equal(1000, render.Discounts[0].DiscountAmountMinorUnits);
        Assert.Single(render.Totals);
        Assert.Equal(12500, render.Totals[0].AmountMinorUnits);
    }

    [Fact]
    public async Task MissingFiscalDocumentFailsSafely()
    {
        var fiscalDocumentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(null)));

        var result = await service.RenderAsync(fiscalDocumentId);

        Assert.False(result.Succeeded);
        Assert.Equal(DigitalSalesInvoiceRenderErrorCode.NotFound, result.ErrorCode);
        Assert.Null(result.Render);
        Assert.Contains(fiscalDocumentId.ToString(), result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UnassignedFiscalNumberIsMarkedSafely()
    {
        var document = ValidReadModel(assignedNumber: false);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var result = await service.RenderAsync(document.FiscalDocumentId);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Render);
        Assert.Equal("not_assigned", result.Render.FiscalNumberAssignmentState);
        Assert.Null(result.Render.FiscalDocumentNumber);
        Assert.Null(result.Render.FiscalSequenceValue);
        Assert.Null(result.Render.FiscalNumberAssignedAt);
    }

    [Fact]
    public async Task RenderDoesNotMutateFiscalDocumentReadModel()
    {
        var document = ValidReadModel(assignedNumber: true);
        var originalUpdatedAt = document.UpdatedAt;
        var originalLineCount = document.Lines.Count;
        var originalTenderCount = document.Tenders.Count;
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var result = await service.RenderAsync(document.FiscalDocumentId);

        Assert.True(result.Succeeded);
        Assert.Equal(originalUpdatedAt, document.UpdatedAt);
        Assert.Equal(originalLineCount, document.Lines.Count);
        Assert.Equal(originalTenderCount, document.Tenders.Count);
        Assert.Equal("SI-00000001-A", document.FiscalDocumentNumber);
    }

    [Fact]
    public void RuntimeAssemblyDoesNotExposePaymentGateExitRefundOrUnauthorizedReportBehavior()
    {
        var forbiddenNames = new[]
        {
            "FinalizePayment",
            "PaymentLifecycle",
            "ExitAuthorization",
            "GateExecution",
            "OpenGate",
            "RefundCommand",
            "RefundService",
            "ProcessRefund",
            "Reversal",
            "ZRead",
            "Annex"
        };

        foreach (var type in typeof(DigitalSalesInvoiceRenderService).Assembly.GetTypes())
        {
            foreach (var forbiddenName in forbiddenNames)
            {
                Assert.DoesNotContain(forbiddenName, type.Name, StringComparison.OrdinalIgnoreCase);
            }

            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                foreach (var forbiddenName in forbiddenNames)
                {
                    Assert.DoesNotContain(forbiddenName, member.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    private static FiscalDocumentReadModel ValidReadModel(bool assignedNumber) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            assignedNumber ? Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") : null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "sales_invoice",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            assignedNumber ? "recorded" : null,
            assignedNumber ? Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") : null,
            assignedNumber ? 1 : null,
            assignedNumber ? "SI-00000001-A" : null,
            assignedNumber ? "sales_invoice_policy" : null,
            assignedNumber ? "SI-" : null,
            assignedNumber ? "-A" : null,
            assignedNumber ? DateTimeOffset.Parse("2026-07-01T08:15:00Z") : null,
            assignedNumber ? "pos-server:system" : null,
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            "central-finality-001",
            "upstream_finality_ref",
            new string('a', 64),
            "sha256:v1",
            "matched",
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 7, 1),
            null,
            null,
            null,
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:01:00Z"),
            [],
            [],
            [
                new FiscalDocumentLineReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    1,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    null,
                    "Parking fee",
                    1,
                    12500,
                    12500,
                    1000,
                    0,
                    11500,
                    "PHP",
                    "line-source-001",
                    "{\"source_system\":\"central_pms\"}",
                    true,
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTenderReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000301"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "cash",
                    12500,
                    "PHP",
                    "payment-attempt-001",
                    "payment-confirmation-001",
                    "central-finality-001",
                    "provider-ref-001",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTaxDetailReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000401"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    0,
                    12500,
                    0,
                    "PHP",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalDiscountPrivilegeDetailReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000501"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    12500,
                    1000,
                    0,
                    "PHP",
                    "beneficiary-ref-001",
                    "evidence-ref-001",
                    "discount-validation-001",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTotalReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000601"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    12500,
                    "PHP",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ]);

    private sealed class StubFiscalDocumentReader : IFiscalDocumentReader
    {
        private readonly FiscalDocumentReadModel? document;

        public StubFiscalDocumentReader(FiscalDocumentReadModel? document)
        {
            this.document = document;
        }

        public int ReadCount { get; private set; }

        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(document?.FiscalDocumentId == fiscalDocumentId ? document : null);
        }
    }
}
