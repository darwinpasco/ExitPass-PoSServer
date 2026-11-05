using System.Reflection;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
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

    [Fact]
    public void PdfContractPreservesStableSoftwareAndPartitionsEffectiveDatedMachineHeaders()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Persistence.Postgres", "FiscalReports", "PostgresMonthlyESalesRepository.cs"));

        Assert.Contains("var version = Stable(sources.Select(source => source.SoftwareVersion))", source, StringComparison.Ordinal);
        Assert.Contains("Serial = source.PosSerialNumber?.Trim() ?? string.Empty", source, StringComparison.Ordinal);
        Assert.Contains("Ptu = source.PtuNumber?.Trim() ?? string.Empty", source, StringComparison.Ordinal);
        Assert.Contains("MonthlyESalesAggregator.Build", source, StringComparison.Ordinal);
        Assert.Contains("The governed machine sections do not reconcile", source, StringComparison.Ordinal);
        Assert.Contains("not stable across the selected month", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfReportPartitionsOneMinByImmutableSerialWithoutDuplicatingMonthlyTotals()
    {
        var query = new MonthlyESalesQuery(Guid.NewGuid(), Guid.NewGuid(), "PHP", 2026, 11);
        var sources = new[]
        {
            Source(1, new DateOnly(2026, 11, 3), "SN-TEST-PITX-L3-001", 5_000, "SI-00000000000177"),
            Source(2, new DateOnly(2026, 11, 5), "0026_B728_3B2F_8505", 7_500, "SI-00000000000178")
        };
        var aggregate = MonthlyESalesAggregator.Build(query, new DateOnly(2026, 11, 5), sources);
        var report = Assert.IsType<MonthlyESalesReport>(aggregate.Report);
        var method = typeof(PostgresMonthlyESalesRepository).GetMethod(
            "BuildPdfReport", BindingFlags.NonPublic | BindingFlags.Static);

        var result = Assert.IsType<MonthlyESalesPdfResult>(method!.Invoke(null, [report, sources]));
        var pdf = Assert.IsType<MonthlyESalesPdfReport>(result.Report);
        Assert.Equal(MonthlyESalesOutcome.Succeeded, result.Outcome);
        Assert.Collection(pdf.Machines.OrderBy(machine => machine.Total.FirstBusinessDate),
            oldMachine =>
            {
                Assert.Equal("SN-TEST-PITX-L3-001", oldMachine.PosSerialNumber);
                Assert.Equal(5_000, oldMachine.Total.MonthlySalesAmountMinorUnits);
                Assert.Equal(new DateOnly(2026, 11, 3), oldMachine.Total.FirstBusinessDate);
            },
            newMachine =>
            {
                Assert.Equal("0026_B728_3B2F_8505", newMachine.PosSerialNumber);
                Assert.Equal(7_500, newMachine.Total.MonthlySalesAmountMinorUnits);
                Assert.Equal(new DateOnly(2026, 11, 5), newMachine.Total.FirstBusinessDate);
            });
        Assert.Equal(report.Rows.Single().MonthlySalesAmountMinorUnits,
            pdf.Machines.Sum(machine => machine.Total.MonthlySalesAmountMinorUnits));
    }

    private static MonthlyESalesSourcePeriod Source(
        long sequence,
        DateOnly businessDate,
        string serial,
        long gross,
        string endingInvoice) => new(
            Guid.NewGuid(), sequence, businessDate, true, true, true,
            "123456789", "ExitPass Test", "PITX", "PITX Level 3", "V1.0.0",
            serial, "123456789", "PTU-TEST-001", 1, endingInvoice, endingInvoice,
            gross, gross, 0, 0, 0, new DateTimeOffset(businessDate, TimeOnly.MinValue, TimeSpan.FromHours(8)));

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
