using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresCloseableFiscalBusinessDateRepositoryTests
{
    [Fact]
    public void QueryIsExactScopeDatabaseTimedOpenEndedAndDeterministicallyOrdered()
    {
        var source = File.ReadAllText(FindSource());

        Assert.Contains("period.site_pos_server_id = @site", source, StringComparison.Ordinal);
        Assert.Contains("period.fiscal_identity_id = @identity", source, StringComparison.Ordinal);
        Assert.Contains("period.currency_code = @currency", source, StringComparison.Ordinal);
        Assert.Contains("status.code_key = 'open'", source, StringComparison.Ordinal);
        Assert.Contains("period.period_end_at <= transaction_timestamp()", source, StringComparison.Ordinal);
        Assert.Contains(
            "ORDER BY business_day_date, period_sequence, fiscal_reporting_period_id",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain("DateTimeOffset.UtcNow", source, StringComparison.Ordinal);
        Assert.DoesNotContain("site_group", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void QueryUsesZQualifyingDocumentPopulationAndProjectsSequentialStateVersions()
    {
        var source = File.ReadAllText(FindSource());

        Assert.Contains("document_type.code_key = 'sales_invoice'", source, StringComparison.Ordinal);
        Assert.Contains("document_status.code_key IN ('recorded', 'issued', 'voided')", source, StringComparison.Ordinal);
        Assert.Contains("document.fiscal_reporting_period_id = period.fiscal_reporting_period_id", source, StringComparison.Ordinal);
        Assert.Contains("state.state_version AS expected_state_version", source, StringComparison.Ordinal);
        Assert.Contains("chain.expected_state_version + 1", source, StringComparison.Ordinal);
        Assert.Contains("period.expected_prior_period_id = chain.fiscal_reporting_period_id", source, StringComparison.Ordinal);
        Assert.Contains("last_z.x_z_report_id IS NOT NULL", source, StringComparison.Ordinal);
    }

    private static string FindSource()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                "src",
                "ExitPass.PosServer.Persistence.Postgres",
                "FiscalReports",
                "PostgresCloseableFiscalBusinessDateRepository.cs");
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException("PostgreSQL closeable fiscal business date repository source was not found.");
    }
}
