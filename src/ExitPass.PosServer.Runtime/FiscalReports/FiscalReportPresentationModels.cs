namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class FiscalReportPresentationContract
{
    public const string XPresentationVersion = "fiscal-x-reading-presentation-json-v1";
    public const string ZPresentationVersion = "fiscal-z-reading-presentation-json-v1";
    public const string JsonExportVersion = "fiscal-x-z-reading-export-json-v1";
    public const string CsvExportVersion = "fiscal-x-z-reading-export-csv-v1";
    public const string PrintVersion = "fiscal-x-z-reading-print-text-v1";
    public const string OutputIdentityVersion = "fiscal-report-output:sha256:v1";
}

public sealed record FiscalReportPresentationModel(
    string PresentationContractVersion,
    string ReportKind,
    string Title,
    string ReportStatus,
    string PeriodStatus,
    string Finality,
    string ReportReference,
    string OperationReference,
    Guid SitePosServerId,
    string SiteIdentityPosture,
    Guid FiscalIdentityId,
    Guid FiscalReportingPeriodId,
    Guid? PriorFiscalReportingPeriodId,
    long PeriodSequence,
    DateOnly BusinessDayDate,
    DateTimeOffset PeriodStartAt,
    DateTimeOffset PeriodEndAt,
    string ReportingTimezoneName,
    TimeOnly BusinessDayCutoffLocalTime,
    string CurrencyCode,
    DateTimeOffset GeneratedAt,
    DateTimeOffset CommittedAt,
    DateTimeOffset? ClosedAt,
    string SourceReportContractVersion,
    string SourceSemanticHashVersion,
    string SnapshotIdentity,
    long QualifyingDocumentCount,
    FiscalXReadingAmounts Amounts,
    IReadOnlyList<FiscalReportTenderPresentation> Tenders,
    IReadOnlyList<FiscalReportDiscountPresentation> Discounts,
    IReadOnlyList<FiscalReportRangePresentation> FiscalNumberRanges,
    FiscalReportCounterPresentation? Counters,
    FiscalReportReconciliationPresentation Reconciliation,
    string CorrelationReference,
    string SupportReference,
    IReadOnlyList<FiscalReportFieldPosture> FieldPostures,
    IReadOnlyList<string> Statements,
    bool Immutable);

public sealed record FiscalReportTenderPresentation(
    string Classification,
    long TransactionCount,
    long AmountMinorUnits,
    string CurrencyCode);

public sealed record FiscalReportDiscountPresentation(
    string Classification,
    long QualifyingDocumentCount,
    long DiscountAmountMinorUnits,
    long VatExemptionAmountMinorUnits,
    string CurrencyCode);

public sealed record FiscalReportRangePresentation(
    string FiscalSeries,
    long FirstSequenceValue,
    long LastSequenceValue,
    string FirstFiscalNumber,
    string LastFiscalNumber,
    long QualifyingDocumentCount,
    IReadOnlyList<FiscalReportGapPresentation> Gaps,
    string CurrencyCode);

public sealed record FiscalReportGapPresentation(long SequenceValue, string Classification);

public sealed record FiscalReportCounterPresentation(
    long PreviousResetCounterValue,
    long ResultingResetCounterValue,
    long PreviousZCounterValue,
    long ResultingZCounterValue,
    long PreviousGrandTotalAmountMinorUnits,
    long CurrentPeriodGrandTotalAmountMinorUnits,
    long ResultingGrandTotalAmountMinorUnits,
    long ExpectedStateVersion,
    long ResultingStateVersion);

public sealed record FiscalReportReconciliationPresentation(
    string Status,
    long RecordedNetSalesAmountMinorUnits,
    long TenderTotalAmountMinorUnits,
    long DiscountBreakdownAmountMinorUnits,
    long RangeDocumentCount,
    long SequenceGapCount,
    bool TenderReconciled,
    bool DiscountReconciled,
    bool RangeCountReconciled,
    bool GtaReconciled);

public sealed record FiscalReportFieldPosture(string Field, string Posture);

public sealed record FiscalReportPresentationResult(
    bool Succeeded,
    FiscalReportPresentationErrorCode ErrorCode,
    string SafeMessage,
    FiscalReportPresentationModel? Presentation = null)
{
    public static FiscalReportPresentationResult Success(FiscalReportPresentationModel presentation) =>
        new(true, FiscalReportPresentationErrorCode.None, "Fiscal report presentation created.", presentation);

    public static FiscalReportPresentationResult Failure(FiscalReportPresentationErrorCode code, string message) =>
        new(false, code, message);
}

public enum FiscalReportPresentationErrorCode
{
    None,
    NotFound,
    WrongReportKind,
    NotFinalized,
    UnsupportedVersion,
    MalformedSnapshot,
    UnsupportedFormat,
    InvalidWidthProfile
}

public enum FiscalReportOutputFormat
{
    PresentationJson,
    Json,
    Csv,
    Text
}

public enum FiscalReportPrintWidthProfile
{
    Narrow,
    Standard,
    Office
}

public sealed record FiscalReportOutputArtifact(
    FiscalReportOutputFormat Format,
    string ContractVersion,
    string OutputIdentity,
    string ETag,
    string ContentType,
    string FileName,
    string ContentDisposition,
    byte[] Content);

public sealed record FiscalReportPresentationEnvelope(
    string ContractVersion,
    string OutputIdentity,
    FiscalReportPresentationModel Presentation);

public sealed record FiscalReportJsonExportEnvelope(
    string ExportContractVersion,
    string OutputIdentity,
    FiscalReportPresentationModel Report);
