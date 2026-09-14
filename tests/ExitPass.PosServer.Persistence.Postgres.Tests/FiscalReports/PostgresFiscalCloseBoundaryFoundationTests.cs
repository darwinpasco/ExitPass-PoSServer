using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresFiscalCloseBoundaryFoundationTests
{
    [Theory]
    [InlineData("2026-09-15T06:59:59+08:00", "2026-09-14", "2026-09-13T23:00:00+00:00", "2026-09-14T23:00:00+00:00")]
    [InlineData("2026-09-15T07:00:00+08:00", "2026-09-15", "2026-09-14T23:00:00+00:00", "2026-09-15T23:00:00+00:00")]
    public void PitxBusinessDayUsesSevenAmManilaHalfOpenBoundary(
        string fiscalTimestamp,
        string expectedBusinessDate,
        string expectedStart,
        string expectedEnd)
    {
        var window = PostgresFiscalCloseBoundaryCoordinator.ResolveBusinessDayWindow(
            DateTimeOffset.Parse(fiscalTimestamp),
            "Asia/Manila",
            new TimeOnly(7, 0));

        Assert.Equal(DateOnly.Parse(expectedBusinessDate), window.BusinessDayDate);
        Assert.Equal(DateTimeOffset.Parse(expectedStart), window.PeriodStartAt);
        Assert.Equal(DateTimeOffset.Parse(expectedEnd), window.PeriodEndAt);
    }

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
    public void FiscalCreationEnsuresOnlyTheTimestampContainingPeriodAndPreservesSnapshots()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalCloseBoundaryCoordinator.cs"));

        Assert.Contains("transaction_timestamp() >= period.period_start_at", source, StringComparison.Ordinal);
        Assert.Contains("transaction_timestamp() < period.period_end_at", source, StringComparison.Ordinal);
        Assert.Contains("site.reporting_timezone_name", source, StringComparison.Ordinal);
        Assert.Contains("site.business_day_cutoff_local_time", source, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO pos.fiscal_reporting_periods", source, StringComparison.Ordinal);
        Assert.Contains("ON CONFLICT (site_pos_server_id, fiscal_identity_id, period_start_at, period_end_at, currency_code)", source, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE OF site", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ZReadingRemainsManualAndRejectsAnUnfinishedPeriod()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalZReadingRepository.cs"));

        Assert.Contains("p.fiscal_reporting_period_id=@period", source, StringComparison.Ordinal);
        Assert.Contains("period.DatabaseNow < period.PeriodEndAt", source, StringComparison.Ordinal);
        Assert.Contains("FiscalZReadingOutcome.PeriodNotEnded", source, StringComparison.Ordinal);
        Assert.DoesNotContain("business_day_cutoff_local_time = transaction_timestamp", source, StringComparison.OrdinalIgnoreCase);
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
