using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresAnnexE1RepositoryTests
{
    [Fact]
    public void GenerationValidatesCommonIdentityBeforeSerialAndMinGrouping()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresAnnexE1Repository.cs"));
        var validation = source.IndexOf("AnnexE1IdentityCompatibility.ValidateMonthlySources", StringComparison.Ordinal);
        var grouping = source.IndexOf(".GroupBy(entry => new", validation, StringComparison.Ordinal);

        Assert.True(validation >= 0);
        Assert.True(grouping > validation);
        var groupingEnd = source.IndexOf("})", grouping, StringComparison.Ordinal);
        var groupingKey = source[grouping..groupingEnd];
        Assert.Contains("entry.Source.PosSerialNumber", groupingKey, StringComparison.Ordinal);
        Assert.Contains("entry.Source.MachineIdentificationNumber", groupingKey, StringComparison.Ordinal);
        Assert.DoesNotContain("entry.Source.TaxpayerName", groupingKey, StringComparison.Ordinal);
        Assert.DoesNotContain("entry.Source.SiteCode", groupingKey, StringComparison.Ordinal);
        Assert.DoesNotContain("entry.Source.SoftwareName", groupingKey, StringComparison.Ordinal);
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
