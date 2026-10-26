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
        Assert.Contains("p.period_start_at <= @observed_at", source, StringComparison.Ordinal);
        Assert.Contains("@observed_at < p.period_end_at", source, StringComparison.Ordinal);
        Assert.DoesNotContain("p.reporting_timezone_name = site.reporting_timezone_name", source, StringComparison.Ordinal);
        Assert.DoesNotContain("p.business_day_cutoff_local_time = site.business_day_cutoff_local_time", source, StringComparison.Ordinal);
        Assert.DoesNotContain("document_context", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("tender_context", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void XSnapshotAndZCloseUseSameAssignedPeriodWhileZIncludesLaterPeriodDocuments()
    {
        var xSource = File.ReadAllText(FindSource());
        var zSource = File.ReadAllText(FindSource("PostgresFiscalZReadingRepository.cs"));
        Assert.Contains("d.business_day_date=@business", zSource, StringComparison.Ordinal);
        Assert.Contains("d.fiscal_reporting_period_id=@period", zSource, StringComparison.Ordinal);
        Assert.Contains("c.Parameters.AddWithValue(\"business\",p.BusinessDayDate)", zSource, StringComparison.Ordinal);
        Assert.DoesNotContain("d.created_at>=@start AND d.created_at<@end", zSource, StringComparison.Ordinal);
        Assert.Contains("d.created_at < @observed_at", xSource, StringComparison.Ordinal);
        Assert.DoesNotContain("d.created_at < @observed_at", zSource, StringComparison.Ordinal);
    }

    [Fact]
    public void V2SnapshotCapturesSourceBackedCountsPriorZAndRequestActorWithoutMutatingHistory()
    {
        var xSource = File.ReadAllText(FindSource());
        var zSource = File.ReadAllText(FindSource("PostgresFiscalZReadingRepository.cs"));
        var schema = File.ReadAllText(FindRepositoryFile("db", "state", "tables", "pos.x_z_reports.sql"));

        Assert.Contains("issued_sales_invoice_count", xSource, StringComparison.Ordinal);
        Assert.Contains("snapshot_detail_version", xSource, StringComparison.Ordinal);
        Assert.Contains("void_transaction_count", xSource, StringComparison.Ordinal);
        Assert.Contains("last_z_counter_value", xSource, StringComparison.Ordinal);
        Assert.Contains("last_z_closed_at", xSource, StringComparison.Ordinal);
        Assert.Contains("req.requested_by_ref", xSource, StringComparison.Ordinal);
        Assert.Contains("issued_sales_invoice_count", zSource, StringComparison.Ordinal);
        Assert.Contains("void_transaction_count", zSource, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS issued_sales_invoice_count bigint NULL", schema, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS snapshot_detail_version text NULL", schema, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS last_z_counter_value bigint NULL", schema, StringComparison.Ordinal);
        Assert.Contains("CREATE OR REPLACE TRIGGER trg_x_z_reports_immutable", schema, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.x_z_reports", xSource, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.x_z_reports", zSource, StringComparison.OrdinalIgnoreCase);
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

    private static string FindRepositoryFile(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Repository file was not found: {string.Join('/', parts)}");
    }
}
