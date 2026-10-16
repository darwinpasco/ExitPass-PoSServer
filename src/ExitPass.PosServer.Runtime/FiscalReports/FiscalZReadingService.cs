namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalZReadingService
{
    private readonly IFiscalZReadingRepository repository;
    private readonly BirSalesSummaryService? birSalesSummaryService;

    public FiscalZReadingService(IFiscalZReadingRepository repository) : this(repository, null)
    {
    }

    public FiscalZReadingService(
        IFiscalZReadingRepository repository,
        BirSalesSummaryService? birSalesSummaryService)
    {
        this.repository = repository;
        this.birSalesSummaryService = birSalesSummaryService;
    }

    public async Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.OperationKey) || command.OperationKey.Length > 200 ||
            command.SitePosServerId == Guid.Empty || command.FiscalIdentityId == Guid.Empty ||
            command.FiscalReportingPeriodId == Guid.Empty || command.ExpectedStateVersion < 1 ||
            string.IsNullOrWhiteSpace(command.CurrencyCode) || command.CurrencyCode.Length != 3 ||
            command.CurrencyCode.Any(character => character is < 'A' or > 'Z') ||
            !IsSafeReference(command.RequestedByRef) || !IsSafeReference(command.ServiceIdentityRef) ||
            !IsSafeReference(command.CorrelationId))
        {
            return new FiscalZReadingResult(
                FiscalZReadingOutcome.InvalidRequest,
                SafeMessage: "The Z Reading close request is incomplete or invalid.");
        }

        var normalized = command with
        {
            OperationKey = command.OperationKey.Trim(),
            CurrencyCode = command.CurrencyCode.Trim(),
            RequestedByRef = command.RequestedByRef.Trim(),
            ServiceIdentityRef = command.ServiceIdentityRef.Trim(),
            CorrelationId = command.CorrelationId.Trim()
        };
        var zResult = await repository.CloseAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (zResult.Outcome is not (FiscalZReadingOutcome.Created or FiscalZReadingOutcome.Replayed) ||
            zResult.Record is null || birSalesSummaryService is null)
            return zResult;

        var completion = await EnsureBirSalesSummaryAsync(
            zResult.Record,
            normalized,
            cancellationToken).ConfigureAwait(false);
        return zResult with { BirSalesSummaryCompletion = completion };
    }

    public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) =>
        string.IsNullOrWhiteSpace(fiscalReportReference) || fiscalReportReference.Length > 160
            ? Task.FromResult(new FiscalZReadingResult(FiscalZReadingOutcome.InvalidRequest, SafeMessage: "Z Reading reference is required."))
            : repository.GetByReferenceAsync(fiscalReportReference.Trim(), cancellationToken);

    private static bool IsSafeReference(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= 200 && !value.Contains('\r') && !value.Contains('\n');

    private async Task<BirSalesSummaryCompletion> EnsureBirSalesSummaryAsync(
        FiscalZReadingRecord zReading,
        FiscalZReadingCommand zCommand,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await birSalesSummaryService!.GenerateAsync(new(
                $"bir-sales-summary:auto:{zReading.FiscalReportId:D}",
                zReading.SitePosServerId,
                zReading.FiscalIdentityId,
                zReading.CurrencyCode,
                zReading.FiscalReportingPeriodId,
                zReading.FiscalReportReference,
                zCommand.RequestedByRef,
                zCommand.ServiceIdentityRef,
                zCommand.CorrelationId), cancellationToken).ConfigureAwait(false);

            if (result.Outcome is BirSalesSummaryOutcome.Created or BirSalesSummaryOutcome.Replayed && result.Record is not null)
                return new("COMMITTED", result.Record.BirSalesSummaryReportId, "bir_sales_summary_committed", false);

            return new(
                "PENDING",
                null,
                CodeFor(result.Outcome),
                IsRetryable(result.Outcome),
                result.SafeMessage ?? "The BIR Sales Summary is pending and can be retried from BIR Sales Summary.");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not StackOverflowException)
        {
            return new(
                "PENDING",
                null,
                "bir_sales_summary_pending",
                true,
                "The BIR Sales Summary is pending and can be retried from BIR Sales Summary.");
        }
    }

    private static bool IsRetryable(BirSalesSummaryOutcome outcome) => outcome is
        BirSalesSummaryOutcome.RetryableConcurrencyFailure or
        BirSalesSummaryOutcome.PersistenceFailure or
        BirSalesSummaryOutcome.UnknownCommitOutcome;

    private static string CodeFor(BirSalesSummaryOutcome outcome) => outcome switch
    {
        BirSalesSummaryOutcome.GoverningZNotFound => "bir_sales_summary_governing_z_not_found",
        BirSalesSummaryOutcome.GoverningZNotCommitted => "bir_sales_summary_governing_z_not_committed",
        BirSalesSummaryOutcome.PeriodNotClosed => "bir_sales_summary_period_not_closed",
        BirSalesSummaryOutcome.ScopeMismatch => "bir_sales_summary_scope_mismatch",
        BirSalesSummaryOutcome.HeaderProfileUnavailable => "bir_sales_summary_header_profile_unavailable",
        BirSalesSummaryOutcome.ReconciliationFailure => "bir_sales_summary_reconciliation_failed",
        BirSalesSummaryOutcome.UnsupportedSourceClassification => "bir_sales_summary_source_classification_unsupported",
        BirSalesSummaryOutcome.RetryableConcurrencyFailure => "bir_sales_summary_temporarily_busy",
        BirSalesSummaryOutcome.PersistenceFailure => "bir_sales_summary_persistence_failed",
        BirSalesSummaryOutcome.UnknownCommitOutcome => "bir_sales_summary_unknown_commit_outcome",
        BirSalesSummaryOutcome.Conflict => "bir_sales_summary_conflict",
        _ => "bir_sales_summary_pending"
    };
}
