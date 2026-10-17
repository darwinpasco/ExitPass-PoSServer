namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class AuditTrailService(IAuditTrailRepository repository)
{
    public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default)
    {
        if (query.SitePosServerId == Guid.Empty || query.From == default || query.To == default || query.To <= query.From)
            return Task.FromResult(new AuditTrailResult(AuditTrailOutcome.InvalidRequest, SafeMessage: "A bounded Audit Trail date range is required."));
        if (query.To - query.From > TimeSpan.FromDays(AuditTrailContract.MaximumRangeDays))
            return Task.FromResult(new AuditTrailResult(AuditTrailOutcome.RangeTooLarge, SafeMessage: "The Audit Trail date range cannot exceed 31 days."));
        if (!SafeFilter(query.ActionCode) || !SafeFilter(query.Actor) || !SafeFilter(query.Result) || !SafeFilter(query.Reference))
            return Task.FromResult(new AuditTrailResult(AuditTrailOutcome.InvalidRequest, SafeMessage: "An Audit Trail filter is invalid."));
        return repository.ReadAsync(query with
        {
            ActionCode = Normalize(query.ActionCode),
            Actor = Normalize(query.Actor),
            Result = Normalize(query.Result),
            Reference = Normalize(query.Reference)
        }, cancellationToken);
    }

    private static bool SafeFilter(string? value) => value is null ||
        value.Length <= 160 && !value.Contains('\r') && !value.Contains('\n');

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
