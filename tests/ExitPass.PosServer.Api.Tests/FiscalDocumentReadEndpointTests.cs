using System.Reflection;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentReadEndpointTests
{
    [Fact]
    public async Task ExistingDocumentMapsToFoundResponse()
    {
        var document = ValidReadModel();
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(document));

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(document.FiscalDocumentId, service);

        Assert.True(response.Succeeded);
        Assert.Equal("found", response.Code);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Same(document, response.Document);
    }

    [Fact]
    public async Task ExistingDocumentCanReturnPopulatedFiscalNumberingFields()
    {
        var assignedAt = DateTimeOffset.Parse("2026-07-01T08:15:00Z");
        var document = ValidReadModel() with
        {
            FiscalIdentityId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            FiscalSequencePolicyId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            FiscalSequenceValue = 42,
            FiscalDocumentNumber = "SI-00000042",
            FiscalSeries = "SI",
            FiscalNumberPrefixText = "SI-",
            FiscalNumberSuffixText = "-A",
            FiscalNumberAssignedAt = assignedAt,
            FiscalNumberAssignedByRef = "pos-server-numbering-service"
        };
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(document));

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(document.FiscalDocumentId, service);

        Assert.True(response.Succeeded);
        Assert.NotNull(response.Document);
        Assert.Equal(Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"), response.Document.FiscalIdentityId);
        Assert.Equal(Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"), response.Document.FiscalSequencePolicyId);
        Assert.Equal(42, response.Document.FiscalSequenceValue);
        Assert.Equal("SI-00000042", response.Document.FiscalDocumentNumber);
        Assert.Equal("SI", response.Document.FiscalSeries);
        Assert.Equal("SI-", response.Document.FiscalNumberPrefixText);
        Assert.Equal("-A", response.Document.FiscalNumberSuffixText);
        Assert.Equal(assignedAt, response.Document.FiscalNumberAssignedAt);
        Assert.Equal("pos-server-numbering-service", response.Document.FiscalNumberAssignedByRef);
    }

    [Fact]
    public async Task ExistingDocumentCanReturnNullFiscalNumberingFields()
    {
        var document = ValidReadModel();
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(document));

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(document.FiscalDocumentId, service);

        Assert.True(response.Succeeded);
        Assert.NotNull(response.Document);
        Assert.Null(response.Document.FiscalIdentityId);
        Assert.Null(response.Document.FiscalSequencePolicyId);
        Assert.Null(response.Document.FiscalSequenceValue);
        Assert.Null(response.Document.FiscalDocumentNumber);
        Assert.Null(response.Document.FiscalSeries);
        Assert.Null(response.Document.FiscalNumberPrefixText);
        Assert.Null(response.Document.FiscalNumberSuffixText);
        Assert.Null(response.Document.FiscalNumberAssignedAt);
        Assert.Null(response.Document.FiscalNumberAssignedByRef);
    }

    [Fact]
    public async Task MissingDocumentMapsToDeterministicNotFound()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var service = new FiscalDocumentReadService(new StubFiscalDocumentReader(null));

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(documentId, service);

        Assert.False(response.Succeeded);
        Assert.Equal("fiscal_document_not_found", response.Code);
        Assert.Equal(StatusCodes.Status404NotFound, response.HttpStatusCode);
        Assert.Null(response.Document);
    }

    [Fact]
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new FiscalDocumentReadService(new PersistenceNotConfiguredFiscalDocumentReader());

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(Guid.NewGuid(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidPersistenceConfigurationFailsClosedWithoutConnectionDetails()
    {
        var service = new FiscalDocumentReadService(new InvalidPersistenceConfigurationFiscalDocumentReader());

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(Guid.NewGuid(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_persistence_configuration", response.Code);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.DoesNotContain("postgresql://", response.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Password", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PersistenceReadFailureFailsClosedWithoutConnectionDetails()
    {
        var service = new FiscalDocumentReadService(new FailingFiscalDocumentReader());

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(Guid.NewGuid(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_read_failed", response.Code);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.DoesNotContain("Password", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DependencyInjectionUsesFailClosedReaderWhenPersistenceIsNotConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var reader = provider.GetRequiredService<IFiscalDocumentReader>();
        Assert.IsType<PersistenceNotConfiguredFiscalDocumentReader>(reader);
    }

    [Fact]
    public void DependencyInjectionUsesInvalidConfigurationReaderForUrlStylePostgresConnectionString()
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
        var reader = provider.GetRequiredService<IFiscalDocumentReader>();
        Assert.IsType<InvalidPersistenceConfigurationFiscalDocumentReader>(reader);
    }

    [Fact]
    public void DependencyInjectionUsesPostgresReaderWhenConnectionIsConfigured()
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
        var reader = provider.GetRequiredService<IFiscalDocumentReader>();
        Assert.IsType<PostgresFiscalDocumentReader>(reader);
    }

    [Fact]
    public void GetResponseDoesNotExposeAuthorityLeakingFields()
    {
        var forbiddenNames = new[]
        {
            "Entitlement",
            "Eligibility",
            "Ordinance",
            "PaymentStatus",
            "FinalizePayment",
            "ExitAuthorization",
            "GateExecution",
            "BirFinality",
            "XRead",
            "ZRead",
            "Annex"
        };
        var responseTypes = new[]
        {
            typeof(GetFiscalDocumentResponse),
            typeof(FiscalDocumentReadModel),
            typeof(FiscalTenderReadModel),
            typeof(FiscalDiscountPrivilegeDetailReadModel),
            typeof(FiscalTotalReadModel)
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

    private static FiscalDocumentReadModel ValidReadModel() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            null,
            null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 7, 1),
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private sealed class StubFiscalDocumentReader : IFiscalDocumentReader
    {
        private readonly FiscalDocumentReadModel? document;

        public StubFiscalDocumentReader(FiscalDocumentReadModel? document)
        {
            this.document = document;
        }

        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken) =>
            Task.FromResult(document?.FiscalDocumentId == fiscalDocumentId ? document : null);
    }

    private sealed class FailingFiscalDocumentReader : IFiscalDocumentReader
    {
        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
        {
            throw new FiscalDocumentPersistenceException(
                "Fiscal document read failed.",
                new InvalidOperationException("storage failure"));
        }
    }
}
