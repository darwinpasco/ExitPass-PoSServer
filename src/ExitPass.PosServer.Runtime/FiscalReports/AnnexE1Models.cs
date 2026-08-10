namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class AnnexE1Contract
{
    public const string Profile = "pos-server-bir-annex-e1-rmo24-2023:v1";
    public const string CalculationProfile = "pos-server-annex-e1-accounting-calculation:v1";
    public const string CalculationProfileSha256 = "36bbba7f014f5713955d50fe2fdab63f0cb6e80aab77713fe5fef45fbe7c1a4f";
    public const string SemanticHashVersion = "pos-server-annex-e1-request:sha256:v1";
    public const string FactSemanticHashVersion = "pos-server-annex-e1-period-fact:sha256:v1";
    public const string RendererVersion = "pos-server-annex-e1-openxml-renderer:v1";
    public const string OfficialTemplateSha256 = "7e46f34ae8779b303baa903f01bde794a519732de881d78109f79047d5a44197";
    public const string MimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string ReportKind = "ANNEX_E";
    public const string SoftwareName = "ExitPass POS Server";
    public const string SoftwareVersion = "1.3";
    public const string ReleaseNumber = "Z-012B";
    public const string ReleaseDate = "2026-08-10";
}

public static class AnnexE1FactTypes
{
    public const string ManualSiOrNetIncome = "manual_si_or_net_income";
    public const string SalesOverrunOverflowNetIncome = "sales_overrun_overflow_net_income";
    public const string NaacDiscount = "naac_discount";
    public const string SoloParentDiscount = "solo_parent_discount";
    public const string OtherVatAdjustment = "other_vat_adjustment";
    public const string VatOnReturns = "vat_on_returns";
    public const string ResidualVatAdjustment = "residual_vat_adjustment";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        ManualSiOrNetIncome, SalesOverrunOverflowNetIncome, NaacDiscount, SoloParentDiscount,
        OtherVatAdjustment, VatOnReturns, ResidualVatAdjustment
    };
}

public static class AnnexE1FactStatuses
{
    public const string Recorded = "recorded";
    public const string AttestedZero = "attested_zero";
}

public sealed record AnnexE1PeriodFactCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    string FactType,
    string FactStatus,
    long AmountMinorUnits,
    long SourceDocumentCount,
    string? FirstSourceReference,
    string? LastSourceReference,
    string? SourceEventReference,
    string ApprovalReference,
    Guid? SupersedesFactId,
    string? CorrectionReason,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record AnnexE1PeriodFactRecord(
    Guid FactId,
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    Guid FiscalReportingPeriodId,
    DateOnly BusinessDayDate,
    string FactType,
    string FactStatus,
    long AmountMinorUnits,
    long SourceDocumentCount,
    string? FirstSourceReference,
    string? LastSourceReference,
    string? SourceEventReference,
    string ApprovalReference,
    string SemanticHash,
    Guid? SupersedesFactId,
    string? CorrectionReason,
    DateTimeOffset EffectiveAt,
    DateTimeOffset RecordedAt,
    string RecordedByRef,
    string ServiceIdentityRef,
    string CorrelationId,
    bool Immutable = true);

public sealed record AnnexE1GenerationCommand(
    string OperationKey,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    int CalendarYear,
    int CalendarMonth,
    string Profile,
    Guid? SupersedesWorkbookId,
    string? CorrectionReason,
    string? CorrectionApprovalReference,
    string RequestedByRef,
    string ServiceIdentityRef,
    string CorrelationId);

public sealed record AnnexE1Header(
    string TaxpayerName,
    string TaxpayerAddress,
    string Tin,
    string SoftwareName,
    string SoftwareVersion,
    string ReleaseNumber,
    string ReleaseDate,
    string PosSerialNumber,
    string MachineIdentificationNumber,
    string PosTerminalNumber,
    DateTimeOffset GeneratedAt,
    string GeneratedByRef);

