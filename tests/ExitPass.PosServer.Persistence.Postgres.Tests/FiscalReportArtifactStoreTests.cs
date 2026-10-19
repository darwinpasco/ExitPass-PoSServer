using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Persistence.Postgres.FiscalReports;
using Xunit;

namespace ExitPass.PosServer.Persistence.Postgres.Tests;

public sealed class FiscalReportArtifactStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "exitpass-fiscal-artifacts-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ContentAddressedArtifactSurvivesStoreReplacementAndDoesNotMoveOnRead()
    {
        var bytes = Encoding.UTF8.GetBytes("immutable fiscal report");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var first = new FileSystemFiscalReportArtifactStore(root);
        var key = await first.PublishAsync(Guid.NewGuid(), "MONTHLY_ESALES", "json", bytes, hash);
        var path = Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar));
        var timestamp = File.GetLastWriteTimeUtc(path);

        var afterContainerReplacement = new FileSystemFiscalReportArtifactStore(root);
        Assert.Equal(bytes, await afterContainerReplacement.ReadAsync(key));
        Assert.True(File.Exists(path));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
    }

    [Fact]
    public async Task ExistingTamperedArtifactIsRejectedInsteadOfOverwritten()
    {
        var bytes = Encoding.UTF8.GetBytes("authoritative");
        var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var store = new FileSystemFiscalReportArtifactStore(root);
        var key = await store.PublishAsync(Guid.NewGuid(), "Z_READING", "pdf", bytes, hash);
        await File.WriteAllTextAsync(Path.Combine(root, key.Replace('/', Path.DirectorySeparatorChar)), "tampered");

        await Assert.ThrowsAsync<IOException>(() => store.PublishAsync(Guid.Parse(key[..36]), "Z_READING", "pdf", bytes, hash));
    }

    [Theory]
    [InlineData("../secret")]
    [InlineData("/absolute")]
    [InlineData("C:/absolute")]
    public async Task ReadRejectsPathsOutsideConfiguredRoot(string key)
    {
        var store = new FileSystemFiscalReportArtifactStore(root);
        Assert.Null(await store.ReadAsync(key));
    }

    [Fact]
    public void RuntimeLauncherAndGuardedReconcilerDeclareDurableArtifactStorage()
    {
        var root = FindRoot();
        var launcher = File.ReadAllText(Path.Combine(root, "scripts", "Start-PosServerPitxLocal.ps1"));
        Assert.Contains("pos-server\\fiscal-report-artifacts", launcher, StringComparison.Ordinal);
        Assert.Contains("/var/lib/exitpass/fiscal-report-artifacts", launcher, StringComparison.Ordinal);
        Assert.Contains("PosServer__FiscalReports__ArtifactRoot", launcher, StringComparison.Ordinal);
        Assert.Contains("test', '-w', $fiscalReportContainerArtifactRoot", launcher, StringComparison.Ordinal);

        var reconciliation = File.ReadAllText(Path.Combine(root, "db", "scripts", "Invoke-PosPersistentStateReconciliation.ps1"));
        Assert.Contains("FiscalReportArtifacts", reconciliation, StringComparison.Ordinal);
        Assert.Contains("BLOCKED_FISCAL_REPORT_ARTIFACT_SCHEMA_DRIFT", reconciliation, StringComparison.Ordinal);
        Assert.Contains("protected_manifest", reconciliation, StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "db", "rebuild", "pos_sql_apply_order.txt")))
            current = current.Parent;
        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    public void Dispose()
    {
        if (Directory.Exists(root)) Directory.Delete(root, true);
    }
}
