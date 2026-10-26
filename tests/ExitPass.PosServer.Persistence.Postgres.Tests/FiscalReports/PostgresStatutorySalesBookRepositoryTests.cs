using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresStatutorySalesBookRepositoryTests
{
    [Fact]
    public void SourceReadIsReadOnlyAndUsesOnlyCommittedFiscalEvidence()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresStatutorySalesBookRepository.cs"));

        Assert.Contains("IsolationLevel.RepeatableRead", source, StringComparison.Ordinal);
        Assert.Contains("SET TRANSACTION READ ONLY", source, StringComparison.Ordinal);
        Assert.Contains("document_status.code_key='issued'", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_document_applied_statutory_facts", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_document_statutory_beneficiary_snapshots", source, StringComparison.Ordinal);
        Assert.Contains("JOIN pos.channel_terminals terminal", source, StringComparison.Ordinal);
        Assert.Contains("terminal.site_pos_server_id=d.site_pos_server_id", source, StringComparison.Ordinal);
        Assert.Contains("d.site_pos_server_id=@site", source, StringComparison.Ordinal);
        Assert.Contains("d.fiscal_identity_id=@identity", source, StringComparison.Ordinal);
        Assert.Contains("d.currency_code=@currency", source, StringComparison.Ordinal);
        Assert.Contains("d.business_day_date>=@month_start", source, StringComparison.Ordinal);
        Assert.Contains("d.business_day_date<@month_end", source, StringComparison.Ordinal);
        Assert.Contains("d.is_active", source, StringComparison.Ordinal);
        Assert.Contains("d.voided_at IS NULL", source, StringComparison.Ordinal);
        Assert.DoesNotContain("terminal.is_active", source, StringComparison.Ordinal);
        Assert.DoesNotContain("d.channel_terminal_id=@terminal", source, StringComparison.Ordinal);
        Assert.Contains("pos.x_z_reports", source, StringComparison.Ordinal);
        Assert.Contains("lower(request_status.code_key)='committed'", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReaderFailsClosedForMissingIdentityDuplicateDocumentsOrUnreconciledRows()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresStatutorySalesBookRepository.cs"));

        Assert.Contains("MissingBeneficiarySnapshot", source, StringComparison.Ordinal);
        Assert.Contains("s.fiscal_document_id=d.fiscal_document_id", source, StringComparison.Ordinal);
        Assert.Contains("StatutorySalesBookReconciliation.HasUniqueFiscalEvidence", source, StringComparison.Ordinal);
        Assert.Contains("StatutorySalesBookReconciliation.IsValid", source, StringComparison.Ordinal);
        Assert.Contains("ReconciliationFailure", source, StringComparison.Ordinal);
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
