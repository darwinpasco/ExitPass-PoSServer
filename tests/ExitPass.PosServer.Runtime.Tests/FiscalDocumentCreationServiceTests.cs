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
    public void CommandModelsDoNotIncludeEntitlementApprovalFields()
    {
        var forbiddenNames = new[] { "Entitlement", "Eligibility", "Ordinance", "SupervisorApproval", "OperatorApproval", "RawId", "EvidencePayload" };
        var modelTypes = new[]
        {
            typeof(FiscalDocumentCreationCommand),
            typeof(FiscalizationPayableBasisInput),
            typeof(FiscalDiscountReferenceInput),
            typeof(FiscalDocumentLinkInput)
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
            ]);

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
