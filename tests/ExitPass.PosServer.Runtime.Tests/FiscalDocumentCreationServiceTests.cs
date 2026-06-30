using System.Reflection;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class FiscalDocumentCreationServiceTests
{
    [Fact]
    public async Task ValidApprovedPayableBasisInputReachesDocumentCreationPath()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.None, result.ErrorCode);
        Assert.Equal(1, repository.CreateCount);
        Assert.NotNull(result.Draft);
        Assert.Equal("payable-basis-001", result.Draft.PayableBasisRef);
        Assert.Equal("central-finality-001", result.Draft.UpstreamFinalityRef);
        Assert.Equal("discount-validation-001", result.Draft.DiscountReferences[0].DiscountValidationRef);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), result.Draft.DocumentLinks[0].TargetFiscalDocumentId);
        Assert.Equal(1, result.Draft.DocumentLines[0].LineSequence);
        Assert.Equal("Parking fee", result.Draft.DocumentLines[0].Description);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), result.Draft.Tenders[0].TenderTypeCodeId);
        Assert.Equal(12500, result.Draft.Tenders[0].AmountMinorUnits);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), result.Draft.TaxDetails[0].TaxTypeCodeId);
        Assert.Equal(12500, result.Draft.TaxDetails[0].TaxableAmountMinorUnits);
        Assert.Equal(1, result.Draft.TaxDetails[0].LineSequence);
        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), result.Draft.DiscountPrivilegeDetails[0].DiscountPrivilegeTypeCodeId);
        Assert.Equal(1000, result.Draft.DiscountPrivilegeDetails[0].DiscountAmountMinorUnits);
        Assert.Equal("discount-validation-001", result.Draft.DiscountPrivilegeDetails[0].ApprovalRef);
        Assert.Equal(Guid.Parse("66666666-6666-6666-6666-666666666666"), result.Draft.Totals[0].TotalTypeCodeId);
        Assert.Equal(12500, result.Draft.Totals[0].AmountMinorUnits);
    }

    [Fact]
    public async Task MissingPayableBasisIsRejected()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidCommand() with { PayableBasis = null });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.MissingPayableBasis, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task MissingUpstreamFinalityReferenceIsRejected()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var payableBasis = ValidPayableBasis() with { UpstreamFinalityRef = " " };
        var result = await service.CreateAsync(ValidCommand() with { PayableBasis = payableBasis });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.MissingUpstreamFinalityReference, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData(FiscalDiscountReferenceStatus.Pending)]
    [InlineData(FiscalDiscountReferenceStatus.Rejected)]
    [InlineData(FiscalDiscountReferenceStatus.Expired)]
    [InlineData(FiscalDiscountReferenceStatus.Unresolved)]
    [InlineData(FiscalDiscountReferenceStatus.Inconsistent)]
    public async Task PendingRejectedExpiredUnresolvedDiscountReferencesAreRejected(FiscalDiscountReferenceStatus status)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var discount = new FiscalDiscountReferenceInput("discount-validation-001", status, true);
        var payableBasis = ValidPayableBasis() with { DiscountReferences = [discount] };

        var result = await service.CreateAsync(ValidCommand() with { PayableBasis = payableBasis });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.UnapprovedDiscountReference, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawIdImageEvidencePayloadIsRejected()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var discount = new FiscalDiscountReferenceInput(
            "discount-validation-001",
            FiscalDiscountReferenceStatus.Approved,
            true,
            new Dictionary<string, string> { ["raw_id_image"] = "base64-image-payload" });
        var payableBasis = ValidPayableBasis() with { DiscountReferences = [discount] };

        var result = await service.CreateAsync(ValidCommand() with { PayableBasis = payableBasis });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveEvidencePayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task InvalidDocumentLinkIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var command = ValidCommand() with
        {
            DocumentLinks =
            [
                new FiscalDocumentLinkInput(
                    Guid.Empty,
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"))
            ]
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawEvidenceMarkerInDocumentLinkIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var command = ValidCommand() with
        {
            DocumentLinks =
            [
                new FiscalDocumentLinkInput(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    LinkReasonText: "raw_id_image")
            ]
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveEvidencePayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task MissingDocumentLinesAreRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidCommand() with { DocumentLines = [] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task DuplicateLineSequenceIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var command = ValidCommand() with
        {
            DocumentLines =
            [
                ValidLine(1),
                ValidLine(1) with { SourceRef = "line-source-002" }
            ]
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData("quantity")]
    [InlineData("amount")]
    [InlineData("description")]
    [InlineData("line_type")]
    public async Task InvalidFiscalLineFieldsAreRejectedBeforePersistence(string invalidField)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var invalidLine = invalidField switch
        {
            "quantity" => ValidLine(1) with { Quantity = 0 },
            "amount" => ValidLine(1) with { NetAmountMinorUnits = 999 },
            "description" => ValidLine(1) with { Description = " " },
            "line_type" => ValidLine(1) with { LineTypeCodeId = Guid.Empty },
            _ => ValidLine(1)
        };

        var result = await service.CreateAsync(ValidCommand() with { DocumentLines = [invalidLine] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.UnsupportedFiscalDocumentRequest, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawEvidenceMarkerInDocumentLineIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var line = ValidLine(1) with
        {
            LineContext = new Dictionary<string, string> { ["raw_id_image"] = "base64-image-payload" }
        };

        var result = await service.CreateAsync(ValidCommand() with { DocumentLines = [line] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveEvidencePayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task MissingTendersAreRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidCommand() with { Tenders = [] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.MissingFiscalTender, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("type")]
    [InlineData("blank_ref")]
    public async Task InvalidTenderFieldsAreRejectedBeforePersistence(string invalidField)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var invalidTender = invalidField switch
        {
            "amount" => ValidTender() with { AmountMinorUnits = 0 },
            "currency" => ValidTender() with { CurrencyCode = "USD" },
            "type" => ValidTender() with { TenderTypeCodeId = Guid.Empty },
            "blank_ref" => ValidTender() with { ProviderRef = " " },
            _ => ValidTender()
        };

        var result = await service.CreateAsync(ValidCommand() with { Tenders = [invalidTender] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.InvalidFiscalTender, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawCredentialMarkerInTenderIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var tender = ValidTender() with
        {
            TenderContext = new Dictionary<string, string> { ["card_number"] = "4111111111111111" }
        };

        var result = await service.CreateAsync(ValidCommand() with { Tenders = [tender] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveTenderPayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData("taxable_amount")]
    [InlineData("tax_amount")]
    [InlineData("currency")]
    [InlineData("tax_type")]
    [InlineData("tax_classification")]
    [InlineData("line_sequence")]
    [InlineData("context")]
    public async Task InvalidTaxDetailFieldsAreRejectedBeforePersistence(string invalidField)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var invalidTaxDetail = invalidField switch
        {
            "taxable_amount" => ValidTaxDetail() with { TaxableAmountMinorUnits = -1 },
            "tax_amount" => ValidTaxDetail() with { TaxAmountMinorUnits = -1 },
            "currency" => ValidTaxDetail() with { CurrencyCode = "USD" },
            "tax_type" => ValidTaxDetail() with { TaxTypeCodeId = Guid.Empty },
            "tax_classification" => ValidTaxDetail() with { TaxClassificationCodeId = Guid.Empty },
            "line_sequence" => ValidTaxDetail() with { LineSequence = 99 },
            "context" => ValidTaxDetail() with { TaxContext = new Dictionary<string, string> { ["source_system"] = " " } },
            _ => ValidTaxDetail()
        };

        var result = await service.CreateAsync(ValidCommand() with { TaxDetails = [invalidTaxDetail] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.InvalidFiscalTaxDetail, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawEvidenceOrPaymentMarkerInTaxDetailIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var taxDetail = ValidTaxDetail() with
        {
            TaxContext = new Dictionary<string, string> { ["payment_payload"] = "raw-provider-payload" }
        };

        var result = await service.CreateAsync(ValidCommand() with { TaxDetails = [taxDetail] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveTaxDetailPayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData("basis_amount")]
    [InlineData("discount_amount")]
    [InlineData("vat_privilege_amount")]
    [InlineData("discount_exceeds_basis")]
    [InlineData("vat_privilege_exceeds_basis")]
    [InlineData("currency")]
    [InlineData("type")]
    [InlineData("line_sequence")]
    [InlineData("blank_ref")]
    [InlineData("context")]
    public async Task InvalidDiscountPrivilegeDetailFieldsAreRejectedBeforePersistence(string invalidField)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var invalidDetail = invalidField switch
        {
            "basis_amount" => ValidDiscountPrivilegeDetail() with { BasisAmountMinorUnits = -1 },
            "discount_amount" => ValidDiscountPrivilegeDetail() with { DiscountAmountMinorUnits = -1 },
            "vat_privilege_amount" => ValidDiscountPrivilegeDetail() with { VatPrivilegeAmountMinorUnits = -1 },
            "discount_exceeds_basis" => ValidDiscountPrivilegeDetail() with { BasisAmountMinorUnits = 1000, DiscountAmountMinorUnits = 1001 },
            "vat_privilege_exceeds_basis" => ValidDiscountPrivilegeDetail() with { BasisAmountMinorUnits = 1000, VatPrivilegeAmountMinorUnits = 1001 },
            "currency" => ValidDiscountPrivilegeDetail() with { CurrencyCode = "USD" },
            "type" => ValidDiscountPrivilegeDetail() with { DiscountPrivilegeTypeCodeId = Guid.Empty },
            "line_sequence" => ValidDiscountPrivilegeDetail() with { LineSequence = 99 },
            "blank_ref" => ValidDiscountPrivilegeDetail() with { EvidenceRef = " " },
            "context" => ValidDiscountPrivilegeDetail() with { DiscountPrivilegeContext = new Dictionary<string, string> { ["source_system"] = " " } },
            _ => ValidDiscountPrivilegeDetail()
        };

        var result = await service.CreateAsync(ValidCommand() with { DiscountPrivilegeDetails = [invalidDetail] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.InvalidFiscalDiscountPrivilegeDetail, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawEvidenceOrPaymentMarkerInDiscountPrivilegeDetailIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var detail = ValidDiscountPrivilegeDetail() with
        {
            EvidenceRef = "raw_id_image_payload"
        };

        var result = await service.CreateAsync(ValidCommand() with { DiscountPrivilegeDetails = [detail] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveDiscountPrivilegePayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Theory]
    [InlineData("amount")]
    [InlineData("currency")]
    [InlineData("type")]
    [InlineData("duplicate_type")]
    [InlineData("context")]
    public async Task InvalidFiscalTotalFieldsAreRejectedBeforePersistence(string invalidField)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var command = invalidField switch
        {
            "amount" => ValidCommand() with { Totals = [ValidTotal() with { AmountMinorUnits = -1 }] },
            "currency" => ValidCommand() with { Totals = [ValidTotal() with { CurrencyCode = "USD" }] },
            "type" => ValidCommand() with { Totals = [ValidTotal() with { TotalTypeCodeId = Guid.Empty }] },
            "duplicate_type" => ValidCommand() with { Totals = [ValidTotal(), ValidTotal()] },
            "context" => ValidCommand() with { Totals = [ValidTotal() with { TotalContext = new Dictionary<string, string> { ["source_system"] = " " } }] },
            _ => ValidCommand()
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.InvalidFiscalTotal, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task RawEvidenceOrPaymentMarkerInFiscalTotalIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var total = ValidTotal() with
        {
            TotalContext = new Dictionary<string, string> { ["payment_payload"] = "raw-provider-payload" }
        };

        var result = await service.CreateAsync(ValidCommand() with { Totals = [total] });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.SensitiveTotalPayloadNotAllowed, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public void CommandModelsDoNotIncludeEntitlementApprovalFields()
    {
        var forbiddenNames = new[] { "Entitlement", "Eligibility", "Ordinance", "SupervisorApproval", "OperatorApproval", "RawId", "EvidencePayload" };
        var modelTypes = new[]
        {
            typeof(FiscalDocumentCreationCommand),
            typeof(FiscalizationPayableBasisInput),
            typeof(FiscalDiscountReferenceInput),
            typeof(FiscalDocumentLinkInput),
            typeof(FiscalDocumentLineInput),
            typeof(FiscalTenderInput),
            typeof(FiscalTaxDetailInput),
            typeof(FiscalDiscountPrivilegeDetailInput),
            typeof(FiscalTotalInput)
        };

        foreach (var type in modelTypes)
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

    [Fact]
    public void NoOperatorConsoleValidationBehaviorIsIntroduced()
    {
        var forbiddenNames = new[] { "OperatorConsole", "ValidateEntitlement", "ApproveEntitlement", "ReviewEvidence", "EvidenceCapture" };
        AssertAssemblyDoesNotExpose(forbiddenNames, typeof(FiscalDocumentCreationService).Assembly);
    }

    [Fact]
    public void NoLocalOrdinanceDecisionBehaviorIsIntroduced()
    {
        var forbiddenNames = new[] { "LocalOrdinance", "OrdinanceEligibility", "ResidencyEligibility" };
        AssertAssemblyDoesNotExpose(forbiddenNames, typeof(FiscalDocumentCreationService).Assembly);
    }

    private static void AssertAssemblyDoesNotExpose(string[] forbiddenNames, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes())
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

    private static FiscalDocumentCreationCommand ValidCommand() =>
        new(
            "site-pos-server-001",
            "sales_invoice",
            ValidPayableBasis(),
            SitePosServerId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            FiscalDocumentTypeCodeId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            FiscalDocumentStatusCodeId: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            CentralPmsParkingSessionRef: "parking-session-001",
            CentralPmsPaymentAttemptRef: "payment-attempt-001",
            CentralPmsPaymentConfirmationRef: "payment-confirmation-001",
            PaymentFinalityRef: "central-finality-001",
            DocumentLinks:
            [
                new FiscalDocumentLinkInput(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    CreatedByRef: "pos-server-runtime")
            ],
            DocumentLines: [ValidLine(1)],
            Tenders: [ValidTender()],
            TaxDetails: [ValidTaxDetail()],
            DiscountPrivilegeDetails: [ValidDiscountPrivilegeDetail()],
            Totals: [ValidTotal()]);

    private static FiscalizationPayableBasisInput ValidPayableBasis() =>
        new(
            "payable-basis-001",
            "central-finality-001",
            "PHP",
            12500,
            [
                new FiscalDiscountReferenceInput(
                    "discount-validation-001",
                    FiscalDiscountReferenceStatus.Approved,
                    true)
            ]);

    private static FiscalDocumentLineInput ValidLine(int lineSequence) =>
        new(
            lineSequence,
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Parking fee",
            1,
            12500,
            12500,
            0,
            0,
            12500,
            "PHP",
            SourceRef: $"line-source-{lineSequence:000}",
            LineContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static FiscalTenderInput ValidTender() =>
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            12500,
            "PHP",
            CentralPmsPaymentAttemptRef: "payment-attempt-001",
            CentralPmsPaymentConfirmationRef: "payment-confirmation-001",
            PaymentFinalityRef: "central-finality-001",
            ProviderRef: "provider-ref-001",
            TenderContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static FiscalTaxDetailInput ValidTaxDetail() =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            12500,
            0,
            "PHP",
            LineSequence: 1,
            TaxRate: 0,
            TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static FiscalDiscountPrivilegeDetailInput ValidDiscountPrivilegeDetail() =>
        new(
            Guid.Parse("55555555-5555-5555-5555-555555555555"),
            12500,
            1000,
            0,
            "PHP",
            LineSequence: 1,
            BeneficiaryRef: "beneficiary-ref-001",
            EvidenceRef: "evidence-ref-001",
            ApprovalRef: "discount-validation-001",
            DiscountPrivilegeContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static FiscalTotalInput ValidTotal() =>
        new(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            12500,
            "PHP",
            new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private sealed class RecordingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        public int CreateCount { get; private set; }

        public Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken)
        {
            CreateCount++;
            return Task.FromResult(draft);
        }
    }
}
