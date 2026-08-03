using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresFiscalXReadingRepositoryTests
{
    [Fact]
    public void RepositoryUsesRepeatableReadAtomicSnapshotAndStoredReadback()
    {
        var source = File.ReadAllText(FindSource());
        Assert.Contains("IsolationLevel.RepeatableRead", source, StringComparison.Ordinal);
        Assert.Contains("pg_advisory_lock", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_report_requests", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_report_scopes", source, StringComparison.Ordinal);
        Assert.Contains("pos.x_z_reports", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_report_tender_breakdowns", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_report_discount_breakdowns", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_report_fiscal_number_ranges", source, StringComparison.Ordinal);
        Assert.Contains("ReadRecordAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.fiscal_documents", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("pos.fiscal_z_counter_snapshots", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("report_snapshot_context", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SourceQueriesUseHalfOpenWindowAndGovernedFirstClassTables()
    {
        var source = File.ReadAllText(FindSource());
        Assert.Contains("d.created_at >= @period_start_at AND d.created_at < @effective_end", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_document_applied_statutory_facts", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_sequence_gap_audit", source, StringComparison.Ordinal);
        Assert.Contains("status.code_key = 'open'", source, StringComparison.Ordinal);
        Assert.DoesNotContain("document_context", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tender_context", source, StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresFiscalXReadingRepository.cs");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Postgres X Reading repository source was not found.");
    }
}
