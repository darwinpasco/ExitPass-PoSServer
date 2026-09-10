namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed record FiscalReportingPeriodSnapshot(Guid FiscalReportingPeriodId, Guid SitePosServerId, Guid FiscalIdentityId,
    DateOnly BusinessDayDate, DateTimeOffset PeriodStartAt, DateTimeOffset PeriodEndAt, string CurrencyCode,
    long PeriodSequence, string Status, long ExpectedZStateVersion);

public sealed record FiscalReadingHistoryItem(string ReportReference, string ReportKind, Guid FiscalReportingPeriodId,
    DateOnly BusinessDayDate, DateTimeOffset PeriodStartAt, DateTimeOffset PeriodEndAt, DateTimeOffset GeneratedAt,
    long TransactionCount, FiscalXReadingAmounts Amounts, string CurrencyCode, long PeriodSequence, string ReportStatus,
    IReadOnlyList<FiscalXReadingTenderBreakdown> Tenders,
    IReadOnlyList<FiscalXReadingDiscountBreakdown> Discounts);

public sealed record FiscalReportingHistorySnapshot(FiscalReportingPeriodSnapshot? CurrentPeriod,
    IReadOnlyList<FiscalReadingHistoryItem> XReadings, IReadOnlyList<FiscalReadingHistoryItem> ZReadings,
    string Code, string? SafeMessage = null);

public interface IFiscalReportingHistoryRepository
{
    Task<FiscalReportingHistorySnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId, string currencyCode,
        int limit, CancellationToken cancellationToken = default);
}

public sealed class FiscalReportingHistoryService(IFiscalReportingHistoryRepository repository)
{
    public Task<FiscalReportingHistorySnapshot> ReadAsync(Guid sitePosServerId, Guid fiscalIdentityId,
        string currencyCode, int limit = 50, CancellationToken cancellationToken = default)
    {
        var currency = currencyCode?.Trim().ToUpperInvariant();
        if (sitePosServerId == Guid.Empty || fiscalIdentityId == Guid.Empty || currency is not { Length: 3 } || limit is < 1 or > 100)
            return Task.FromResult(new FiscalReportingHistorySnapshot(null, [], [], "fiscal_reporting_history_invalid", "The fiscal reporting history request is invalid."));
        return repository.ReadAsync(sitePosServerId, fiscalIdentityId, currency, limit, cancellationToken);
    }
}
