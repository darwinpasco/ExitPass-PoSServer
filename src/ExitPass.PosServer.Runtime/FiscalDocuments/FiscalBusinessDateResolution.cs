namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record FiscalBusinessDateResolutionQuery(
    Guid SitePosServerId,
    Guid FiscalDocumentTypeCodeId,
    DateTimeOffset FiscalEventAt,
    string UpstreamFinalityReference);

public sealed record FiscalBusinessDateResolution(
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    bool IdempotencyRecordExists,
    Guid? LinkedFiscalDocumentId,
    string? ExistingSemanticRequestHash);

public interface IFiscalBusinessDateResolutionRepository
{
    Task<FiscalBusinessDateResolution> ResolveAsync(
        FiscalBusinessDateResolutionQuery query,
        CancellationToken cancellationToken = default);
}

public sealed class FiscalBusinessDateResolutionService(
    IFiscalBusinessDateResolutionRepository repository)
{
    public Task<FiscalBusinessDateResolution> ResolveAsync(
        FiscalBusinessDateResolutionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        if (query.SitePosServerId == Guid.Empty ||
            query.FiscalDocumentTypeCodeId == Guid.Empty ||
            query.FiscalEventAt == default ||
            string.IsNullOrWhiteSpace(query.UpstreamFinalityReference))
        {
            throw new ArgumentException("Fiscal business-date resolution facts are required.", nameof(query));
        }

        return repository.ResolveAsync(
            query with { UpstreamFinalityReference = query.UpstreamFinalityReference.Trim() },
            cancellationToken);
    }
}

public sealed class FiscalBusinessDateResolutionUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);
