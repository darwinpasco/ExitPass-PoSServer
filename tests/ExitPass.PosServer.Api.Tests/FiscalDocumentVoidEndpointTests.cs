using System.Reflection;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentVoidEndpointTests
{
    [Fact]
    public async Task ValidVoidRequestMapsToAcceptedResponse()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var response = await FiscalDocumentVoidEndpoint.VoidAsync(documentId, ValidRequest(), service);

        Assert.True(response.Succeeded);
        Assert.Equal("accepted", response.Code);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Equal("newly_voided", response.ResultClassification);
        Assert.Equal(documentId, response.FiscalDocumentId);
        Assert.Equal("SI-00000001-A", response.FiscalDocumentNumber);
        Assert.Equal(1, response.FiscalSequenceValue);
        Assert.Equal("voided", response.FiscalDocumentStatus);
        Assert.Equal("recorded", response.VoidStatus);
        Assert.Equal("operator_error", response.VoidReasonCode);
        Assert.Equal("operator-ref-001", response.RequestedByRef);
        Assert.Equal("void-key-001", response.IdempotencyKey);
        Assert.Equal("corr-void-001", response.CorrelationId);
    }

    [Fact]
    public async Task IdempotentReplayMapsToIdempotentReplayClassification()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var first = await FiscalDocumentVoidEndpoint.VoidAsync(documentId, ValidRequest(), service);
        var second = await FiscalDocumentVoidEndpoint.VoidAsync(documentId, ValidRequest(), service);

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal("idempotent_replay", second.ResultClassification);
        Assert.Equal("SI-00000001-A", second.FiscalDocumentNumber);
        Assert.Equal(1, repository.VoidWriteCount);
    }

    [Fact]
    public async Task ConflictingReplayMapsToConflict()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var first = await FiscalDocumentVoidEndpoint.VoidAsync(documentId, ValidRequest(), service);
        var second = await FiscalDocumentVoidEndpoint.VoidAsync(
            documentId,
            ValidRequest() with { ReasonText = "Different safe reason" },
            service);

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal("fiscal_document_void_idempotency_conflict", second.Code);
        Assert.Equal("conflict", second.ResultClassification);
        Assert.Equal(StatusCodes.Status409Conflict, second.HttpStatusCode);
    }

    [Fact]
    public async Task UnknownFiscalDocumentMapsToNotFound()
    {
        var service = new FiscalDocumentVoidService(new RecordingFiscalDocumentRepository());

        var response = await FiscalDocumentVoidEndpoint.VoidAsync(Guid.NewGuid(), ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("fiscal_document_not_found", response.Code);
        Assert.Equal(StatusCodes.Status404NotFound, response.HttpStatusCode);
    }

    [Fact]
    public async Task MissingIdempotencyKeyMapsToBadRequest()
    {
        var service = new FiscalDocumentVoidService(new RecordingFiscalDocumentRepository());

        var response = await FiscalDocumentVoidEndpoint.VoidAsync(
            Guid.NewGuid(),
            ValidRequest() with { IdempotencyKey = " " },
            service);

        Assert.False(response.Succeeded);
        Assert.Equal("missing_idempotency_key", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task InvalidReasonCodeMapsToBadRequest()
    {
        var service = new FiscalDocumentVoidService(new RecordingFiscalDocumentRepository());

        var response = await FiscalDocumentVoidEndpoint.VoidAsync(
            Guid.NewGuid(),
            ValidRequest() with { ReasonCode = "not_approved" },
            service);

        Assert.False(response.Succeeded);
        Assert.Equal("invalid_reason_code", response.Code);
        Assert.Equal(StatusCodes.Status400BadRequest, response.HttpStatusCode);
    }

    [Fact]
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new FiscalDocumentVoidService(new PersistenceNotConfiguredFiscalDocumentRepository());

        var response = await FiscalDocumentVoidEndpoint.VoidAsync(Guid.NewGuid(), ValidRequest(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal("rejected", response.ResultClassification);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
    }

    [Fact]
    public void DependencyInjectionRegistersVoidService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<FiscalDocumentVoidService>();
        Assert.NotNull(service);
    }

    [Fact]
    public void EndpointRouteBuilderMapsVoidRoute()
    {
        var source = File.ReadAllText(FindApiSourcePath("FiscalDocumentEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("MapPost(\"/{fiscalDocumentId:guid}/void\"", source, StringComparison.Ordinal);
        Assert.Contains("FiscalDocumentVoidEndpoint", source, StringComparison.Ordinal);
        Assert.Contains("Results.Json(response, statusCode: response.HttpStatusCode)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void VoidDtosDoNotExposeForbiddenSideEffectFields()
    {
        var forbiddenNames = new[] { "HikCentral", "PaymentProvider", "ExitAuthorization", "GateExecution", "Pdf", "Html", "Qr" };
        var dtoTypes = new[] { typeof(VoidFiscalDocumentRequest), typeof(VoidFiscalDocumentResponse) };

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

    private static VoidFiscalDocumentRequest ValidRequest() =>
        new(
            "void-key-001",
            "operator_error",
            "Safe correction note",
            "operator-ref-001",
            DateTimeOffset.Parse("2026-07-09T04:30:00Z"),
            "corr-void-001",
            "central-pms",
            DateOnly.Parse("2026-07-09"));

    private static string FindApiSourcePath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "ExitPass.PosServer.Api",
                "FiscalDocuments",
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }

    private sealed class RecordingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        private readonly Dictionary<Guid, DocumentState> documents = [];
        private readonly Dictionary<(string Scope, string Key), (string Hash, Guid FiscalDocumentId)> idempotencyRecords = [];

        public int VoidWriteCount { get; private set; }

        public void AddIssuedDocument(Guid fiscalDocumentId, string fiscalDocumentNumber, long fiscalSequenceValue) =>
            documents.Add(fiscalDocumentId, new DocumentState(fiscalDocumentId, fiscalDocumentNumber, fiscalSequenceValue));

        public Task<FiscalDocumentVoidPersistenceResult> VoidAsync(
            FiscalDocumentVoidCommand command,
            FiscalDocumentVoidIdempotency idempotency,
            CancellationToken cancellationToken)
        {
            if (!documents.TryGetValue(command.FiscalDocumentId, out var document))
            {
                throw new FiscalDocumentVoidNotFoundException();
            }

            var key = (idempotency.Scope, idempotency.Key);
            if (idempotencyRecords.TryGetValue(key, out var existing))
            {
                if (!string.Equals(existing.Hash, idempotency.SemanticRequestHash, StringComparison.Ordinal))
                {
                    throw new FiscalDocumentVoidIdempotencyConflictException();
                }

                return Task.FromResult(FiscalDocumentVoidPersistenceResult.Replayed(document.ToRecord(command)));
            }

            if (document.Voided)
            {
                throw new FiscalDocumentVoidIdempotencyConflictException();
            }

            VoidWriteCount++;
            document.MarkVoided(command, idempotency);
            idempotencyRecords.Add(key, (idempotency.SemanticRequestHash, command.FiscalDocumentId));
            return Task.FromResult(FiscalDocumentVoidPersistenceResult.NewlyVoided(document.ToRecord(command)));
        }

        public Task<FiscalDocumentPersistenceResult> CreateAsync(
            FiscalDocumentDraft draft,
            FiscalIssuanceIdempotency idempotency,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        private sealed class DocumentState
        {
            public DocumentState(Guid fiscalDocumentId, string fiscalDocumentNumber, long fiscalSequenceValue)
            {
                FiscalDocumentId = fiscalDocumentId;
                FiscalDocumentNumber = fiscalDocumentNumber;
                FiscalSequenceValue = fiscalSequenceValue;
            }

            public Guid FiscalDocumentId { get; }
            public string FiscalDocumentNumber { get; }
            public long FiscalSequenceValue { get; }
            public bool Voided { get; private set; }
            private DateTimeOffset? VoidedAt { get; set; }
            private string? ReasonCode { get; set; }
            private string? ReasonText { get; set; }
            private string? RequestedByRef { get; set; }
            private string? IdempotencyKey { get; set; }
            private string? CorrelationId { get; set; }

            public void MarkVoided(FiscalDocumentVoidCommand command, FiscalDocumentVoidIdempotency idempotency)
            {
                Voided = true;
                VoidedAt = DateTimeOffset.Parse("2026-07-09T04:31:00Z");
                ReasonCode = command.ReasonCode;
                ReasonText = command.ReasonText;
                RequestedByRef = command.RequestedByRef;
                IdempotencyKey = idempotency.Key;
                CorrelationId = command.CorrelationId;
            }

            public FiscalDocumentVoidRecord ToRecord(FiscalDocumentVoidCommand command) =>
                new(
                    FiscalDocumentId,
                    FiscalDocumentNumber,
                    FiscalSequenceValue,
                    "voided",
                    "recorded",
                    VoidedAt ?? DateTimeOffset.Parse("2026-07-09T04:31:00Z"),
                    ReasonCode ?? command.ReasonCode,
                    ReasonText ?? command.ReasonText,
                    RequestedByRef ?? command.RequestedByRef,
                    IdempotencyKey ?? command.IdempotencyKey,
                    CorrelationId ?? command.CorrelationId);
        }
    }
}
