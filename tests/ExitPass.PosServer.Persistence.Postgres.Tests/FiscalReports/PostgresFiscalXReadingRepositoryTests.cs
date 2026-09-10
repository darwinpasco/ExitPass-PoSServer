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
    public void SourceQueriesUseAssignedPeriodAndActualObservationCutoffWithGovernedFirstClassTables()
    {
        var source = File.ReadAllText(FindSource());
        Assert.Contains("d.fiscal_reporting_period_id = @fiscal_reporting_period_id", source, StringComparison.Ordinal);
        Assert.Contains("d.currency_code = @currency_code AND d.created_at < @observed_at", source, StringComparison.Ordinal);
        Assert.DoesNotContain("d.created_at >= @period_start_at", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_document_applied_statutory_facts", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_sequence_gap_audit", source, StringComparison.Ordinal);
        Assert.Contains("status.code_key = 'open'", source, StringComparison.Ordinal);
        Assert.DoesNotContain("document_context", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tender_context", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ZCloseAssignmentValidationUsesPeriodBusinessDateAndBindsItsParameter()
    {
        var source = File.ReadAllText(FindSource("PostgresFiscalZReadingRepository.cs"));
        Assert.Contains("d.business_day_date=@business", source, StringComparison.Ordinal);
        Assert.Contains("d.fiscal_reporting_period_id=@period", source, StringComparison.Ordinal);
        Assert.Contains("c.Parameters.AddWithValue(\"business\",p.BusinessDayDate)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("d.created_at>=@start AND d.created_at<@end", source, StringComparison.Ordinal);
    }

    private static string FindSource(string fileName = "PostgresFiscalXReadingRepository.cs")
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", fileName);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("Postgres X Reading repository source was not found.");
    }
}
