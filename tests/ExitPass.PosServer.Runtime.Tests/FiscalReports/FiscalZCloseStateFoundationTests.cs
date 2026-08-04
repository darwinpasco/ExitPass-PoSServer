using ExitPass.PosServer.Runtime.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Runtime.Tests.FiscalReports;

public sealed class FiscalZCloseStateFoundationTests
{
    private static readonly Guid SiteId = Guid.Parse("74000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("74000000-0000-4000-8000-000000000002");

    [Theory]
    [InlineData(FiscalPeriodMutationKind.Refund)]
    [InlineData(FiscalPeriodMutationKind.Return)]
    [InlineData(FiscalPeriodMutationKind.Adjustment)]
    [InlineData(FiscalPeriodMutationKind.ServiceCharge)]
    public void UnsupportedPeriodAffectingWritersFailClosed(FiscalPeriodMutationKind mutationKind)
    {
        var exception = Assert.Throws<FiscalCloseBoundaryException>(() =>
            FiscalPeriodMutationGuard.EnsureSupported(mutationKind));

        Assert.Equal(FiscalCloseBoundaryErrorCode.UnsupportedCrossPeriodMutation, exception.ErrorCode);
        Assert.DoesNotContain("table", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreationAndVoidAreTheOnlyApprovedFoundationWriters()
    {
        FiscalPeriodMutationGuard.EnsureSupported(FiscalPeriodMutationKind.FiscalDocumentCreate);
        FiscalPeriodMutationGuard.EnsureSupported(FiscalPeriodMutationKind.FiscalDocumentVoid);
    }

    [Fact]
    public void InitializationHashIsDeterministicAndExcludesAuditTransportFacts()
    {
        var command = Command();
        var changedAudit = command with
        {
            ActorReference = "another-actor",
            ServiceIdentityReference = "another-service",
            CorrelationReference = "another-correlation"
        };

        Assert.Equal(
            FiscalZCloseStateSemanticRequestHasher.Compute(command),
            FiscalZCloseStateSemanticRequestHasher.Compute(changedAudit));
        var canonical = FiscalZCloseStateSemanticRequestHasher.CanonicalSource(command);
        Assert.DoesNotContain("actor", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("correlation", canonical, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(FiscalZCloseStateContract.InitializationSemanticHashVersion, canonical, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("USD", 0, 0, 0)]
    [InlineData("PHP", 0, 1, 0)]
    [InlineData("PHP", 0, 0, 1)]
    public void MaterialInitializationFactsChangeSemanticHash(string currency, long reset, long z, long gta)
    {
        Assert.NotEqual(
            FiscalZCloseStateSemanticRequestHasher.Compute(Command()),
            FiscalZCloseStateSemanticRequestHasher.Compute(Command() with
            {
                CurrencyCode = currency,
                ResetCounterValue = reset,
                ZCounterValue = z,
                GrandTotalAmountMinorUnits = gta
            }));
    }

    [Fact]
    public async Task NewScopeRequiresExplicitZeroAndDoesNotCallRepositoryWhenInvalid()
    {
        var repository = new StubRepository();
        var service = new FiscalZCloseStateInitializationService(repository);

        var result = await service.InitializeAsync(Command() with { ZCounterValue = 1 });

        Assert.False(result.Succeeded);
        Assert.Equal(FiscalZCloseStateInitializationErrorCode.InvalidRequest, result.ErrorCode);
        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task VerifiedLegacyImportPreservesSuppliedVerifiedValues()
    {
        var repository = new StubRepository();
        var service = new FiscalZCloseStateInitializationService(repository);
        var command = Command() with
        {
            Provenance = FiscalZCloseStateContract.VerifiedLegacyImport,
            ResetCounterValue = 2,
            ZCounterValue = 17,
            GrandTotalAmountMinorUnits = 123_456
        };

        var result = await service.InitializeAsync(command);

        Assert.True(result.Succeeded);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(17, repository.LastCommand!.ZCounterValue);
        Assert.Equal(FiscalZCloseStateContract.InitializationSemanticHashVersion, repository.LastHashVersion);
    }

    private static InitializeFiscalZCloseStateCommand Command() => new(
        "z-state-init-operation", SiteId, IdentityId, "PHP",
        FiscalZCloseStateContract.ApprovedNewScopeZero, 0, 0, 0,
        "approved-design-authority-20260804", "actor-ref", "service-ref", "correlation-ref");

    private sealed class StubRepository : IFiscalZCloseStateRepository
    {
        public int Calls { get; private set; }
        public InitializeFiscalZCloseStateCommand? LastCommand { get; private set; }
        public string? LastHashVersion { get; private set; }

        public Task<FiscalZCloseStateInitializationResult> InitializeAsync(
            InitializeFiscalZCloseStateCommand command,
            string semanticRequestHash,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastCommand = command;
            LastHashVersion = semanticRequestHash.Length == 64
                ? FiscalZCloseStateContract.InitializationSemanticHashVersion
                : null;
            var state = new FiscalZCloseStateRecord(
                Guid.NewGuid(), command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode,
                FiscalZCloseStateContract.ContractVersion, command.ResetCounterValue, command.ZCounterValue,
                command.GrandTotalAmountMinorUnits, 1, null, null, command.Provenance,
                DateTimeOffset.UtcNow, command.ApprovalReference, command.OperationReference, DateTimeOffset.UtcNow);
            return Task.FromResult(FiscalZCloseStateInitializationResult.Initialized(state));
        }
    }
}
