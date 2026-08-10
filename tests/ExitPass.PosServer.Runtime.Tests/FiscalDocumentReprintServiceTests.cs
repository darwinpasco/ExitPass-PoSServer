using ExitPass.PosServer.Runtime.FiscalDocuments;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests;

public sealed class FiscalDocumentReprintServiceTests
{
    [Fact]
    public async Task NormalizesAndHashesGovernedRequestWithoutCorrelationIdentity()
    {
        var repository = new RecordingRepository();
        var service = new FiscalDocumentReprintService(repository);
        var command = Valid() with { CurrencyCode = " php ", ReasonCode = " OPERATOR_REQUEST ", OperationKey = " op-1 " };
        var result = await service.RecordAsync(command);
        Assert.Equal(FiscalDocumentReprintOutcome.Created, result.Outcome);
        Assert.Equal("PHP", repository.Command!.CurrencyCode);
        Assert.Equal("operator_request", repository.Command.ReasonCode);
        Assert.Equal("op-1", repository.Command.OperationKey);
        Assert.Equal(64, repository.SemanticHash!.Length);
        Assert.Equal(FiscalDocumentReprintSemanticRequestHasher.Hash(repository.Command), repository.SemanticHash);
        Assert.Equal(
            FiscalDocumentReprintSemanticRequestHasher.Hash(repository.Command),
            FiscalDocumentReprintSemanticRequestHasher.Hash(repository.Command with { CorrelationReference = "another-correlation" }));
    }

    [Theory]
    [InlineData("other")]
    [InlineData("")]
    [InlineData("raw_id")]
    public async Task UnsupportedReasonFailsBeforePersistence(string reason)
    {
        var repository = new RecordingRepository();
        var result = await new FiscalDocumentReprintService(repository).RecordAsync(Valid() with { ReasonCode = reason });
        Assert.Equal(FiscalDocumentReprintOutcome.InvalidRequest, result.Outcome);
        Assert.Null(repository.Command);
    }

    [Fact]
    public async Task MissingOrUnsafeReferencesFailBeforePersistence()
    {
        var repository = new RecordingRepository();
        var service = new FiscalDocumentReprintService(repository);
        Assert.Equal(FiscalDocumentReprintOutcome.InvalidRequest, (await service.RecordAsync(Valid() with { OperationKey = " " })).Outcome);
        Assert.Equal(FiscalDocumentReprintOutcome.InvalidRequest, (await service.RecordAsync(Valid() with { ActorReference = "actor\nforged" })).Outcome);
        Assert.Null(repository.Command);
    }

    [Fact]
    public void SemanticHashChangesForEveryGovernedFiscalFact()
    {
        var command = Valid();
        var original = FiscalDocumentReprintSemanticRequestHasher.Hash(command);
        Assert.NotEqual(original, FiscalDocumentReprintSemanticRequestHasher.Hash(command with { FiscalDocumentId = Guid.NewGuid() }));
        Assert.NotEqual(original, FiscalDocumentReprintSemanticRequestHasher.Hash(command with { SitePosServerId = Guid.NewGuid() }));
        Assert.NotEqual(original, FiscalDocumentReprintSemanticRequestHasher.Hash(command with { FiscalIdentityId = Guid.NewGuid() }));
        Assert.NotEqual(original, FiscalDocumentReprintSemanticRequestHasher.Hash(command with { CurrencyCode = "USD" }));
        Assert.NotEqual(original, FiscalDocumentReprintSemanticRequestHasher.Hash(command with { ReasonCode = "audit_request" }));
    }

    private static FiscalDocumentReprintCommand Valid() => new(
        Guid.Parse("83000000-0000-4000-8000-000000000001"),
        Guid.Parse("83000000-0000-4000-8000-000000000002"),
        Guid.Parse("83000000-0000-4000-8000-000000000003"),
        "PHP", "reprint-operation-001", "operator_request",
        "operator-service", "pos-server", "correlation-001");

    private sealed class RecordingRepository : IFiscalDocumentReprintRepository
    {
        public FiscalDocumentReprintCommand? Command { get; private set; }
        public string? SemanticHash { get; private set; }
        public Task<FiscalDocumentReprintResult> RecordAsync(
            FiscalDocumentReprintCommand command, string semanticRequestHash, CancellationToken cancellationToken = default)
        {
            Command = command;
            SemanticHash = semanticRequestHash;
            return Task.FromResult(new FiscalDocumentReprintResult(FiscalDocumentReprintOutcome.Created));
        }
    }
}
