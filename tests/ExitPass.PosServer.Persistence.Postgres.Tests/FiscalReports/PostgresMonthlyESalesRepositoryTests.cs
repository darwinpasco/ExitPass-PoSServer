using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests.FiscalReports;

public sealed class PostgresMonthlyESalesRepositoryTests
{
    [Fact]
    public void SourceReadUsesAStableReadOnlySnapshotWithoutAWriteLock()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresMonthlyESalesRepository.cs"));

        Assert.Contains("IsolationLevel.RepeatableRead", source, StringComparison.Ordinal);
        Assert.Contains("SET TRANSACTION READ ONLY", source, StringComparison.Ordinal);
        Assert.DoesNotContain("FOR UPDATE", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FOR SHARE", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("p.business_day_date", source, StringComparison.Ordinal);
        Assert.Contains("b.governing_z_report_id", source, StringComparison.Ordinal);
        Assert.Contains("brs.code_key", source, StringComparison.Ordinal);
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
