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
        Assert.NotNull(repository.LastDraft);
        Assert.Equal("payable-basis-001", repository.LastDraft.PayableBasisRef);
        Assert.Equal("central-finality-001", repository.LastDraft.UpstreamFinalityRef);
        Assert.Equal("discount-validation-001", repository.LastDraft.DiscountReferences[0].DiscountValidationRef);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), repository.LastDraft.DocumentLinks[0].TargetFiscalDocumentId);
        Assert.Equal(1, repository.LastDraft.DocumentLines[0].LineSequence);
        Assert.Equal("Parking fee", repository.LastDraft.DocumentLines[0].Description);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), repository.LastDraft.Tenders[0].TenderTypeCodeId);
        Assert.Equal(12500, repository.LastDraft.Tenders[0].AmountMinorUnits);
    }

    [Fact]
    public async Task MissingPayableBasisMapsToDeterministicFailure()
    {
        var response = await CreateWithRecordingRepository(ValidRequest() with { PayableBasis = null });

        Assert.False(response.Succeeded);
        Assert.Equal("missing_payable_basis", response.Code);
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
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new FiscalDocumentCreationService(new PersistenceNotConfiguredFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidPersistenceConfigurationFailsClosedWithoutConnectionDetails()
    {
        var service = new FiscalDocumentCreationService(new InvalidPersistenceConfigurationFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_persistence_configuration", response.Code);
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
            typeof(CreateFiscalTenderRequest)
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
            Tenders: [ValidTender()]);

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

    private sealed class RecordingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        public FiscalDocumentDraft? LastDraft { get; private set; }

        public Task<FiscalDocumentDraft> CreateAsync(FiscalDocumentDraft draft, CancellationToken cancellationToken)
        {
            LastDraft = draft;
            return Task.FromResult(draft);
        }
    }
}
