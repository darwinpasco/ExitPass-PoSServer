namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalXReadingContract
{
    public const string ContractVersion = "pos-server-fiscal-reporting:v1";
    public const string SemanticHashVersion = "pos-server-fiscal-report-request:sha256:v1";
    public const string ReportKind = "X_READING";
}

public sealed record FiscalXReadingCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    DateTimeOffset ObservedAt,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record FiscalXReadingPeriod(
    Guid FiscalReportingPeriodId,
    Guid FiscalReportingContractVersionId,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    string CurrencyCode,
    long PeriodSequence);

public sealed record FiscalXReadingTenderBreakdown(string Classification, long TransactionCount, long AmountMinorUnits, string CurrencyCode);
public sealed record FiscalXReadingDiscountBreakdown(string Classification, long QualifyingDocumentCount, long DiscountAmountMinorUnits, long VatExemptionAmountMinorUnits, string CurrencyCode);
public sealed record FiscalXReadingSequenceGap(long SequenceValue, string Classification);
public sealed record FiscalXReadingFiscalNumberRange(Guid FiscalSequencePolicyId, string FiscalSeries, long FirstSequenceValue, long LastSequenceValue, string FirstFiscalNumber, string LastFiscalNumber, long QualifyingDocumentCount, IReadOnlyList<FiscalXReadingSequenceGap> Gaps, string CurrencyCode);

public sealed record FiscalXReadingAmounts(
    long GrossSalesAmountMinorUnits,
    long NetSalesAmountMinorUnits,
    long VatableSalesAmountMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesAmountMinorUnits,
    long ZeroRatedSalesAmountMinorUnits,
    long DiscountAmountMinorUnits,
    long SeniorCitizenDiscountAmountMinorUnits,
    long PwdDiscountAmountMinorUnits,
    long OtherStatutoryDiscountAmountMinorUnits,
    long VatExemptionAmountMinorUnits,
    long CouponDiscountAmountMinorUnits,
    long PromotionalDiscountAmountMinorUnits,
    long VoidAmountMinorUnits,
    long RefundAmountMinorUnits,
    long ReturnAmountMinorUnits,
    long AdjustmentAmountMinorUnits,
    long ServiceChargeAmountMinorUnits);

public sealed record FiscalXReadingRecord(
    Guid FiscalReportId,
    string FiscalReportReference,
    Guid FiscalReportRequestId,
    string OperationKey,
    string ReportKind,
    string ContractVersion,
    string SemanticHashVersion,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    Guid FiscalReportingPeriodId,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    string CurrencyCode,
    DateTimeOffset GeneratedAt,
    DateTimeOffset CommittedAt,
    long QualifyingDocumentCount,
    FiscalXReadingAmounts Amounts,
    IReadOnlyList<FiscalXReadingTenderBreakdown> Tenders,
    IReadOnlyList<FiscalXReadingDiscountBreakdown> Discounts,
    IReadOnlyList<FiscalXReadingFiscalNumberRange> FiscalNumberRanges,
    string CorrelationId,
    string SupportReference,
    bool Immutable);

public enum FiscalXReadingOutcome
{
    Created, Replayed, Conflict, SitePosServerNotFound, FiscalIdentityNotFound,
    ReportingConfigurationUnavailable, OpenPeriodUnavailable, AmbiguousOpenPeriod,
    InvalidRequest, UnsupportedSourceClassification, MixedCurrency,
    ReconciliationFailure, ArithmeticOverflow, PersistenceFailure,
    UnknownCommitOutcome, NotFound
}

public sealed record FiscalXReadingResult(FiscalXReadingOutcome Outcome, FiscalXReadingRecord? Record = null, string? SafeMessage = null);

public interface IFiscalXReadingRepository
{
    Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default);
    Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default);
}

public sealed class FiscalXReadingSafeException : Exception
{
    public FiscalXReadingSafeException(FiscalXReadingOutcome outcome, string message) : base(message) => Outcome = outcome;
    public FiscalXReadingOutcome Outcome { get; }
}
