using System.Security.Cryptography;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Persistence.Postgres.FiscalReports;

public sealed class FileSystemFiscalReportArtifactStore : IFiscalReportArtifactStore
{
    private readonly string root;

    public FileSystemFiscalReportArtifactStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (!Path.IsPathFullyQualified(root))
            throw new ArgumentException("The fiscal report artifact root must be fully qualified.", nameof(root));
        this.root = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        Directory.CreateDirectory(this.root);
    }

    public async Task<string> PublishAsync(
        Guid sitePosServerId,
        string reportType,
        string format,
        byte[] bytes,
        string sha256,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length == 0 || !ValidHash(sha256)) throw new IOException("The fiscal report artifact is empty or has an invalid digest.");
        var actual = Hash(bytes);
        if (!string.Equals(actual, sha256, StringComparison.Ordinal)) throw new IOException("The fiscal report artifact digest does not match its content.");

        var type = Segment(reportType.ToLowerInvariant());
        var extension = Segment(format.ToLowerInvariant());
        var directory = SafePath(sitePosServerId.ToString("D"), type, sha256[..2]);
        Directory.CreateDirectory(directory);
        var destination = SafePath(sitePosServerId.ToString("D"), type, sha256[..2], $"{sha256}.{extension}");
        if (File.Exists(destination))
        {
            await VerifyAsync(destination, sha256, bytes.LongLength, cancellationToken).ConfigureAwait(false);
            return Relative(destination);
        }

        var temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken).ConfigureAwait(false);
            await VerifyAsync(temporary, sha256, bytes.LongLength, cancellationToken).ConfigureAwait(false);
            try { File.Move(temporary, destination, false); }
            catch (IOException) when (File.Exists(destination))
            {
                await VerifyAsync(destination, sha256, bytes.LongLength, cancellationToken).ConfigureAwait(false);
            }
            return Relative(destination);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<byte[]?> ReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey)) return null;
        var path = Path.GetFullPath(Path.Combine(root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return null;
        return await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
    }

    private string SafePath(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine([root, .. segments]));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("The fiscal report artifact path escaped its configured root.");
        return path;
    }

    private string Relative(string path) => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/');
    private static string Segment(string value) => value.Length is > 0 and <= 80 && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_')
        ? value
        : throw new IOException("The fiscal report artifact path segment is invalid.");
    private static bool ValidHash(string value) => value.Length == 64 && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static async Task VerifyAsync(string path, string sha256, long byteLength, CancellationToken cancellationToken)
    {
        var info = new FileInfo(path);
        if (info.Length != byteLength) throw new IOException("The persisted fiscal report artifact length is inconsistent.");
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false)).ToLowerInvariant();
        if (!string.Equals(hash, sha256, StringComparison.Ordinal)) throw new IOException("The persisted fiscal report artifact failed integrity verification.");
    }
}
