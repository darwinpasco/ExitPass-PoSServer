using System.Security.Cryptography;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class FileSystemAnnexE1ArtifactStore(string rootPath) : IAnnexE1ArtifactStore
{
    private readonly string root = Path.GetFullPath(rootPath);

    public async Task<string> PublishAsync(byte[] bytes, string sha256, CancellationToken cancellationToken = default)
    {
        var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(actual, sha256, StringComparison.Ordinal) || sha256.Length != 64)
            throw new AnnexE1SafeException(AnnexE1Outcome.ArtifactIntegrityFailure, "The Annex E-1 artifact failed integrity validation.");

        var directory = Path.Combine(root, sha256[..2]);
        var finalPath = Path.Combine(directory, sha256 + ".xlsx");
        Directory.CreateDirectory(directory);
        if (File.Exists(finalPath))
        {
            var existing = await File.ReadAllBytesAsync(finalPath, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(existing), SHA256.HashData(bytes)))
                throw new AnnexE1SafeException(AnnexE1Outcome.ArtifactIntegrityFailure, "The Annex E-1 artifact identity is already bound to different bytes.");
            return RelativeKey(finalPath);
        }

        var temporaryPath = Path.Combine(directory, $".{sha256}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 65536,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                stream.Flush(flushToDisk: true);
            }
            try { File.Move(temporaryPath, finalPath); }
            catch (IOException) when (File.Exists(finalPath)) { }
            var published = await File.ReadAllBytesAsync(finalPath, cancellationToken).ConfigureAwait(false);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(published), SHA256.HashData(bytes)))
                throw new AnnexE1SafeException(AnnexE1Outcome.ArtifactIntegrityFailure, "The published Annex E-1 artifact failed integrity validation.");
            return RelativeKey(finalPath);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    public async Task<byte[]?> ReadAsync(string artifactKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(artifactKey)) return null;
        var path = Path.GetFullPath(Path.Combine(root, artifactKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private string RelativeKey(string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
}
