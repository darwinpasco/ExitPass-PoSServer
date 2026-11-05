using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalZReadingRuntimeTests
{
    private static readonly Guid SiteId = Guid.Parse("78000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("78000000-0000-4000-8000-000000000002");
    private static readonly Guid PeriodId = Guid.Parse("78000000-0000-4000-8000-000000000003");
    private static readonly Guid ContractId = Guid.Parse("f6766f48-62f0-513f-b9eb-e61c2f3e8c66");

    [Fact]
    public void SemanticIdentityIsDeterministicAndExcludesPrincipalAndCorrelation()
    {
        var first = Command();
        var second = first with { RequestedByRef = "other", ServiceIdentityRef = "other-service", CorrelationId = "other-correlation" };
        Assert.Equal(FiscalZReadingSemanticRequestHasher.Compute(first, Period()), FiscalZReadingSemanticRequestHasher.Compute(second, Period()));
        var canonical = FiscalZReadingSemanticRequestHasher.CanonicalSource(first, Period());
        Assert.Contains(FiscalZReadingContract.SemanticHashVersion, canonical, StringComparison.Ordinal);
        Assert.Contains("atomic_open_to_closed", canonical, StringComparison.Ordinal);
        Assert.DoesNotContain("correlation", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("actor", canonical, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MaterialScopePeriodAndVersionChangeSemanticIdentity()
    {
        var hash = FiscalZReadingSemanticRequestHasher.Compute(Command(), Period());
        Assert.NotEqual(hash, FiscalZReadingSemanticRequestHasher.Compute(Command() with { CurrencyCode = "USD" }, Period() with { CurrencyCode = "USD" }));
        Assert.NotEqual(hash, FiscalZReadingSemanticRequestHasher.Compute(Command() with { FiscalReportingPeriodId = Guid.NewGuid() }, Period() with { FiscalReportingPeriodId = Guid.NewGuid() }));
        Assert.NotEqual(hash, FiscalZReadingSemanticRequestHasher.Compute(Command() with { ExpectedStateVersion = 2 }, Period()));
    }

    [Fact]
    public void TransitionHashCoversEveryCounterAndGtaTransitionValue()
    {
        var first = FiscalZReadingSemanticRequestHasher.ComputeTransition(new('a', 64), 1, 1, 2, 3, 100, 50, 150);
        var changed = FiscalZReadingSemanticRequestHasher.ComputeTransition(new('a', 64), 1, 1, 2, 3, 100, 51, 151);
        Assert.NotEqual(first, changed);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public async Task ServiceRejectsInvalidRequestsBeforeRepositoryAndPreservesReplay()
    {
        var repository = new StubRepository(new(FiscalZReadingOutcome.Replayed, Record()));
        var service = new FiscalZReadingService(repository);
        Assert.Equal(FiscalZReadingOutcome.InvalidRequest, (await service.CloseAsync(Command() with { ExpectedStateVersion = 0 })).Outcome);
        Assert.Equal(FiscalZReadingOutcome.InvalidRequest, (await service.CloseAsync(Command() with { CurrencyCode = "php" })).Outcome);
        Assert.Equal(FiscalZReadingOutcome.InvalidRequest, (await service.CloseAsync(Command() with { CurrencyCode = null! })).Outcome);
        Assert.Equal(0, repository.CloseCalls);
        Assert.Equal(FiscalZReadingOutcome.Replayed, (await service.CloseAsync(Command())).Outcome);
        Assert.Equal(1, repository.CloseCalls);
    }

    [Fact]
    public void ApprovedGtaTransitionIsCheckedAndResetRemainsUnchanged()
    {
        const long previousGta = 10_000;
        var aggregate = new FiscalXReadingAggregationService().Aggregate([Document()], [], "PHP");
        var resultingGta = checked(previousGta + aggregate.Amounts.NetSalesAmountMinorUnits);
        Assert.Equal(18_000, resultingGta);
        var snapshot = new FiscalZReadingCounterSnapshot(4, 4, 9, 10, previousGta, aggregate.Amounts.NetSalesAmountMinorUnits, resultingGta, 7, 8);
        Assert.Equal(snapshot.PreviousResetCounterValue, snapshot.ResultingResetCounterValue);
        Assert.Equal(snapshot.PreviousZCounterValue + 1, snapshot.ResultingZCounterValue);
    }

    private static FiscalXReadingSourceDocument Document() => new(Guid.NewGuid(), "sales_invoice", "recorded",
        DateTimeOffset.Parse("2026-08-03T01:00:00Z"), Guid.NewGuid(), 1, "SI-0001", "SI",
        [new("parking_fee", 10_000, 2_000, 0, 8_000, "PHP")], [new("cash", 8_000, "PHP")],
        [new("vat_exempt", false, 8_000, 0, "PHP")], [new("coupon", 2_000, 0, "PHP")], null);

    private static FiscalZReadingCommand Command() => new("z-operation", SiteId, IdentityId, "PHP", PeriodId, 1, "actor", "service", "correlation");
    private static FiscalZReadingPeriod Period() => new(PeriodId, ContractId, SiteId, IdentityId, new(2026, 8, 3),
        DateTimeOffset.Parse("2026-08-03T00:00:00Z"), DateTimeOffset.Parse("2026-08-04T00:00:00Z"),
        "Asia/Manila", new(0, 0), "PHP", 1, null, "open", DateTimeOffset.Parse("2026-08-04T01:00:00Z"));

    private static FiscalZReadingRecord Record() => new(Guid.NewGuid(), "Z-TEST", Guid.NewGuid(), "z-operation",
        "Z_READING", FiscalZReadingContract.ContractVersion, FiscalZReadingContract.SemanticHashVersion,
        SiteId, IdentityId, PeriodId, null, new(2026, 8, 3), DateTimeOffset.Parse("2026-08-03T00:00:00Z"),
        DateTimeOffset.Parse("2026-08-04T00:00:00Z"), "Asia/Manila", new(0, 0), "PHP",
        DateTimeOffset.Parse("2026-08-04T01:00:00Z"), DateTimeOffset.Parse("2026-08-04T01:00:00Z"),
        DateTimeOffset.Parse("2026-08-04T01:00:00Z"), 0, new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
        [], [], [], new(0, 0, 0, 1, 0, 0, 0, 1, 2), "CLOSED", "correlation", "Z-TEST", true);

    private sealed class StubRepository(FiscalZReadingResult result) : IFiscalZReadingRepository
    {
        public int CloseCalls { get; private set; }
        public Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default) { CloseCalls++; return Task.FromResult(result); }
        public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
