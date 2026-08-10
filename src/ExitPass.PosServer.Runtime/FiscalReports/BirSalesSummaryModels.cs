namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class BirSalesSummaryContract
{
    public const string ContractVersion = "pos-server-fiscal-reporting:v1";
    public const string SemanticHashVersion = "pos-server-fiscal-report-request:sha256:v1";
    public const string ExportVersion = "pos-server-bir-sales-summary-export:v1";
    public const string ReportingProfile = "pos-server-bir-sales-summary:v1";
    public const string ReportKind = "BIR_SALES_SUMMARY";
}

public sealed record BirSalesSummaryCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    string GoverningZReadingReference,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record BirSalesSummaryHeaderProfile(
    Guid SalesInvoiceHeaderProfileId,
    string ProfileVersion,
    string PosSerialNumber,
    string MachineIdentificationNumber,
    string BirAccreditationNumber,
    DateOnly BirAccreditationIssuedDate,
    DateOnly BirAccreditationValidUntil,
    string PtuNumber,
    DateOnly PtuIssuedDate);

public sealed record BirSalesSummaryRecord(
    Guid BirSalesSummaryReportId,
    Guid FiscalReportRequestId,
    string OperationKey,
    string ReportKind,
    string ContractVersion,
    string SemanticHashVersion,
    string ReportingContractProfile,
    Guid GoverningZReportId,
    string GoverningZReadingReference,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    Guid FiscalReportingPeriodId,
    DateOnly BusinessDayDate,
    DateOnly ReportingPeriodStartDate,
    DateOnly ReportingPeriodEndDate,
    string CurrencyCode,
    long TransactionCount,
    string? BeginningSalesInvoiceReference,
    string? EndingSalesInvoiceReference,
    FiscalXReadingAmounts Amounts,
    IReadOnlyList<FiscalXReadingTenderBreakdown> Tenders,
    IReadOnlyList<FiscalXReadingDiscountBreakdown> Discounts,
    IReadOnlyList<FiscalXReadingFiscalNumberRange> FiscalNumberRanges,
    FiscalZReadingCounterSnapshot CounterSnapshot,
    BirSalesSummaryHeaderProfile HeaderProfile,
    DateTimeOffset GeneratedAt,
    DateTimeOffset CommittedAt,
    string CorrelationId,
    string SupportReference,
    bool Immutable,
    string ReportStatus = "COMMITTED");

public enum BirSalesSummaryOutcome
{
    Created,
    Replayed,
    Conflict,
    GoverningZNotFound,
    GoverningZNotCommitted,
    PeriodNotClosed,
    ScopeMismatch,
    HeaderProfileUnavailable,
    ReconciliationFailure,
    UnsupportedSourceClassification,
    RetryableConcurrencyFailure,
    PersistenceFailure,
    UnknownCommitOutcome,
    InvalidRequest,
    NotFound
}

public sealed record BirSalesSummaryResult(
    BirSalesSummaryOutcome Outcome,
    BirSalesSummaryRecord? Record = null,
    string? SafeMessage = null);

public interface IBirSalesSummaryRepository
{
    Task<BirSalesSummaryResult> GenerateAsync(BirSalesSummaryCommand command, CancellationToken cancellationToken = default);
    Task<BirSalesSummaryResult> GetByIdAsync(Guid birSalesSummaryReportId, CancellationToken cancellationToken = default);
}

public enum BirSalesSummaryExportFormat
{
    Json,
    Csv
}

public sealed record BirSalesSummaryExport(
    BirSalesSummaryExportFormat Format,
    string ContentType,
    string FileName,
    byte[] Bytes,
    string Sha256);

public sealed class BirSalesSummarySafeException : Exception
{
    public BirSalesSummarySafeException(BirSalesSummaryOutcome outcome, string message) : base(message) => Outcome = outcome;
    public BirSalesSummaryOutcome Outcome { get; }
}
