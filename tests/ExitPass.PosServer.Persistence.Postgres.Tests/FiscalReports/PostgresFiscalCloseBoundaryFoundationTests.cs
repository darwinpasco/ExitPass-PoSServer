using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresFiscalCloseBoundaryFoundationTests
{
    [Theory]
    [InlineData("2026-10-20T06:59:59+08:00", "2026-10-19", "2026-10-18T23:00:00+00:00", "2026-10-19T23:00:00+00:00")]
    [InlineData("2026-10-20T07:00:00+08:00", "2026-10-20", "2026-10-19T23:00:00+00:00", "2026-10-20T23:00:00+00:00")]
    [InlineData("2026-10-21T06:59:59+08:00", "2026-10-20", "2026-10-19T23:00:00+00:00", "2026-10-20T23:00:00+00:00")]
    [InlineData("2026-10-21T07:00:00+08:00", "2026-10-21", "2026-10-20T23:00:00+00:00", "2026-10-21T23:00:00+00:00")]
    [InlineData("2026-10-21T07:43:18+08:00", "2026-10-21", "2026-10-20T23:00:00+00:00", "2026-10-21T23:00:00+00:00")]
    [InlineData("2026-10-20T23:48:00+00:00", "2026-10-21", "2026-10-20T23:00:00+00:00", "2026-10-21T23:00:00+00:00")]
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
    public void FiscalCreationUsesOnlyTheExplicitOpenPeriodAndDoesNotCreateOrRollIt()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalCloseBoundaryCoordinator.cs"));

        Assert.Contains("period.period_status_code_id = '1a6f7021-bc84-5c01-afaa-c5d6685633c8'", source, StringComparison.Ordinal);
        Assert.Contains("period.period_end_at IS NULL", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO pos.fiscal_reporting_periods", source, StringComparison.Ordinal);
        Assert.DoesNotContain("EnsureCurrentOpenPeriodAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualOpenUsesDatabaseTimeUniqueLabelAndDurableIdempotencyEvidence()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalBusinessDateLifecycleRepository.cs"));

        Assert.Contains("transaction_timestamp(),NULL", source, StringComparison.Ordinal);
        Assert.Contains("command.BusinessDateLabel != before.ProposedNextBusinessDateLabel", source, StringComparison.Ordinal);
        Assert.Contains("fiscal_business_date_open_operations", source, StringComparison.Ordinal);
        var applyOrder = File.ReadAllText(FindSource("db", "rebuild", "pos_sql_apply_order.txt"));
        Assert.Contains("db/state/tables/pos.fiscal_business_date_open_operations.sql", applyOrder, StringComparison.Ordinal);
        Assert.Contains("semantic_request_hash", source, StringComparison.Ordinal);
        Assert.Contains("before.LastClosedPeriod is { ZReadingReference: null }", source, StringComparison.Ordinal);
        Assert.DoesNotContain("business_day_cutoff_local_time +", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ManualLifecycleMigrationReleasesSnapshotForeignKeyBeforeNullingActualEnd()
    {
        var source = File.ReadAllText(FindSource("db", "scripts", "pos-manual-fiscal-business-date-lifecycle-migration.sql"));
        var dropForeignKey = source.IndexOf("DROP CONSTRAINT IF EXISTS fk_x_z_reports__period_identity", StringComparison.Ordinal);
        var dropNotNull = source.IndexOf("ALTER COLUMN period_end_at DROP NOT NULL", StringComparison.Ordinal);
        var convertActive = source.IndexOf("SET legacy_scheduled_period_end_at = period_end_at", StringComparison.Ordinal);

        Assert.True(dropForeignKey >= 0 && dropForeignKey < convertActive);
        Assert.True(dropNotNull >= 0 && dropNotNull < convertActive);
        Assert.Contains("period_end_at = NULL", source, StringComparison.Ordinal);
        Assert.Contains("NOT is_manual_lifecycle OR period_end_at = closed_at", source, StringComparison.Ordinal);
        Assert.Contains("pos-server-fiscal-report-request:sha256:v2-manual-close", source, StringComparison.Ordinal);
        Assert.Contains("ux_fiscal_reporting_periods__business_date_label", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ZReadingManualCloseTransitionsOpenThroughClosingAndUsesDatabaseCommitTime()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalZReadingRepository.cs"));

        Assert.Contains("p.fiscal_reporting_period_id=@period", source, StringComparison.Ordinal);
        Assert.Contains("MarkPeriodClosingAsync", source, StringComparison.Ordinal);
        Assert.Contains("EnsureNoUnresolvedFiscalCreationAsync", source, StringComparison.Ordinal);
        Assert.Contains("period_end_at=@at,closed_at=@at", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FiscalZReadingOutcome.PeriodNotEnded", source, StringComparison.Ordinal);
        Assert.DoesNotContain("business_day_cutoff_local_time = transaction_timestamp", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HistorySelectsOnlyTheExplicitOpenPeriodWithoutClockRollover()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalReportingHistoryRepository.cs"));

        Assert.Contains("status.code_key='open'", source, StringComparison.Ordinal);
        Assert.Contains("p.period_end_at IS NULL", source, StringComparison.Ordinal);
        Assert.Contains("ORDER BY p.period_sequence DESC", source, StringComparison.Ordinal);
        Assert.Contains("LIMIT 1", source, StringComparison.Ordinal);
        Assert.DoesNotContain("fiscal_reporting_period_ambiguous", source, StringComparison.Ordinal);
        Assert.Contains("kind.code_key IN ('x_reading','z_reading')", source, StringComparison.Ordinal);
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
