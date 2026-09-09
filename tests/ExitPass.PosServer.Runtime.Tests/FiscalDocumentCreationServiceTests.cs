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
        Assert.Equal(Guid.Parse("77777777-7777-7777-7777-777777777777"), result.Draft.ResolvedFiscalIdentityId);
        Assert.Equal(Guid.Parse("88888888-8888-8888-8888-888888888888"), result.Draft.ResolvedFiscalSequencePolicyId);
        Assert.Equal(1, result.Draft.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", result.Draft.FiscalDocumentNumber);
        Assert.Equal("sales_invoice_policy", result.Draft.FiscalSeries);
        Assert.Equal("SI-", result.Draft.FiscalNumberPrefixText);
        Assert.Equal("-A", result.Draft.FiscalNumberSuffixText);
        Assert.Equal("pos-server:system", result.Draft.FiscalNumberAssignedByRef);
        Assert.NotNull(result.Draft.FiscalNumberAssignedAt);
        Assert.NotNull(repository.LastIdempotency);
        Assert.Equal(
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            repository.LastIdempotency.Scope);
        Assert.Equal("central-finality-001", repository.LastIdempotency.Key);
        Assert.Equal(64, repository.LastIdempotency.SemanticRequestHash.Length);
    }

    [Fact]
    public void SemanticRequestHashIsDeterministicForSameRequest()
    {
        var first = FiscalDocumentSemanticRequestHasher.Hash(ValidCommand());
        var second = FiscalDocumentSemanticRequestHasher.Hash(ValidCommand());

        Assert.Equal(first, second);
    }

    [Fact]
    public void SemanticRequestHashChangesWhenMaterialFiscalFieldChanges()
    {
        var original = FiscalDocumentSemanticRequestHasher.Hash(ValidCommand());
        var changedLine = ValidLine(1) with
        {
            GrossAmountMinorUnits = 13000,
            NetAmountMinorUnits = 13000
        };
        var changed = FiscalDocumentSemanticRequestHasher.Hash(ValidCommand() with { DocumentLines = [changedLine] });

        Assert.NotEqual(original, changed);
    }

    [Fact]
    public async Task AppliedStatutoryFactsCreateSnapshotAndUseStatutoryHashVersion()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidStatutoryCommand());

        Assert.True(result.Succeeded);
        Assert.Equal(1, repository.CreateCount);
        Assert.NotNull(repository.LastIdempotency);
        Assert.NotNull(result.Draft);
        Assert.NotNull(result.Draft.AppliedStatutoryFiscalFacts);
        Assert.Equal(
            FiscalDocumentSemanticRequestHasher.StatutoryVersion,
            FiscalDocumentSemanticRequestHasher.GetVersion(ValidStatutoryCommand()));
        Assert.Equal("SENIOR_CITIZEN", result.Draft.AppliedStatutoryFiscalFacts.EntitlementType);
        Assert.Equal("VAT_EXEMPTION_AND_STATUTORY_DISCOUNT", result.Draft.AppliedStatutoryFiscalFacts.BenefitClassification);
        Assert.Equal(7143, result.Draft.AppliedStatutoryFiscalFacts.FinalPayableAmountMinorUnits);
    }

    [Fact]
    public async Task ZeroPayableStatutoryCompletionCreatesFiscalDocumentWithoutPaymentOrTender()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidZeroPayableStatutoryCommand());

        Assert.True(result.Succeeded, $"{result.ErrorCode}: {result.Message}");
        Assert.Equal(1, repository.CreateCount);
        Assert.NotNull(result.Draft);
        Assert.Equal(FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality, result.Draft.CompletionBasis);
        Assert.Equal("21000000-0000-4000-8000-000000000003", result.Draft.CompletionAuthorityRef);
        Assert.Null(result.Draft.CentralPmsPaymentAttemptRef);
        Assert.Null(result.Draft.CentralPmsPaymentConfirmationRef);
        Assert.Null(result.Draft.PaymentFinalityRef);
        Assert.Empty(result.Draft.Tenders);
        Assert.Equal(2679, result.Draft.DocumentLines.Single().GrossAmountMinorUnits);
        Assert.Equal(2679, result.Draft.DocumentLines.Single().DiscountAmountMinorUnits);
        Assert.Equal(0, result.Draft.DocumentLines.Single().TaxAmountMinorUnits);
        Assert.Equal(0, result.Draft.DocumentLines.Single().NetAmountMinorUnits);
        Assert.Equal(321, result.Draft.TaxDetails.Single().TaxAmountMinorUnits);
        Assert.Equal(0, result.Draft.Totals.Single().AmountMinorUnits);
        Assert.Equal(FiscalDocumentSemanticRequestHasher.CompletionVersion,
            FiscalDocumentSemanticRequestHasher.GetVersion(ValidZeroPayableStatutoryCommand()));
    }

    [Fact]
    public async Task ZeroPayableStatutoryCompletionReplaysWithoutDuplicateDocument()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var first = await service.CreateAsync(ValidZeroPayableStatutoryCommand());
        var replay = await service.CreateAsync(ValidZeroPayableStatutoryCommand());

        Assert.True(first.Succeeded, $"{first.ErrorCode}: {first.Message}");
        Assert.True(replay.Succeeded, $"{replay.ErrorCode}: {replay.Message}");
        Assert.Equal(1, repository.CreateCount);
        Assert.Equal(first.Draft!.FiscalDocumentId, replay.Draft!.FiscalDocumentId);
    }

    [Theory]
    [InlineData("positive_payable")]
    [InlineData("missing_authority")]
    [InlineData("payment_attempt")]
    [InlineData("payment_confirmation")]
    [InlineData("payment_finality")]
    [InlineData("monetary_tender")]
    public async Task ZeroPayableStatutoryCompletionRejectsIncompatibleCompletionFacts(string mutation)
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var command = ValidZeroPayableStatutoryCommand();
        command = mutation switch
        {
            "positive_payable" => command with
            {
                PayableBasis = command.PayableBasis! with { PayableAmountMinorUnits = 1 }
            },
            "missing_authority" => command with { CompletionAuthorityRef = null },
            "payment_attempt" => command with { CentralPmsPaymentAttemptRef = "payment-attempt-001" },
            "payment_confirmation" => command with { CentralPmsPaymentConfirmationRef = "payment-confirmation-001" },
            "payment_finality" => command with { PaymentFinalityRef = "payment-finality-001" },
            "monetary_tender" => command with { Tenders = [ValidTender()] },
            _ => command
        };

        var result = await service.CreateAsync(command);

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.InvalidCompletionAuthority, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task PresentButIncompleteAppliedStatutoryFactsAreRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with { AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with { PolicyReference = null } });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryFactsIncomplete, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
        Assert.Null(repository.LastIdempotency);
    }

    [Fact]
    public async Task UnsupportedAppliedStatutoryCodesAreRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with
                {
                    EntitlementType = "senior_citizen"
                }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryUnsupportedEntitlementType, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task AppliedStatutoryCurrencyMismatchIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with { Currency = "USD" }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryCurrencyMismatch, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task AppliedStatutoryTotalMismatchIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with { FinalPayableAmountMinorUnits = 8000 }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryTotalMismatch, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task AppliedStatutoryProhibitedExtensionFieldIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with
                {
                    UnknownFieldNames = ["beneficiaryName"]
                }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryProhibitedPrivacyField, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task AptAppliedStatutoryFactsRequireTerminalCashTenderReference()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with
                {
                    SourcePaymentChannel = "ASSISTED_PAYMENT_TERMINAL",
                    TerminalCashTenderId = null
                }
            });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.AppliedStatutoryFactsIncomplete, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
    }

    [Fact]
    public async Task AppliedStatutoryReplayReturnsOriginalSnapshotAndConflictDetectsMaterialChanges()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var first = await service.CreateAsync(ValidStatutoryCommand());
        var replay = await service.CreateAsync(ValidStatutoryCommand());
        var conflict = await service.CreateAsync(
            ValidStatutoryCommand() with
            {
                AppliedStatutoryFiscalFacts = ValidAppliedStatutoryFiscalFacts() with { BenefitClassification = "REDUCED_PARKING_RATE" }
            });

        Assert.True(first.Succeeded);
        Assert.True(replay.Succeeded);
        Assert.True(replay.Replayed);
        Assert.Equal(first.Draft!.FiscalDocumentId, replay.Draft!.FiscalDocumentId);
        Assert.Equal(first.Draft.AppliedStatutoryFiscalFacts, replay.Draft.AppliedStatutoryFiscalFacts);
        Assert.False(conflict.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.IdempotencyConflict, conflict.ErrorCode);
        Assert.Equal(1, repository.CreateCount);
    }

    [Fact]
    public async Task DuplicateSameIdempotencyKeyAndSemanticRequestReturnsOriginalDraft()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var first = await service.CreateAsync(ValidCommand());
        var second = await service.CreateAsync(ValidCommand());

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.False(first.Replayed);
        Assert.True(second.Replayed);
        Assert.Equal(1, repository.CreateCount);
        Assert.Equal(first.Draft!.FiscalDocumentId, second.Draft!.FiscalDocumentId);
        Assert.Equal(first.Draft.FiscalSequenceValue, second.Draft.FiscalSequenceValue);
        Assert.Equal(first.Draft.FiscalDocumentNumber, second.Draft.FiscalDocumentNumber);
        Assert.Equal(first.Draft.FiscalNumberAssignedAt, second.Draft.FiscalNumberAssignedAt);
        Assert.Equal(first.Draft.DocumentLines[0].NetAmountMinorUnits, second.Draft.DocumentLines[0].NetAmountMinorUnits);
        Assert.Equal(first.Draft.Tenders[0].PaymentFinalityRef, second.Draft.Tenders[0].PaymentFinalityRef);
    }

    [Fact]
    public async Task SameIdempotencyKeyWithDifferentSemanticRequestFailsClosed()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var first = await service.CreateAsync(ValidCommand());
        var changed = ValidCommand() with
        {
            DocumentLines =
            [
                ValidLine(1) with
                {
                    GrossAmountMinorUnits = 13000,
                    NetAmountMinorUnits = 13000
                }
            ]
        };
        var second = await service.CreateAsync(changed);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(FiscalDocumentCreationErrorCode.IdempotencyConflict, second.ErrorCode);
        Assert.Equal(1, repository.CreateCount);
    }

    [Theory]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityNotFound)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityAmbiguous)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityNotEffective)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotFound)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyAmbiguous)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotEffective)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequenceStateNotFound)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequenceStateNotEffective)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalNumberAllocationFailed)]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalDocumentNumberFormatFailed)]
    public async Task FiscalContextResolutionFailuresFailClosed(FiscalDocumentCreationErrorCode errorCode)
    {
        var repository = new RecordingFiscalDocumentRepository(errorCode);
        var service = new FiscalDocumentCreationService(repository);

        var result = await service.CreateAsync(ValidCommand());

        Assert.False(result.Succeeded);
        Assert.Equal(errorCode, result.ErrorCode);
        Assert.Equal(0, repository.CreateCount);
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

    private static FiscalDocumentCreationCommand ValidStatutoryCommand()
    {
        var facts = ValidAppliedStatutoryFiscalFacts();
        return ValidCommand() with
        {
            SiteId = facts.SiteId,
            CentralPmsParkingSessionRef = facts.ParkingSessionId?.ToString("D"),
            PayableBasis = ValidPayableBasis() with { PayableAmountMinorUnits = 7143 },
            DocumentLines =
            [
                ValidLine(1) with
                {
                    UnitAmountMinorUnits = 10000,
                    GrossAmountMinorUnits = 10000,
                    DiscountAmountMinorUnits = 2857,
                    TaxAmountMinorUnits = 0,
                    NetAmountMinorUnits = 7143
                }
            ],
            Tenders = [ValidTender() with { AmountMinorUnits = 7143 }],
            TaxDetails = [ValidTaxDetail() with { TaxableAmountMinorUnits = 8929, TaxAmountMinorUnits = 0 }],
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with
                {
                    BasisAmountMinorUnits = 10000,
                    DiscountAmountMinorUnits = 1786,
                    VatPrivilegeAmountMinorUnits = 1071
                }
            ],
            Totals = [ValidTotal() with { AmountMinorUnits = 7143 }],
            AppliedStatutoryFiscalFacts = facts
        };
    }

    private static AppliedStatutoryFiscalFactsInput ValidAppliedStatutoryFiscalFacts() =>
        new(
            Guid.Parse("21000000-0000-4000-8000-000000000001"),
            Guid.Parse("21000000-0000-4000-8000-000000000002"),
            Guid.Parse("21000000-0000-4000-8000-000000000003"),
            Guid.Parse("21000000-0000-4000-8000-000000000004"),
            Guid.Parse("21000000-0000-4000-8000-000000000005"),
            Guid.Parse("21000000-0000-4000-8000-000000000006"),
            Guid.Parse("21000000-0000-4000-8000-000000000007"),
            "SENIOR_CITIZEN",
            "VAT_EXEMPTION_AND_STATUTORY_DISCOUNT",
            new AppliedStatutoryPolicyReferenceInput(
                "NATIONAL_LAW",
                AppliedPolicyReferenceId: Guid.Parse("21000000-0000-4000-8000-000000000008"),
                PolicyCode: "TEST-SENIOR-CITIZEN-POLICY",
                NationalLawReference: "TEST-NATIONAL-LAW-REFERENCE"),
            Guid.Parse("21000000-0000-4000-8000-000000000009"),
            Guid.Parse("21000000-0000-4000-8000-000000000010"),
            10000,
            8929,
            0,
            "VAT_EXEMPT",
            1786,
            7143,
            "PHP",
            DateTimeOffset.Parse("2026-07-29T08:15:00+08:00"),
            "WEBPAY");

    private static FiscalDocumentCreationCommand ValidZeroPayableStatutoryCommand()
    {
        var facts = ValidAppliedStatutoryFiscalFacts() with
        {
            BenefitClassification = "FREE_PARKING",
            OriginalAmountMinorUnits = 3000,
            VatExclusiveBasisAmountMinorUnits = 2679,
            VatAmountMinorUnits = 321,
            VatTreatment = "VAT_EXCLUSIVE",
            StatutoryDiscountAmountMinorUnits = 2679,
            FinalPayableAmountMinorUnits = 0
        };
        var appliedTariffRef = facts.AppliedTariffSnapshotId!.Value.ToString("D");
        var upstreamReference = $"ZERO_PAYABLE_STATUTORY_FINALITY:{facts.StatutoryPayableBasisApplicationCommandId:D}";

        return ValidCommand() with
        {
            SiteId = facts.SiteId,
            CentralPmsParkingSessionRef = facts.ParkingSessionId?.ToString("D"),
            CentralPmsPaymentAttemptRef = null,
            CentralPmsPaymentConfirmationRef = null,
            PaymentFinalityRef = null,
            CompletionBasis = FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality,
            CompletionAuthorityRef = facts.StatutoryPayableBasisApplicationCommandId?.ToString("D"),
            PayableBasis = ValidPayableBasis() with
            {
                PayableBasisRef = appliedTariffRef,
                UpstreamFinalityRef = upstreamReference,
                PayableAmountMinorUnits = 0
            },
            DocumentLines =
            [
                ValidLine(1) with
                {
                    UnitAmountMinorUnits = 2679,
                    GrossAmountMinorUnits = 2679,
                    DiscountAmountMinorUnits = 2679,
                    TaxAmountMinorUnits = 0,
                    NetAmountMinorUnits = 0
                }
            ],
            Tenders = [],
            TaxDetails = [ValidTaxDetail() with { TaxableAmountMinorUnits = 2679, TaxAmountMinorUnits = 321, TaxRate = 12 }],
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with
                {
                    BasisAmountMinorUnits = 2679,
                    DiscountAmountMinorUnits = 2679,
                    VatPrivilegeAmountMinorUnits = 321
                }
            ],
            Totals = [ValidTotal() with { AmountMinorUnits = 0 }],
            AppliedStatutoryFiscalFacts = facts
        };
    }

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
        private readonly Dictionary<(string Scope, string Key), (string Hash, FiscalDocumentDraft Draft)> records = [];

        public int CreateCount { get; private set; }
        private readonly FiscalDocumentCreationErrorCode? forcedFailure;

        public RecordingFiscalDocumentRepository(FiscalDocumentCreationErrorCode? forcedFailure = null)
        {
            this.forcedFailure = forcedFailure;
        }

        public FiscalIssuanceIdempotency? LastIdempotency { get; private set; }

        public Task<FiscalDocumentPersistenceResult> CreateAsync(
            FiscalDocumentDraft draft,
            FiscalIssuanceIdempotency idempotency,
            CancellationToken cancellationToken)
        {
            LastIdempotency = idempotency;
            var key = (idempotency.Scope, idempotency.Key);
            if (records.TryGetValue(key, out var existing))
            {
                if (!string.Equals(existing.Hash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                {
                    throw new FiscalDocumentIdempotencyConflictException();
                }

                return Task.FromResult(FiscalDocumentPersistenceResult.Replayed(existing.Draft));
            }

            if (forcedFailure is not null)
            {
                throw new FiscalDocumentFiscalContextException(
                    forcedFailure.Value,
                    "Fiscal context resolution failed closed.");
            }

            CreateCount++;
            var resolvedDraft = draft with
            {
                ResolvedFiscalIdentityId = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                ResolvedFiscalSequencePolicyId = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                FiscalSequenceValue = 1,
                FiscalDocumentNumber = "SI-00000001-A",
                FiscalSeries = "sales_invoice_policy",
                FiscalNumberPrefixText = "SI-",
                FiscalNumberSuffixText = "-A",
                FiscalNumberAssignedAt = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
                FiscalNumberAssignedByRef = "pos-server:system"
            };
            records.Add(key, (idempotency.SemanticRequestHash, resolvedDraft));
            return Task.FromResult(FiscalDocumentPersistenceResult.Created(resolvedDraft));
        }

        public Task<FiscalDocumentVoidPersistenceResult> VoidAsync(
            FiscalDocumentVoidCommand command,
            FiscalDocumentVoidIdempotency idempotency,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
