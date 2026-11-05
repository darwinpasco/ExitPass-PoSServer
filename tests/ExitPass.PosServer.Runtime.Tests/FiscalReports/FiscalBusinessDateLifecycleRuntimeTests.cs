using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalBusinessDateLifecycleRuntimeTests
{
    private static readonly Guid Site = Guid.Parse("71000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("71000000-0000-4000-8000-000000000002");

    [Fact]
    public async Task OpenRequiresExplicitConfirmationAndAnImmutableDateLabel()
    {
        var repository = new StubRepository(Snapshot("NO_OPEN_PERIOD"));
        var service = new FiscalBusinessDateLifecycleService(repository);

        var unconfirmed = await service.OpenAsync(Command() with { ConfirmOpen = false });
        var missingLabel = await service.OpenAsync(Command() with { BusinessDateLabel = default });
        var opened = await service.OpenAsync(Command());

        Assert.Equal(OpenFiscalBusinessDateOutcome.InvalidRequest, unconfirmed.Outcome);
        Assert.Equal(OpenFiscalBusinessDateOutcome.InvalidRequest, missingLabel.Outcome);
        Assert.Equal(OpenFiscalBusinessDateOutcome.Opened, opened.Outcome);
        Assert.Equal(1, repository.OpenCalls);
    }

    [Fact]
    public async Task ReadDoesNotDeriveOrRollThePeriodFromTheClock()
    {
        var open = Snapshot("OPEN") with { DatabaseNow = DateTimeOffset.Parse("2026-11-08T23:30:00Z") };
        var repository = new StubRepository(open);

        var result = await new FiscalBusinessDateLifecycleService(repository)
            .ReadAsync(Site, Identity, "php");

        Assert.Equal("OPEN", result.State);
        Assert.Equal(new DateOnly(2026, 11, 5), result.ActivePeriod!.BusinessDateLabel);
        Assert.Equal("PHP", repository.LastCurrency);
    }

    private static OpenFiscalBusinessDateCommand Command() => new(
        "open-20261105", Site, Identity, "PHP", new DateOnly(2026, 11, 5), true,
        "operator", "central-pms", "correlation");

    private static FiscalBusinessDateLifecycleSnapshot Snapshot(string state) => new(
        Site, Identity, "PHP", "Asia/Manila", state, new DateOnly(2026, 11, 6),
        state == "OPEN" ? new(Guid.NewGuid(), new DateOnly(2026, 11, 5), "OPEN", DateTimeOffset.Parse("2026-11-04T23:00:00Z"), null, 1, 2, 3) : null,
        null, DateTimeOffset.Parse("2026-11-05T12:00:00Z"));

    private sealed class StubRepository(FiscalBusinessDateLifecycleSnapshot snapshot) : IFiscalBusinessDateLifecycleRepository
    {
        public int OpenCalls { get; private set; }
        public string? LastCurrency { get; private set; }

        public Task<FiscalBusinessDateLifecycleSnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode, CancellationToken cancellationToken = default)
        {
            LastCurrency = currencyCode;
            return Task.FromResult(snapshot);
        }

        public Task<OpenFiscalBusinessDateResult> OpenAsync(OpenFiscalBusinessDateCommand command, CancellationToken cancellationToken = default)
        {
            OpenCalls++;
            return Task.FromResult(new OpenFiscalBusinessDateResult(OpenFiscalBusinessDateOutcome.Opened, snapshot));
        }
    }
}
