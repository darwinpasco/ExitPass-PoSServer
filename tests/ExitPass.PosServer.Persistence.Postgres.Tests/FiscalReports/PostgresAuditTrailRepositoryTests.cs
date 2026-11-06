using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresAuditTrailRepositoryTests
{
    [Fact]
    public void SalesProjectionUsesCommittedFiscalAndStatutoryFactsWithoutMutatingEvents()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresAuditTrailRepository.cs"));

        Assert.Contains("pos.fiscal_document_lines", source, StringComparison.Ordinal);
        Assert.Contains("sum(line.gross_amount_minor_units)", source, StringComparison.Ordinal);
        Assert.Contains("sum(line.discount_amount_minor_units)", source, StringComparison.Ordinal);
        Assert.Contains("jsonb_object_agg(classified.classification", source, StringComparison.Ordinal);
        Assert.Contains("CommercialDiscounts(discountClassifications, discount)", source, StringComparison.Ordinal);
        Assert.Contains("lineDiscount == 0 && discount == 0 ? payable : null", source, StringComparison.Ordinal);
        Assert.DoesNotContain("lineNet", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_totals", source, StringComparison.Ordinal);
        Assert.Contains("total_type.code_key='payable_total'", source, StringComparison.Ordinal);
        Assert.Contains("pos.fiscal_document_applied_statutory_facts", source, StringComparison.Ordinal);
        Assert.Contains("statutory.original_amount_minor_units", source, StringComparison.Ordinal);
        Assert.Contains("statutory.statutory_discount_amount_minor_units", source, StringComparison.Ordinal);
        Assert.Contains("statutory.final_payable_amount_minor_units", source, StringComparison.Ordinal);
        Assert.Contains("benefit.code_key", source, StringComparison.Ordinal);
        Assert.Contains("AuditTrailSalesDescription.Format", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UPDATE pos.", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM pos.", source, StringComparison.OrdinalIgnoreCase);
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
