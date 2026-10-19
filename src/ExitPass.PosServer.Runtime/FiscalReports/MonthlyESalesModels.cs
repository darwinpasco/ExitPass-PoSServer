namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class MonthlyESalesContract
{
    public const string ContractVersion = "pos-server-monthly-esales:v1";
    public const string JsonExportVersion = "pos-server-monthly-esales-json:v1";
    public const string CsvExportVersion = "bir-esales-job-aid:v1";
    public const string CurrencyCode = "PHP";
}

public static class MonthlyESalesSubmissionReadiness
{
    public const string TechnicalUat = "TECHNICAL_UAT";
    public const string FilingMetadataIncomplete = "FILING_METADATA_INCOMPLETE";
    public const string TechnicalReportReady = "TECHNICAL_REPORT_READY";
}

public static class MonthlyESalesCoverageStatus
{
    public const string MonthToDate = "MONTH_TO_DATE";
    public const string CompleteMonth = "COMPLETE_MONTH";
}

public sealed record MonthlyESalesQuery(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    int CalendarYear,
    int CalendarMonth);

public sealed record MonthlyESalesSourcePeriod(
    Guid FiscalReportingPeriodId,
    long PeriodSequence,
    DateOnly BusinessDayDate,
    bool PeriodClosed,
    bool GoverningZCommitted,
    bool BirSalesSummaryCommitted,
    string TaxpayerTin,
    string? RegisteredBusinessName,
    string? RegisteredBusinessAddress,
    string? SiteDisplayName,
    string? SoftwareVersion,
    string? PosSerialNumber,
    string? MachineIdentificationNumber,
    string? PtuNumber,
    long TransactionCount,
    string? BeginningSalesInvoiceReference,
    string? EndingSalesInvoiceReference,
    long GrossSalesAmountMinorUnits,
    long VatableSalesAmountMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesAmountMinorUnits,
    long ZeroRatedSalesAmountMinorUnits,
    DateTimeOffset? BirSalesSummaryCommittedAt);

public sealed record MonthlyESalesDailyRow(
    DateOnly BusinessDayDate,
    string? BeginningSalesInvoiceReference,
    string? EndingSalesInvoiceReference,
    long TransactionCount,
    long GrossSalesAmountMinorUnits,
    long VatableSalesAmountMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesAmountMinorUnits,
    long ZeroRatedSalesAmountMinorUnits);

public sealed record MonthlyESalesMachineSection(
    string PosSerialNumber,
    string MachineIdentificationNumber,
    string PtuNumber,
    MonthlyESalesRow Total,
    IReadOnlyList<MonthlyESalesDailyRow> DailyRows);

public sealed record MonthlyESalesPdfReport(
    MonthlyESalesReport Report,
    string? RegisteredBusinessName,
    string? RegisteredBusinessAddress,
    string? SiteDisplayName,
    string? SoftwareVersion,
    DateTimeOffset ReportAsOf,
    IReadOnlyList<MonthlyESalesMachineSection> Machines);

public sealed record MonthlyESalesRow(
    string MachineIdentificationNumber,
    int CalendarMonth,
    int CalendarYear,
    long TransactionCount,
    long MonthlySalesAmountMinorUnits,
    string MonthlySalesDisplay,
    string? EndingSalesInvoiceReference,
    long VatableSalesAmountMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesAmountMinorUnits,
    long ZeroRatedSalesAmountMinorUnits,
    int SourcePeriodCount,
    DateOnly FirstBusinessDate,
    DateOnly LastBusinessDate);

public sealed record MonthlyESalesReport(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    int CalendarYear,
    int CalendarMonth,
    string CurrencyCode,
    string TaxpayerTin,
    DateOnly? SubmissionDueDate,
    string? SubmissionDueDateBasis,
    string SubmissionReadiness,
    string SubmissionReadinessMessage,
    string CoverageStatus,
    string CoverageMessage,
    DateOnly FirstCoveredBusinessDate,
    DateOnly LastCoveredBusinessDate,
    DateTimeOffset CoverageAsOf,
    IReadOnlyList<MonthlyESalesRow> Rows);

public enum MonthlyESalesOutcome
{
    Succeeded,
    MonthNotComplete,
    MissingAuthoritativeSource,
    IncompleteAuthoritativeSources,
    UnsupportedSourceClassification,
    MissingEndRange,
    ArithmeticOverflow,
    ScopeMismatch,
    PersistenceFailure,
    InvalidRequest
}

public sealed record MonthlyESalesResult(
    MonthlyESalesOutcome Outcome,
    MonthlyESalesReport? Report = null,
    string? SafeMessage = null);

public sealed record MonthlyESalesPdfResult(
    MonthlyESalesOutcome Outcome,
    MonthlyESalesPdfReport? Report = null,
    string? SafeMessage = null);

public interface IMonthlyESalesRepository
{
    Task<MonthlyESalesResult> ReadAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default);
    Task<MonthlyESalesPdfResult> ReadPdfAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default);
}

public enum MonthlyESalesExportFormat
{
    Json,
    Csv,
    Pdf
}

public sealed record MonthlyESalesExport(
    MonthlyESalesExportFormat Format,
    string ContentType,
    string FileName,
    byte[] Bytes,
    string Sha256);
