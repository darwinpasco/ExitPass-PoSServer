using System.Reflection;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class FiscalDocumentVoidServiceTests
{
    [Fact]
    public async Task NewlyVoidedDocumentSucceedsAndKeepsOriginalNumbering()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var result = await service.VoidAsync(ValidCommand(documentId));

        Assert.True(result.Succeeded);
        Assert.Equal("newly_voided", result.ResultClassification);
        Assert.Equal("voided", result.Record!.FiscalDocumentStatus);
        Assert.Equal("recorded", result.Record.VoidStatus);
        Assert.Equal("SI-00000001-A", result.Record.FiscalDocumentNumber);
        Assert.Equal(1, result.Record.FiscalSequenceValue);
        Assert.Equal("operator_error", result.Record.VoidReasonCode);
        Assert.Equal(0, repository.SequenceAllocationCount);
    }

    [Fact]
    public async Task ReplayWithSameIdempotencyKeyAndSemanticHashReturnsIdempotentReplay()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var first = await service.VoidAsync(ValidCommand(documentId));
        var second = await service.VoidAsync(ValidCommand(documentId) with { RequestedAt = DateTimeOffset.Parse("2026-07-09T05:30:00Z") });

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal("idempotent_replay", second.ResultClassification);
        Assert.Equal(first.Record!.FiscalDocumentId, second.Record!.FiscalDocumentId);
        Assert.Equal(first.Record.FiscalDocumentNumber, second.Record.FiscalDocumentNumber);
        Assert.Equal(first.Record.FiscalSequenceValue, second.Record.FiscalSequenceValue);
        Assert.Equal(1, repository.VoidWriteCount);
        Assert.Equal(0, repository.SequenceAllocationCount);
    }

    [Fact]
    public async Task SameIdempotencyKeyWithDifferentSemanticFactsFailsClosed()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var first = await service.VoidAsync(ValidCommand(documentId));
        var second = await service.VoidAsync(ValidCommand(documentId) with { ReasonText = "Different safe reason" });

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.IdempotencyConflict, second.ErrorCode);
        Assert.Equal("conflict", second.ResultClassification);
        Assert.Equal(1, repository.VoidWriteCount);
    }

    [Fact]
    public async Task AlreadyVoidedDocumentWithDifferentIdempotencySurfaceFailsClosed()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddIssuedDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var first = await service.VoidAsync(ValidCommand(documentId));
        var second = await service.VoidAsync(ValidCommand(documentId) with { IdempotencyKey = "void-key-002" });

        Assert.True(first.Succeeded);
        Assert.False(second.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.IdempotencyConflict, second.ErrorCode);
        Assert.Equal("conflict", second.ResultClassification);
    }

    [Fact]
    public async Task UnknownFiscalDocumentReturnsNotFound()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentVoidService(repository);

        var result = await service.VoidAsync(ValidCommand(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa")));

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.FiscalDocumentNotFound, result.ErrorCode);
    }

    [Fact]
    public async Task InvalidReasonCodeIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentVoidService(repository);

        var result = await service.VoidAsync(ValidCommand(Guid.NewGuid()) with { ReasonCode = "not_approved" });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.InvalidReasonCode, result.ErrorCode);
        Assert.Equal(0, repository.VoidWriteCount);
    }

    [Fact]
    public async Task MissingIdempotencyKeyIsRejectedBeforePersistence()
    {
        var repository = new RecordingFiscalDocumentRepository();
        var service = new FiscalDocumentVoidService(repository);

        var result = await service.VoidAsync(ValidCommand(Guid.NewGuid()) with { IdempotencyKey = " " });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.MissingIdempotencyKey, result.ErrorCode);
        Assert.Equal(0, repository.VoidWriteCount);
    }

    [Fact]
    public async Task InvalidStateTransitionReturnsConflictPosture()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var repository = new RecordingFiscalDocumentRepository();
        repository.AddNonVoidableDocument(documentId, "SI-00000001-A", 1);
        var service = new FiscalDocumentVoidService(repository);

        var result = await service.VoidAsync(ValidCommand(documentId));

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalDocumentVoidErrorCode.InvalidStateTransition, result.ErrorCode);
        Assert.Equal("rejected", result.ResultClassification);
    }

    [Fact]
    public void SemanticHashIgnoresRequestedAtButIncludesMeaningfulVoidFacts()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var first = ValidCommand(documentId);
        var sameFactsDifferentTime = first with { RequestedAt = first.RequestedAt.AddHours(1) };
        var differentReason = first with { ReasonCode = "customer_request" };

        Assert.Equal(
            FiscalDocumentVoidSemanticRequestHasher.Hash(first),
            FiscalDocumentVoidSemanticRequestHasher.Hash(sameFactsDifferentTime));
        Assert.NotEqual(
            FiscalDocumentVoidSemanticRequestHasher.Hash(first),
            FiscalDocumentVoidSemanticRequestHasher.Hash(differentReason));
    }

    [Fact]
    public void VoidModelsDoNotExposeForbiddenExternalDependencies()
    {
        var modelTypes = new[]
        {
            typeof(FiscalDocumentVoidCommand),
            typeof(FiscalDocumentVoidRecord),
            typeof(FiscalDocumentVoidService)
        };
        var forbiddenNames = new[] { "HikCentral", "PaymentProvider", "ExitAuthorization", "GateExecution", "Pdf", "Html", "Qr" };

        foreach (var type in modelTypes)
        {
            AssertDoesNotExpose(type, forbiddenNames);
        }
    }

    private static FiscalDocumentVoidCommand ValidCommand(Guid fiscalDocumentId) =>
        new(
            fiscalDocumentId,
            "void-key-001",
            "operator_error",
            "Safe correction note",
            "operator-ref-001",
            DateTimeOffset.Parse("2026-07-09T04:30:00Z"),
            "corr-void-001",
            "central-pms",
            DateOnly.Parse("2026-07-09"));

    private static void AssertDoesNotExpose(Type type, string[] forbiddenNames)
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

    private sealed class RecordingFiscalDocumentRepository : IFiscalDocumentRepository
    {
        private readonly Dictionary<Guid, DocumentState> documents = [];
        private readonly Dictionary<(string Scope, string Key), (string Hash, Guid FiscalDocumentId)> idempotencyRecords = [];

        public int VoidWriteCount { get; private set; }
        public int SequenceAllocationCount { get; private set; }

        public void AddIssuedDocument(Guid fiscalDocumentId, string fiscalDocumentNumber, long fiscalSequenceValue) =>
            documents.Add(fiscalDocumentId, new DocumentState(fiscalDocumentId, fiscalDocumentNumber, fiscalSequenceValue, true));

        public void AddNonVoidableDocument(Guid fiscalDocumentId, string fiscalDocumentNumber, long fiscalSequenceValue) =>
            documents.Add(fiscalDocumentId, new DocumentState(fiscalDocumentId, fiscalDocumentNumber, fiscalSequenceValue, false));

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

            if (!document.Voidable)
            {
                throw new FiscalDocumentVoidInvalidStateException("Only issued or recorded fiscal documents can be voided.");
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
            public DocumentState(Guid fiscalDocumentId, string fiscalDocumentNumber, long fiscalSequenceValue, bool voidable)
            {
                FiscalDocumentId = fiscalDocumentId;
                FiscalDocumentNumber = fiscalDocumentNumber;
                FiscalSequenceValue = fiscalSequenceValue;
                Voidable = voidable;
            }

            public Guid FiscalDocumentId { get; }
            public string FiscalDocumentNumber { get; }
            public long FiscalSequenceValue { get; }
            public bool Voidable { get; }
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
