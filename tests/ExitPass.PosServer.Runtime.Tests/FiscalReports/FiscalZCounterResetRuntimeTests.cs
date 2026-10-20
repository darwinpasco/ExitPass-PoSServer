using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalZCounterResetRuntimeTests
{
    private static readonly Guid SiteId = Guid.Parse("7a000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("7a000000-0000-4000-8000-000000000002");

    [Fact]
    public void SemanticHashIsStableAcrossTransportFactsButBindsTheAdministratorAndReason()
    {
        var command = Command();

        Assert.Equal(
            FiscalZCounterResetSemanticHasher.Compute(command),
            FiscalZCounterResetSemanticHasher.Compute(command with
            {
                ServiceIdentityRef = "replacement-service",
                CorrelationId = "replacement-correlation"
            }));
        Assert.NotEqual(
            FiscalZCounterResetSemanticHasher.Compute(command),
            FiscalZCounterResetSemanticHasher.Compute(command with { RequestedByRef = "another-administrator" }));
        Assert.NotEqual(
            FiscalZCounterResetSemanticHasher.Compute(command),
            FiscalZCounterResetSemanticHasher.Compute(command with { Reason = "A different demonstration reason" }));
    }

    [Fact]
    public async Task ValidResetIsNormalizedAndForwardedOnce()
    {
        var repository = new StubRepository();
        var service = new FiscalZCounterResetService(repository);

        var result = await service.ResetAsync(Command() with
        {
            OperationKey = "  reset-operation  ",
            CurrencyCode = "php",
            Reason = "  BIR demonstration  "
        });

        Assert.Equal(FiscalZCounterResetOutcome.Created, result.Outcome);
        Assert.Equal(1, repository.ResetCalls);
        Assert.Equal("reset-operation", repository.LastCommand!.OperationKey);
        Assert.Equal("PHP", repository.LastCommand.CurrencyCode);
        Assert.Equal("BIR demonstration", repository.LastCommand.Reason);
    }

    [Theory]
    [InlineData(0, "Valid reason")]
    [InlineData(1, "")]
    [InlineData(1, "line one\nline two")]
    public async Task InvalidStateVersionOrReasonFailsBeforePersistence(long stateVersion, string reason)
    {
        var repository = new StubRepository();
        var service = new FiscalZCounterResetService(repository);

        var result = await service.ResetAsync(Command() with
        {
            ExpectedStateVersion = stateVersion,
            Reason = reason
        });

        Assert.Equal(FiscalZCounterResetOutcome.InvalidRequest, result.Outcome);
        Assert.Equal(0, repository.ResetCalls);
    }

    private static FiscalZCounterResetCommand Command() => new(
        "reset-operation", SiteId, IdentityId, "PHP", 32, "BIR demonstration",
        "fiscal-administrator", "central-pms", "reset-correlation");

    private sealed class StubRepository : IFiscalZCounterResetRepository
    {
        public int ResetCalls { get; private set; }
        public FiscalZCounterResetCommand? LastCommand { get; private set; }

        public Task<FiscalZCounterResetResult> ReadStateAsync(
            FiscalZCounterStateQuery query,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalZCounterResetResult(FiscalZCounterResetOutcome.NotFound));

        public Task<FiscalZCounterResetResult> ResetAsync(
            FiscalZCounterResetCommand command,
            CancellationToken cancellationToken = default)
        {
            ResetCalls++;
            LastCommand = command;
            return Task.FromResult(new FiscalZCounterResetResult(FiscalZCounterResetOutcome.Created));
        }
    }
}