public sealed record AnnexE1RowInputs(
    Guid FiscalReportingPeriodId,
    long PeriodSequence,
    Guid GoverningZReportId,
    string GoverningZReference,
    Guid BirSalesSummaryReportId,
    string BirSalesSummarySemanticHash,
    DateOnly BusinessDayDate,
    string? BeginningFiscalNumber,
    string? EndingFiscalNumber,
    long PreviousGta,
    long ResultingGta,
    long ManualNetIncome,
    long ActiveGross,
    long ReturnAmount,
    long VoidAmount,
    long VatableSales,
    long VatAmount,
    long VatExemptSales,
    long ZeroRatedSales,
    long SeniorCitizenDiscount,
    long PwdDiscount,
    long NaacDiscount,
    long SoloParentDiscount,
    long OtherStatutoryDiscount,
    long CouponDiscount,
    long PromotionalDiscount,
    long SeniorCitizenVatAdjustment,
    long PwdVatAdjustment,
    long OtherVatAdjustment,
    long VatOnReturns,
    long ResidualVatAdjustment,
    long OverflowNetIncome,
    long ResetCounter,
    long ZCounter,
    long TransactionCount,
    long RefundAmount,
    long AdjustmentAmount,
    long ServiceChargeAmount,
    long FiscalRangeCount,
    long FiscalGapCount,
    IReadOnlyList<Guid> FactIds,
    IReadOnlyList<string> FactSemanticHashes);

public sealed record AnnexE1Row(
    AnnexE1RowInputs Source,
    IReadOnlyList<AnnexE1PositionValue> Positions,
    IReadOnlyList<AnnexE1Reconciliation> Reconciliations);

public sealed record AnnexE1PositionValue(string Position, string SemanticName, string? TextValue, long? MinorUnitsValue, long? IntegerValue);
public sealed record AnnexE1Reconciliation(string Rule, long DifferenceMinorUnits, bool Passed);

public sealed record AnnexE1WorkbookRecord(
    Guid WorkbookId,
    string AnnexEReference,
    string OperationKey,
    string Profile,
    string CalculationProfile,
    string RendererVersion,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    int CalendarYear,
    int CalendarMonth,
    int Revision,
    string SemanticHash,
    string ArtifactSha256,
    long ArtifactByteLength,
    string MimeType,
    string FileName,
    AnnexE1Header Header,
    IReadOnlyList<AnnexE1Row> Rows,
    Guid? SupersedesWorkbookId,
    string? SupersededByReference,
    string? CorrectionReason,
    string? CorrectionApprovalReference,
    DateTimeOffset GeneratedAt,
    DateTimeOffset CommittedAt,
    string CorrelationId,
    string Status = "COMMITTED",
    bool Immutable = true);

public enum AnnexE1Outcome
{
    Created, Replayed, Conflict, InvalidRequest, NotFound, ScopeMismatch, PeriodNotClosed,
    GoverningZNotCommitted, MissingAuthoritativeSource, MissingAccountingFact,
    UnsupportedClassification, UnsupportedPrivilege, ReconciliationFailure, ArithmeticOverflow,
    ArtifactUnavailable, ArtifactIntegrityFailure, RetryableConcurrencyFailure,
    PersistenceFailure, UnknownCommitOutcome
}

public sealed record AnnexE1FactResult(AnnexE1Outcome Outcome, AnnexE1PeriodFactRecord? Fact = null, string? SafeMessage = null);
public sealed record AnnexE1WorkbookResult(AnnexE1Outcome Outcome, AnnexE1WorkbookRecord? Workbook = null, string? SafeMessage = null);
public sealed record AnnexE1ArtifactResult(AnnexE1Outcome Outcome, AnnexE1WorkbookRecord? Workbook = null, byte[]? Bytes = null, string? SafeMessage = null);

public interface IAnnexE1Repository
{
    Task<AnnexE1FactResult> RecordFactAsync(AnnexE1PeriodFactCommand command, CancellationToken cancellationToken = default);
    Task<AnnexE1WorkbookResult> GenerateAsync(AnnexE1GenerationCommand command, CancellationToken cancellationToken = default);
    Task<AnnexE1WorkbookResult> GetAsync(Guid workbookId, CancellationToken cancellationToken = default);
    Task<AnnexE1ArtifactResult> DownloadAsync(Guid workbookId, CancellationToken cancellationToken = default);
}

public interface IAnnexE1ArtifactStore
{
    Task<string> PublishAsync(byte[] bytes, string sha256, CancellationToken cancellationToken = default);
    Task<byte[]?> ReadAsync(string artifactKey, CancellationToken cancellationToken = default);
}

public sealed class AnnexE1SafeException(AnnexE1Outcome outcome, string message) : Exception(message)
{
    public AnnexE1Outcome Outcome { get; } = outcome;
}
