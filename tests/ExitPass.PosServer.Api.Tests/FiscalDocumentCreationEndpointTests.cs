using System.Reflection;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentCreationEndpointTests
{
    [Fact]
    public async Task ValidApprovedPayableBasisRequestIsMappedIntoCommand()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.True(response.Succeeded);
        Assert.Equal("accepted", response.Code);
        Assert.Equal(StatusCodes.Status202Accepted, response.HttpStatusCode);
        Assert.Equal("newly_created", response.ResultClassification);
        Assert.Equal("fiscal_document_number_assigned", response.FiscalIssuanceEvidenceStatus);
        Assert.Equal("assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(Guid.Parse("77777777-7777-7777-7777-777777777777"), response.FiscalIdentityId);
        Assert.Equal(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), response.FiscalDocumentStatusCodeId);
        Assert.Equal(Guid.Parse("88888888-8888-8888-8888-888888888888"), response.FiscalSequencePolicyId);
        Assert.Equal(1, response.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", response.FiscalDocumentNumber);
        Assert.Equal("sales_invoice_policy", response.FiscalSeries);
        Assert.Equal("SI-", response.FiscalNumberPrefixText);
        Assert.Equal("-A", response.FiscalNumberSuffixText);
        Assert.Equal("pos-server:system", response.FiscalNumberAssignedByRef);
        Assert.NotNull(response.FiscalNumberAssignedAt);
        Assert.NotNull(repository.LastDraft);
        Assert.Equal("payable-basis-001", repository.LastDraft.PayableBasisRef);
        Assert.Equal("central-finality-001", repository.LastDraft.UpstreamFinalityRef);
        Assert.Equal("discount-validation-001", repository.LastDraft.DiscountReferences[0].DiscountValidationRef);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), repository.LastDraft.DocumentLinks[0].TargetFiscalDocumentId);
        Assert.Equal(1, repository.LastDraft.DocumentLines[0].LineSequence);
        Assert.Equal("Parking fee", repository.LastDraft.DocumentLines[0].Description);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), repository.LastDraft.Tenders[0].TenderTypeCodeId);
        Assert.Equal(12500, repository.LastDraft.Tenders[0].AmountMinorUnits);
        Assert.Equal(Guid.Parse("33333333-3333-3333-3333-333333333333"), repository.LastDraft.TaxDetails[0].TaxTypeCodeId);
        Assert.Equal(12500, repository.LastDraft.TaxDetails[0].TaxableAmountMinorUnits);
        Assert.Equal(1, repository.LastDraft.TaxDetails[0].LineSequence);
        Assert.Equal(Guid.Parse("55555555-5555-5555-5555-555555555555"), repository.LastDraft.DiscountPrivilegeDetails[0].DiscountPrivilegeTypeCodeId);
        Assert.Equal(1000, repository.LastDraft.DiscountPrivilegeDetails[0].DiscountAmountMinorUnits);
        Assert.Equal("discount-validation-001", repository.LastDraft.DiscountPrivilegeDetails[0].ApprovalRef);
        Assert.Equal(Guid.Parse("66666666-6666-6666-6666-666666666666"), repository.LastDraft.Totals[0].TotalTypeCodeId);
        Assert.Equal(12500, repository.LastDraft.Totals[0].AmountMinorUnits);
    }

    [Fact]
    public async Task MissingPayableBasisMapsToDeterministicFailure()
    {
        var response = await CreateWithRecordingRepository(ValidRequest() with { PayableBasis = null });

        Assert.False(response.Succeeded);
        Assert.Equal("missing_payable_basis", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal("do_not_retry_without_request_change", response.ErrorPosture);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task MissingUpstreamFinalityReferenceMapsToDeterministicFailure()
    {
        var payableBasis = ValidPayableBasis() with { UpstreamFinalityRef = " " };

        var response = await CreateWithRecordingRepository(ValidRequest() with { PayableBasis = payableBasis });

        Assert.False(response.Succeeded);
        Assert.Equal("missing_upstream_finality_reference", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("rejected")]
    [InlineData("expired")]
    [InlineData("unresolved")]
    [InlineData("inconsistent")]
    public async Task PendingRejectedExpiredUnresolvedDiscountReferencesAreRejectedThroughEntrypoint(string status)
    {
        var discount = new FiscalDiscountReferenceRequest("discount-validation-001", status, true);
        var payableBasis = ValidPayableBasis() with { DiscountReferences = [discount] };

        var response = await CreateWithRecordingRepository(ValidRequest() with { PayableBasis = payableBasis });

        Assert.False(response.Succeeded);
        Assert.Equal("unapproved_discount_reference", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task RawIdEvidencePayloadMarkersAreRejectedThroughEntrypoint()
    {
        var discount = new FiscalDiscountReferenceRequest(
            "discount-validation-001",
            "approved",
            true,
            new Dictionary<string, string> { ["raw_id_image"] = "base64-image-payload" });
        var payableBasis = ValidPayableBasis() with { DiscountReferences = [discount] };

        var response = await CreateWithRecordingRepository(ValidRequest() with { PayableBasis = payableBasis });

        Assert.False(response.Succeeded);
        Assert.Equal("sensitive_evidence_payload_not_allowed", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidDocumentLinkIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DocumentLinks =
            [
                new FiscalDocumentLinkRequest(
                    Guid.Empty,
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"))
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("unsupported_fiscal_document_request", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task MissingFiscalLinesAreRejectedThroughEntrypoint()
    {
        var response = await CreateWithRecordingRepository(ValidRequest() with { DocumentLines = [] });

        Assert.False(response.Succeeded);
        Assert.Equal("unsupported_fiscal_document_request", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidFiscalLineIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DocumentLines =
            [
                ValidLine(1) with { Quantity = -1 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("unsupported_fiscal_document_request", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task MissingTendersAreRejectedThroughEntrypoint()
    {
        var response = await CreateWithRecordingRepository(ValidRequest() with { Tenders = [] });

        Assert.False(response.Succeeded);
        Assert.Equal("missing_fiscal_tender", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task CompleteRequestWithRequiredLocalSchemaContextReachesTenderValidation()
    {
        var request = ValidRequest() with { Tenders = [] };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("missing_fiscal_tender", response.Code);
        Assert.DoesNotContain("local fiscal schema context", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task TopLevelUpstreamFinalityRefAliasReachesTenderValidation()
    {
        var payableBasis = ValidPayableBasis() with { UpstreamFinalityRef = null };
        var request = ValidRequest() with
        {
            PayableBasis = payableBasis,
            UpstreamFinalityRef = "central-finality-001",
            Tenders = []
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("missing_fiscal_tender", response.Code);
        Assert.DoesNotContain("upstream payment/finality", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidTenderAmountIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Tenders =
            [
                ValidTender() with { AmountMinorUnits = 0 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_tender", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task TenderCurrencyMismatchIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Tenders =
            [
                ValidTender() with { CurrencyCode = "USD" }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_tender", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task RawCredentialMarkerInTenderIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Tenders =
            [
                ValidTender() with
                {
                    TenderContext = new Dictionary<string, string> { ["payment_payload"] = "raw-provider-callback" }
                }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("sensitive_tender_payload_not_allowed", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidTaxDetailAmountIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            TaxDetails =
            [
                ValidTaxDetail() with { TaxableAmountMinorUnits = -1 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_tax_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task TaxDetailCurrencyMismatchIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            TaxDetails =
            [
                ValidTaxDetail() with { CurrencyCode = "USD" }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_tax_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task TaxDetailReferencingMissingLineIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            TaxDetails =
            [
                ValidTaxDetail() with { LineSequence = 99 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_tax_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task RawPaymentPayloadMarkerInTaxDetailIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            TaxDetails =
            [
                ValidTaxDetail() with
                {
                    TaxContext = new Dictionary<string, string> { ["payment_payload"] = "raw-provider-callback" }
                }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("sensitive_tax_detail_payload_not_allowed", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidDiscountPrivilegeAmountIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with { DiscountAmountMinorUnits = -1 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_discount_privilege_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task DiscountPrivilegeCurrencyMismatchIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with { CurrencyCode = "USD" }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_discount_privilege_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task DiscountPrivilegeReferencingMissingLineIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with { LineSequence = 99 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_discount_privilege_detail", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task RawEvidenceMarkerInDiscountPrivilegeDetailIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            DiscountPrivilegeDetails =
            [
                ValidDiscountPrivilegeDetail() with { EvidenceRef = "raw_id_image_payload" }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("sensitive_discount_privilege_payload_not_allowed", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidFiscalTotalAmountIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Totals =
            [
                ValidTotal() with { AmountMinorUnits = -1 }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_total", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task FiscalTotalCurrencyMismatchIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Totals =
            [
                ValidTotal() with { CurrencyCode = "USD" }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_total", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task DuplicateFiscalTotalTypeIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Totals =
            [
                ValidTotal(),
                ValidTotal()
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_fiscal_total", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task RawPaymentPayloadMarkerInFiscalTotalIsRejectedThroughEntrypoint()
    {
        var request = ValidRequest() with
        {
            Totals =
            [
                ValidTotal() with
                {
                    TotalContext = new Dictionary<string, string> { ["payment_payload"] = "raw-provider-callback" }
                }
            ]
        };

        var response = await CreateWithRecordingRepository(request);

        Assert.False(response.Succeeded);
        Assert.Equal("sensitive_total_payload_not_allowed", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task LinesAliasMapsIntoFiscalDocumentLines()
    {
        var request = ValidRequest() with
        {
            DocumentLines = null,
            Lines = [ValidLine(1)]
        };
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(request, service);

        Assert.True(response.Succeeded);
        Assert.NotNull(repository.LastDraft);
        Assert.Equal(1, repository.LastDraft.DocumentLines[0].LineSequence);
    }

    [Fact]
    public async Task DuplicateSameIdempotencyKeyAndSemanticRequestReturnsOriginalFiscalDocumentId()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var first = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);
        var second = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal("accepted", first.Code);
        Assert.Equal("accepted", second.Code);
        Assert.Equal("newly_created", first.ResultClassification);
        Assert.Equal("idempotent_replay", second.ResultClassification);
        Assert.Equal("assigned", first.FiscalNumberAssignmentState);
        Assert.Equal("assigned", second.FiscalNumberAssignmentState);
        Assert.Equal("fiscal_document_number_assigned", second.FiscalIssuanceEvidenceStatus);
        Assert.Equal(StatusCodes.Status202Accepted, second.HttpStatusCode);
        Assert.NotNull(first.FiscalDocumentId);
        Assert.Equal(first.FiscalDocumentId, second.FiscalDocumentId);
        Assert.Equal(first.FiscalSequenceValue, second.FiscalSequenceValue);
        Assert.Equal(first.FiscalDocumentNumber, second.FiscalDocumentNumber);
        Assert.Equal(1, repository.CreateCount);
    }

    [Fact]
    public async Task SameIdempotencyKeyWithDifferentSemanticRequestReturnsConflict()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);
        var changedLine = ValidLine(1) with
        {
            GrossAmountMinorUnits = 13000,
            NetAmountMinorUnits = 13000
        };

        var first = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);
        var second = await FiscalDocumentCreationEndpoint.CreateAsync(
            ValidRequest() with { DocumentLines = [changedLine] },
            service);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("fiscal_document_idempotency_conflict", second.Code);
        Assert.Equal("not_assigned", second.FiscalNumberAssignmentState);
        Assert.Equal("do_not_retry_without_request_change", second.ErrorPosture);
        Assert.Equal(StatusCodes.Status409Conflict, second.HttpStatusCode);
        Assert.Null(second.FiscalDocumentId);
        Assert.Null(second.FiscalDocumentNumber);
        Assert.Null(second.FiscalIssuanceEvidenceStatus);
        Assert.Equal(1, repository.CreateCount);
    }

    [Theory]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityNotFound, "fiscal_identity_not_found")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityAmbiguous, "fiscal_identity_ambiguous")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalIdentityNotEffective, "fiscal_identity_not_effective")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotFound, "fiscal_sequence_policy_not_found")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyAmbiguous, "fiscal_sequence_policy_ambiguous")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequencePolicyNotEffective, "fiscal_sequence_policy_not_effective")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequenceStateNotFound, "fiscal_sequence_state_not_found")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalSequenceStateNotEffective, "fiscal_sequence_state_not_effective")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalNumberAllocationFailed, "fiscal_number_allocation_failed")]
    [InlineData(FiscalDocumentCreationErrorCode.FiscalDocumentNumberFormatFailed, "fiscal_document_number_format_failed")]
    public async Task FiscalContextResolutionFailuresMapToDeterministicResponses(
        FiscalDocumentCreationErrorCode errorCode,
        string expectedCode)
    {
        var service = new FiscalDocumentCreationService(new RecordingFiscalDocumentRepository(errorCode));

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal(expectedCode, response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_configuration_correction", response.ErrorPosture);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
        Assert.Null(response.FiscalDocumentId);
    }

    [Fact]
    public async Task ResolvedFiscalIdentityPolicyAndNumberReachCreationResult()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentCreationService(repository);

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.True(response.Succeeded);
        Assert.NotNull(repository.LastDraft);
        Assert.Equal(Guid.Parse("77777777-7777-7777-7777-777777777777"), repository.LastDraft.ResolvedFiscalIdentityId);
        Assert.Equal(Guid.Parse("88888888-8888-8888-8888-888888888888"), repository.LastDraft.ResolvedFiscalSequencePolicyId);
        Assert.Equal(1, repository.LastDraft.FiscalSequenceValue);
        Assert.Equal("SI-00000001-A", repository.LastDraft.FiscalDocumentNumber);
    }

    [Fact]
    public async Task SuccessfulPersistenceWithoutCompleteNumberingEvidenceFailsClosed()
    {
        var service = new FiscalDocumentCreationService(new RecordingFiscalDocumentRepository(returnIncompleteNumbering: true));

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("fiscal_number_assignment_incomplete", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_service_recovery", response.ErrorPosture);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.NotNull(response.FiscalDocumentId);
        Assert.Null(response.FiscalDocumentNumber);
    }

    [Fact]
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new FiscalDocumentCreationService(new PersistenceNotConfiguredFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_configuration_correction", response.ErrorPosture);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidPersistenceConfigurationFailsClosedWithoutConnectionDetails()
    {
        var service = new FiscalDocumentCreationService(new InvalidPersistenceConfigurationFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_persistence_configuration", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal("retry_after_configuration_correction", response.ErrorPosture);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.DoesNotContain("postgresql://", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InvalidFiscalDocumentRequestStillReturnsValidationFailureBeforePersistence()
    {
        var service = new FiscalDocumentCreationService(new InvalidPersistenceConfigurationFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest() with { PayableBasis = null }, service);

        Assert.False(response.Succeeded);
        Assert.Equal("missing_payable_basis", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public void DependencyInjectionUsesFailClosedRepositoryWhenPersistenceIsNotConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IFiscalDocumentRepository>();
        Assert.IsType<PersistenceNotConfiguredFiscalDocumentRepository>(repository);
    }

    [Fact]
    public void DependencyInjectionUsesInvalidConfigurationRepositoryForMalformedConnectionString()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PosServer"] = "not-a-valid-npgsql-connection-string"
            })
            .Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IFiscalDocumentRepository>();
        Assert.IsType<InvalidPersistenceConfigurationFiscalDocumentRepository>(repository);
    }

    [Fact]
    public void DependencyInjectionUsesInvalidConfigurationRepositoryForUrlStylePostgresConnectionString()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSSERVER_DB_URL"] = "postgresql://exitpass:secret@host.docker.internal:5433/posserver_validation_local"
            })
            .Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IFiscalDocumentRepository>();
        Assert.IsType<InvalidPersistenceConfigurationFiscalDocumentRepository>(repository);
    }

    [Fact]
    public void DependencyInjectionUsesPostgresRepositoryWhenConnectionIsConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PosServer"] = "Host=localhost;Database=posserver_validation_local;Username=postgres;Password=postgres"
            })
            .Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var repository = provider.GetRequiredService<IFiscalDocumentRepository>();
        Assert.IsType<PostgresFiscalDocumentRepository>(repository);
    }

    [Fact]
    public void RequestDtosDoNotContainEntitlementApprovalFields()
    {
        var forbiddenNames = new[] { "Entitlement", "Eligibility", "Ordinance", "SupervisorApproval", "OperatorApproval", "RawId", "EvidencePayload" };
        var dtoTypes = new[]
        {
            typeof(CreateFiscalDocumentRequest),
            typeof(FiscalizationPayableBasisRequest),
            typeof(FiscalDiscountReferenceRequest),
            typeof(FiscalDocumentLinkRequest),
            typeof(CreateFiscalDocumentLineRequest),
            typeof(CreateFiscalTenderRequest),
            typeof(CreateFiscalTaxDetailRequest),
            typeof(CreateFiscalDiscountPrivilegeDetailRequest),
            typeof(CreateFiscalTotalRequest)
        };

        foreach (var type in dtoTypes)
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
        AssertAssemblyDoesNotExpose(
            ["OperatorConsole", "ValidateEntitlement", "ApproveEntitlement", "ReviewEvidence", "EvidenceCapture"],
            typeof(FiscalDocumentCreationEndpoint).Assembly);
    }

    [Fact]
    public void NoLocalOrdinanceDecisionBehaviorIsIntroduced()
    {
        AssertAssemblyDoesNotExpose(
            ["LocalOrdinance", "OrdinanceEligibility", "ResidencyEligibility"],
            typeof(FiscalDocumentCreationEndpoint).Assembly);
    }

    [Fact]
    public void NoPaymentFinalityOrGateExitBehaviorIsExposed()
    {
        AssertAssemblyDoesNotExpose(
            ["PaymentStatus", "FinalizePayment", "PaymentLifecycle", "ExitAuthorization", "GateExecution", "OpenGate"],
            typeof(FiscalDocumentCreationEndpoint).Assembly);
    }

    private static async Task<CreateFiscalDocumentResponse> CreateWithRecordingRepository(CreateFiscalDocumentRequest request)
    {
        var service = new FiscalDocumentCreationService(new RecordingFiscalDocumentRepository());
        return await FiscalDocumentCreationEndpoint.CreateAsync(request, service);
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

    private static CreateFiscalDocumentRequest ValidRequest() =>
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
                new FiscalDocumentLinkRequest(
                    Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
                    Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
                    CreatedByRef: "pos-server-api")
            ],
            DocumentLines: [ValidLine(1)],
            Tenders: [ValidTender()],
            TaxDetails: [ValidTaxDetail()],
            DiscountPrivilegeDetails: [ValidDiscountPrivilegeDetail()],
            Totals: [ValidTotal()]);

    private static FiscalizationPayableBasisRequest ValidPayableBasis() =>
        new(
            "payable-basis-001",
            "central-finality-001",
            "PHP",
            12500,
            [new FiscalDiscountReferenceRequest("discount-validation-001", "approved", true)]);

    private static CreateFiscalDocumentLineRequest ValidLine(int lineSequence) =>
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

    private static CreateFiscalTenderRequest ValidTender() =>
        new(
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            12500,
            "PHP",
            CentralPmsPaymentAttemptRef: "payment-attempt-001",
            CentralPmsPaymentConfirmationRef: "payment-confirmation-001",
            PaymentFinalityRef: "central-finality-001",
            ProviderRef: "provider-ref-001",
            TenderContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static CreateFiscalTaxDetailRequest ValidTaxDetail() =>
        new(
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            12500,
            0,
            "PHP",
            LineSequence: 1,
            TaxRate: 0,
            TaxContext: new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private static CreateFiscalDiscountPrivilegeDetailRequest ValidDiscountPrivilegeDetail() =>
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

    private static CreateFiscalTotalRequest ValidTotal() =>
        new(
            Guid.Parse("66666666-6666-6666-6666-666666666666"),
            12500,
            "PHP",
            new Dictionary<string, string> { ["source_system"] = "central_pms" });

    private sealed class RecordingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        private readonly Dictionary<(string Scope, string Key), (string Hash, FiscalDocumentDraft Draft)> records = [];
        private readonly FiscalDocumentCreationErrorCode? forcedFailure;
        private readonly bool returnIncompleteNumbering;

        public RecordingFiscalDocumentRepository(
            FiscalDocumentCreationErrorCode? forcedFailure = null,
            bool returnIncompleteNumbering = false)
        {
            this.forcedFailure = forcedFailure;
            this.returnIncompleteNumbering = returnIncompleteNumbering;
        }

        public int CreateCount { get; private set; }
        public FiscalDocumentDraft? LastDraft { get; private set; }

        public Task<FiscalDocumentPersistenceResult> CreateAsync(
            FiscalDocumentDraft draft,
            FiscalIssuanceIdempotency idempotency,
            CancellationToken cancellationToken)
        {
            var key = (idempotency.Scope, idempotency.Key);
            if (records.TryGetValue(key, out var existing))
            {
                if (!string.Equals(existing.Hash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                {
                    throw new FiscalDocumentIdempotencyConflictException();
                }

                LastDraft = existing.Draft;
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
                ResolvedFiscalSequencePolicyId = Guid.Parse("88888888-8888-8888-8888-888888888888")
            };
            if (!returnIncompleteNumbering)
            {
                resolvedDraft = resolvedDraft with
                {
                    FiscalSequenceValue = 1,
                    FiscalDocumentNumber = "SI-00000001-A",
                    FiscalSeries = "sales_invoice_policy",
                    FiscalNumberPrefixText = "SI-",
                    FiscalNumberSuffixText = "-A",
                    FiscalNumberAssignedAt = DateTimeOffset.Parse("2026-07-01T00:00:00Z"),
                    FiscalNumberAssignedByRef = "pos-server:system"
                };
            }
            LastDraft = resolvedDraft;
            records.Add(key, (idempotency.SemanticRequestHash, resolvedDraft));
            return Task.FromResult(FiscalDocumentPersistenceResult.Created(resolvedDraft));
        }
    }
}
