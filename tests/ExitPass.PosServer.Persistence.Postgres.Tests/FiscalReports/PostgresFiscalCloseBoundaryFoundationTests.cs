using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresFiscalCloseBoundaryFoundationTests
{
    [Fact]
    public void AdvisoryKeyIsDeterministicAndScopeIsolated()
    {
        var site = Guid.Parse("76000000-0000-4000-8000-000000000001");
        var identity = Guid.Parse("76000000-0000-4000-8000-000000000002");

        var key = PostgresFiscalCloseBoundaryCoordinator.DeriveAdvisoryKey(site, identity, "PHP");

        Assert.Equal(key, PostgresFiscalCloseBoundaryCoordinator.DeriveAdvisoryKey(site, identity, "php"));
        Assert.NotEqual(key, PostgresFiscalCloseBoundaryCoordinator.DeriveAdvisoryKey(Guid.NewGuid(), identity, "PHP"));
        Assert.NotEqual(key, PostgresFiscalCloseBoundaryCoordinator.DeriveAdvisoryKey(site, Guid.NewGuid(), "PHP"));
        Assert.NotEqual(key, PostgresFiscalCloseBoundaryCoordinator.DeriveAdvisoryKey(site, identity, "USD"));
    }

    [Fact]
    public void FiscalCreationAndVoidUseSameBoundaryBeforeSourceMutation()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalDocuments", "PostgresFiscalDocumentRepository.cs"));
        var acquire = source.IndexOf("PostgresFiscalCloseBoundaryCoordinator.AcquireAsync", StringComparison.Ordinal);
        var allocation = source.IndexOf("AllocateFiscalNumberAsync", acquire, StringComparison.Ordinal);
        var insert = source.IndexOf("PostgresFiscalDocumentSql.InsertFiscalDocument", acquire, StringComparison.Ordinal);

        Assert.True(acquire >= 0 && acquire < allocation && allocation < insert);
        Assert.True(source.LastIndexOf("PostgresFiscalCloseBoundaryCoordinator.AcquireAsync", StringComparison.Ordinal) > acquire);
        Assert.Contains("LockAndValidateAssignedPeriodAsync", source, StringComparison.Ordinal);
        Assert.Contains("ReadFiscalDocumentReportingScopeAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalStateRepositoryUsesReadCommittedTransactionLockAndImmutableEvidence()
    {
        var repository = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalZCloseStateRepository.cs"));
        Assert.Contains("IsolationLevel.ReadCommitted", repository, StringComparison.Ordinal);
        Assert.Contains("PostgresFiscalCloseBoundaryCoordinator.AcquireAsync", repository, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_z_close_states", repository, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_z_close_state_transitions", repository, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_z_close_state_transition_values", repository, StringComparison.Ordinal);
        Assert.DoesNotContain("json", repository, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TransitionFoundationLocksStateChecksPredecessorAndAppliesExpectedVersion()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalZCloseTransitionFoundation.cs"));

        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
        Assert.Contains("period_sequence - 1", source, StringComparison.Ordinal);
        Assert.Contains("expected_prior_period_id", source, StringComparison.Ordinal);
        Assert.Contains("apply_fiscal_z_close_state_transition", source, StringComparison.Ordinal);
        Assert.Contains("expected_state_version", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.fiscal_z_close_states", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSource(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(string.Join('/', parts));
    }
}
