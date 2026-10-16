using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class PosServerPitxLocalLauncherTests
{
    private const string ContainerArtifactRoot = "/var/lib/exitpass/annex-e1";

    [Fact]
    public void AnnexE1ArtifactsUsePersistentIstStorageAndExactContainerConfiguration()
    {
        var source = LauncherSource();

        Assert.Contains("$annexE1ArtifactRoot = Join-Path $privateRoot 'pos-server\\annex-e1'", source, StringComparison.Ordinal);
        Assert.Contains("[void](New-Item -ItemType Directory -Path $annexE1ArtifactRoot -Force)", source, StringComparison.Ordinal);
        Assert.Contains("Test-Path -LiteralPath $annexE1ArtifactRoot -PathType Container", source, StringComparison.Ordinal);
        Assert.Contains("The Annex E-1 artifact path is not a directory", source, StringComparison.Ordinal);
        Assert.Contains("The Annex E-1 artifact path must remain under the private persistent IST root", source, StringComparison.Ordinal);
        Assert.Contains("PosServer__AnnexE1__ArtifactRoot=$annexE1ContainerArtifactRoot", source, StringComparison.Ordinal);
        Assert.Contains("type=bind,source=$resolvedAnnexE1ArtifactRoot,target=$annexE1ContainerArtifactRoot", source, StringComparison.Ordinal);
        Assert.Contains("'exec', $containerName, 'test', '-w', $annexE1ContainerArtifactRoot", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PersistentArtifactsAreExcludedFromTemporaryCleanupAndExistingRuntimeContractsRemain()
    {
        var source = LauncherSource();

        Assert.DoesNotContain("Remove-Item -LiteralPath $resolvedAnnexE1ArtifactRoot", source, StringComparison.Ordinal);
        Assert.Contains("Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force", source, StringComparison.Ordinal);
        Assert.Contains("$databaseVolume = 'exitpass-pos-ist-persistent-data'", source, StringComparison.Ordinal);
        Assert.Contains("Wait-ForHealth '/health/live'", source, StringComparison.Ordinal);
        Assert.Contains("Wait-ForHealth '/health/ready'", source, StringComparison.Ordinal);
        Assert.DoesNotContain("$temporaryRoot 'pos-server\\annex-e1'", source, StringComparison.Ordinal);
        Assert.Contains(ContainerArtifactRoot, source, StringComparison.Ordinal);
    }

    private static string LauncherSource() => File.ReadAllText(Path.Combine(
        FindRepositoryRoot(),
        "scripts",
        "Start-PosServerPitxLocal.ps1"));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "scripts", "Start-PosServerPitxLocal.ps1")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate the POS Server repository root.");
    }
}
