using System.Runtime.CompilerServices;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class PersistentIstFiscalIssuanceReadinessTests
{
    [Fact]
    public void StaticMaterializerUsesStablePitxIdentitiesAndNeverResetsSequenceHistory()
    {
        var source = ReadScript("Set-PosPersistentIstFiscalOperationalConfiguration.ps1");

        Assert.Contains("b71f74f7-8e56-5fda-b257-5d9823ab8520", source);
        Assert.Contains("2e4252dd-38f0-5424-bee2-0c864b41f6b2", source);
        Assert.Contains("a46c9d75-5b50-5cb6-927e-d5c0b44a4edc", source);
        Assert.Contains("ON CONFLICT(fiscal_sequence_policy_id) DO NOTHING", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.fiscal_sequence_states", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("before.sequence_current_value", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("changed existing fiscal sequence history", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StaticMaterializerRequiresExistingPitxAuthorityObjectsAndCanonicalCodeKeys()
    {
        var source = ReadScript("Set-PosPersistentIstFiscalOperationalConfiguration.ps1");

        Assert.Contains("2d1dcdf8-f563-537c-8542-0bde7cc9da97", source);
        Assert.Contains("3a138565-1b88-55f8-c83d-5380db6edccc", source);
        Assert.Contains("ad02beb5-b8cd-4545-9ff2-5586782c686a", source);
        Assert.Contains("cb90e882-89f3-4bf3-975c-a6fb10dd765c", source);
        Assert.Contains("channel_terminal_type:webpay", source);
        Assert.Contains("fiscal_document_type:sales_invoice", source);
        Assert.Contains("fiscal_sequence_family:sales_invoice", source);
        Assert.Contains("fiscal_sequence_policy_status:active", source);
        Assert.Contains("fiscal_sequence_state:active", source);
    }

    [Fact]
    public void ReportingPeriodEnsureUsesGovernedBusinessDayAndFailsClosedOnOverlap()
    {
        var source = ReadScript("Ensure-PosPersistentIstFiscalReportingPeriod.ps1");

        Assert.Contains("Asia/Manila", source);
        Assert.Contains("00:00:00", source);
        Assert.Contains("tstzrange(period_start_at,period_end_at,'[)')", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Multiple current OPEN PITX fiscal reporting periods", source);
        Assert.Contains("Exactly one current OPEN PITX fiscal reporting period", source);
        Assert.Contains("expected_prior_period_id", source);
        Assert.Contains("max(period_sequence)+1", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE FROM pos.fiscal_reporting_periods", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReadinessRequiresExactlyOneOfEveryRuntimePrerequisite()
    {
        var source = ReadScript("Test-PosPersistentIstFiscalIssuanceReadiness.ps1");

        Assert.Contains("site_pos_server=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fiscal_identity=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("approved_profile=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("webpay_terminal=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sequence_policy=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sequence_state=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("reporting_contract=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("open_period=1", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LIMIT 1", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CanonicalFiscalIssuanceCodesAreDeterministicAndNotTestFixtures()
    {
        var generated = ReadRepoFile("db", "reference-data", "controlled-codes", "generated", "sql", "010_controlled_codes_fiscal_issuance.sql");
        var index = ReadRepoFile("db", "reference-data", "controlled-codes", "source", "controlled_code_source_index.json");

        Assert.Contains("b9fc7458-57a7-5bbe-809d-8c8b3d780b1d", generated);
        Assert.Contains("'fiscal_document_type','sales_invoice'", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'fiscal_sequence_family','sales_invoice'", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'fiscal_sequence_policy_status','active'", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'fiscal_sequence_state','active'", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("'channel_terminal_type','webpay'", generated, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("smoke", generated, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("fixture", generated, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("families/fiscal_document_type.json", index);
        Assert.Contains("families/fiscal_sequence_state.json", index);
    }

    private static string ReadScript(string fileName) =>
        ReadRepoFile("db", "scripts", fileName);

    private static string ReadRepoFile(params string[] parts)
    {
        foreach (var start in CandidateRoots())
        {
            var current = new DirectoryInfo(start);
            while (current is not null)
            {
                var candidate = Path.Combine(new[] { current.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }
                current = current.Parent;
            }
        }
        throw new FileNotFoundException($"Could not locate {Path.Combine(parts)}.");
    }

    private static IEnumerable<string> CandidateRoots([CallerFilePath] string testPath = "")
    {
        yield return Path.GetDirectoryName(testPath) ?? string.Empty;
        yield return AppContext.BaseDirectory;
        yield return Directory.GetCurrentDirectory();
    }
}
