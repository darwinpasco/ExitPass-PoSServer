using System.Security.Cryptography;
using System.Text.Json;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalReportArtifactContract
{
    public const string CatalogVersion = "pos-server-fiscal-report-artifacts:v1";

    public static string SourceFingerprint<T>(T value)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}

public sealed record FiscalReportArtifactCommand(
    Guid SitePosServerId,
    Guid? FiscalIdentityId,
    string CurrencyCode,
    string ReportType,
    string ScopeReference,
    string Format,
    string ArtifactVersion,
    string SourceFingerprint,
    Guid? FiscalReportingPeriodId = null,
    int? CalendarYear = null,
    int? CalendarMonth = null,
    DateOnly? BusinessDayDate = null);

public sealed record FiscalReportArtifactPayload(
    byte[] Bytes,
    string ContentType,
    string FileName);

public sealed record FiscalReportArtifactRecord(
    Guid ArtifactId,
    Guid SitePosServerId,
    Guid? FiscalIdentityId,
    string CurrencyCode,
    string ReportType,
    string ScopeReference,
    string Format,
    string ArtifactVersion,
    string SourceFingerprint,
    Guid? FiscalReportingPeriodId,
    int? CalendarYear,
    int? CalendarMonth,
    DateOnly? BusinessDayDate,
    int Revision,
    Guid? SupersedesArtifactId,
    string StorageKey,
    string Sha256,
    long ByteLength,
    string ContentType,
    string FileName,
    DateTimeOffset GeneratedAt);

public enum FiscalReportArtifactOutcome
{
    Created,
    Reused,
    InvalidRequest,
    PersistenceUnavailable,
    IntegrityFailure,
    RetryableConcurrencyFailure
}

public sealed record FiscalReportArtifactResult(
    FiscalReportArtifactOutcome Outcome,
    FiscalReportArtifactRecord? Artifact = null,
    byte[]? Bytes = null,
    string? SafeMessage = null)
{
    public bool Succeeded => Outcome is FiscalReportArtifactOutcome.Created or FiscalReportArtifactOutcome.Reused;
}

public interface IFiscalReportArtifactRepository
{
    Task<FiscalReportArtifactResult> GetOrCreateAsync(
        FiscalReportArtifactCommand command,
        Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
        CancellationToken cancellationToken = default);
}

public interface IFiscalReportArtifactStore
{
    Task<string> PublishAsync(
        Guid sitePosServerId,
        string reportType,
        string format,
        byte[] bytes,
        string sha256,
        CancellationToken cancellationToken = default);

    Task<byte[]?> ReadAsync(string storageKey, CancellationToken cancellationToken = default);
}

public sealed class FiscalReportArtifactService(IFiscalReportArtifactRepository repository)
{
    public Task<FiscalReportArtifactResult> GetOrCreateAsync(
        FiscalReportArtifactCommand command,
        Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
        CancellationToken cancellationToken = default) =>
        repository.GetOrCreateAsync(command, render, cancellationToken);
}
