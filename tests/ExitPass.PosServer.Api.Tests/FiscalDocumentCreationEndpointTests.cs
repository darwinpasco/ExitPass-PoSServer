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
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new FiscalDocumentCreationService(new PersistenceNotConfiguredFiscalDocumentRepository());

        var response = await FiscalDocumentCreationEndpoint.CreateAsync(ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
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
            typeof(FiscalDiscountReferenceRequest)
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
            PaymentFinalityRef: "central-finality-001");

    private static FiscalizationPayableBasisRequest ValidPayableBasis() =>
        new(
            "payable-basis-001",
            "central-finality-001",
            "PHP",
            12500,
            [new FiscalDiscountReferenceRequest("discount-validation-001", "approved", true)]);

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
