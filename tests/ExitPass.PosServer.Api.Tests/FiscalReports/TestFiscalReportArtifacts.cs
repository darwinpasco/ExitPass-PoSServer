using System.Security.Cryptography;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

internal static class TestFiscalReportArtifacts
{
    public static FiscalReportArtifactService Service() => new(new Repository());
    public static FiscalReportArtifactService CaptureService(out CaptureRepository repository)
    {
        repository = new CaptureRepository();
        return new(repository);
    }
    public static FiscalReportArtifactService FailureService() => new(new FailureRepository());

    private sealed class FailureRepository : IFiscalReportArtifactRepository
    {
        public Task<FiscalReportArtifactResult> GetOrCreateAsync(
            FiscalReportArtifactCommand command,
            Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalReportArtifactResult(
                FiscalReportArtifactOutcome.PersistenceUnavailable,
                SafeMessage: "Synthetic artifact persistence failure."));
    }

    private sealed class Repository : IFiscalReportArtifactRepository
    {
        public async Task<FiscalReportArtifactResult> GetOrCreateAsync(
            FiscalReportArtifactCommand command,
            Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
            CancellationToken cancellationToken = default)
        {
            var payload = await render(cancellationToken);
            var hash = Convert.ToHexString(SHA256.HashData(payload.Bytes)).ToLowerInvariant();
            var record = new FiscalReportArtifactRecord(
                Guid.NewGuid(), command.SitePosServerId, command.FiscalIdentityId, command.CurrencyCode,
                command.ReportType, command.ScopeReference, command.Format, command.ArtifactVersion,
                command.SourceFingerprint, command.FiscalReportingPeriodId, command.CalendarYear,
                command.CalendarMonth, command.BusinessDayDate, 1, null, $"test/{hash}.{command.Format}",
                hash, payload.Bytes.LongLength, payload.ContentType, payload.FileName, DateTimeOffset.UtcNow);
            return new(FiscalReportArtifactOutcome.Created, record, payload.Bytes);
        }
    }

    internal sealed class CaptureRepository : IFiscalReportArtifactRepository
    {
        public FiscalReportArtifactCommand? Command { get; private set; }

        public async Task<FiscalReportArtifactResult> GetOrCreateAsync(
            FiscalReportArtifactCommand command,
            Func<CancellationToken, Task<FiscalReportArtifactPayload>> render,
            CancellationToken cancellationToken = default)
        {
            Command = command;
            return await new Repository().GetOrCreateAsync(command, render, cancellationToken);
        }
    }
}
