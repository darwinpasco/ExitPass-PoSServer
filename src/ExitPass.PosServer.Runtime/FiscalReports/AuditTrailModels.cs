namespace ExitPass.PosServer.Runtime.FiscalReports;

public static class AuditTrailContract
{
    public const string ContractVersion = "pos-server-audit-trail:v1";
    public const string JsonExportVersion = "pos-server-audit-trail-json:v1";
    public const int MaximumRangeDays = 31;
    public const int MaximumRows = 10_000;
}

public sealed record AuditTrailQuery(
    Guid SitePosServerId,
    DateTimeOffset From,
    DateTimeOffset To,
    string? ActionCode = null,
    string? Actor = null,
    string? Result = null,
    string? Reference = null);

public sealed record AuditTrailHeader(
    string RegisteredBusinessName,
    string RegisteredBusinessAddress,
    string Tin,
    string MachineIdentificationNumber,
    string PosSerialNumber,
    string SiteDisplayName);

public sealed record AuditTrailActionDefinition(string Code, string DisplayName, string Definition);

public static class AuditTrailActionCatalog
{
    public static IReadOnlyList<AuditTrailActionDefinition> Definitions { get; } =
    [
        new("fiscal_document_committed", "SALES TRANSACTION", "A Sales Invoice and its governed fiscal facts were committed atomically."),
        new("fiscal_document_voided", "VOID", "A governed same-period Sales Invoice void was committed."),
        new("fiscal_document_reprint_recorded", "REPRINT", "A governed Sales Invoice reprint was recorded."),
        new("x_reading_committed", "X READING", "An immutable interim X Reading was committed."),
        new("z_reading_committed", "Z READING", "An immutable closing Z Reading was committed."),
        new("bir_sales_summary_committed", "DAILY BIR SALES SUMMARY", "A committed Daily BIR Sales Summary was bound to its governing Z Reading."),
        new("annex_e", "ANNEX E-1", "An Annex E-1 workbook generation or governed correction completed."),
        new("fiscal_report_exported", "REPORT EXPORT", "A governed fiscal report artifact was exported after its source snapshot was fixed."),
        new("electronic_journal_accessed", "REPORT ACCESS", "A governed Electronic Journal read or export was recorded."),
        new("fiscal_adjustment_recorded", "ADJUSTMENT", "A governed fiscal adjustment was recorded by a supported runtime.")
    ];

    public static string Category(string code) => Definitions
        .FirstOrDefault(definition => definition.Code.Equals(code, StringComparison.Ordinal))
        ?.DisplayName ?? code.ToUpperInvariant().Replace('_', ' ');
}

public sealed record AuditTrailRow(
    string AuditReference,
    DateTimeOffset OccurredAt,
    string? Terminal,
    string? UserId,
    string? UserRole,
    string ActionCode,
    string ActionCategory,
    string ActionDescription,
    string? SalesInvoiceNumber,
    string? BeforeValue,
    string? AfterValue,
    string? ReasonRemarks,
    string Result,
    string ActorType,
    string? ServiceIdentity,
    string TargetType,
    string? TargetReference,
    Guid? FiscalDocumentId,
    Guid? FiscalReportRequestId,
    string? CorrelationReference,
    IReadOnlyDictionary<string, string?> SafeMetadata);

public sealed record AuditTrailSalesFacts(
    string? PlateNumber,
    DateTimeOffset? EntryAt,
    long OriginalParkingFeeMinorUnits,
    long FiscalGrossSalesMinorUnits,
    long FinalPayableMinorUnits,
    long AmountPaidMinorUnits,
    string? PaymentMethod,
    long? StatutoryDiscountMinorUnits = null,
    long? LguFullFeeWaiverMinorUnits = null,
    string? EntitlementType = null);

public static class AuditTrailSalesDescription
{
    private static readonly TimeSpan PhilippineOffset = TimeSpan.FromHours(8);

    public static string Format(AuditTrailSalesFacts facts)
    {
        var lines = new List<string>
        {
            $"Plate:{facts.PlateNumber ?? string.Empty}",
            "Entry:",
            facts.EntryAt?.ToOffset(PhilippineOffset).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            facts.EntryAt?.ToOffset(PhilippineOffset).ToString("HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            $"Original Parking Fee:{Money(facts.OriginalParkingFeeMinorUnits)}",
            $"Fiscal Gross Sales:{Money(facts.FiscalGrossSalesMinorUnits)}"
        };
        if (facts.StatutoryDiscountMinorUnits is not null)
            lines.Add($"Statutory Discount:{Money(facts.StatutoryDiscountMinorUnits.Value)}");
        if (facts.LguFullFeeWaiverMinorUnits is not null)
            lines.Add($"LGU Full-Fee Waiver:{Money(facts.LguFullFeeWaiverMinorUnits.Value)}");
        lines.Add($"Final Payable:{Money(facts.FinalPayableMinorUnits)}");
        lines.Add($"Amount Paid:{Money(facts.AmountPaidMinorUnits)}");
        lines.Add($"Payment:{Payment(facts)}");
        if (!string.IsNullOrWhiteSpace(facts.EntitlementType))
            lines.Add($"Entitlement:{facts.EntitlementType.Trim().ToUpperInvariant().Replace('_', ' ')}");
        return string.Join('\n', lines);
    }

    private static string Payment(AuditTrailSalesFacts facts) =>
        facts.FinalPayableMinorUnits == 0 && facts.AmountPaidMinorUnits == 0
            ? "NO PAYMENT REQUIRED"
            : string.IsNullOrWhiteSpace(facts.PaymentMethod)
                ? "UNAVAILABLE"
                : facts.PaymentMethod.Trim().ToUpperInvariant();

    private static string Money(long minorUnits) =>
        $"PHP {(minorUnits / 100m).ToString("N2", System.Globalization.CultureInfo.InvariantCulture)}";
}

public sealed record AuditTrailReport(
    string ContractVersion,
    Guid SitePosServerId,
    DateTimeOffset From,
    DateTimeOffset To,
    AuditTrailHeader Header,
    IReadOnlyList<AuditTrailActionDefinition> ActionCodes,
    IReadOnlyList<AuditTrailRow> Rows);

public enum AuditTrailOutcome
{
    Succeeded,
    InvalidRequest,
    RangeTooLarge,
    ScopeNotFound,
    PersistenceFailure
}

public sealed record AuditTrailResult(
    AuditTrailOutcome Outcome,
    AuditTrailReport? Report = null,
    string? SafeMessage = null);

public interface IAuditTrailRepository
{
    Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default);

    Task RecordAccessAsync(
        Guid sitePosServerId,
        string reportKind,
        string subjectReference,
        string actorReference,
        string correlationReference,
        int rowCount,
        CancellationToken cancellationToken = default);

    Task RecordExportAsync(
        Guid sitePosServerId,
        string reportKind,
        string subjectReference,
        string format,
        string actorReference,
        string correlationReference,
        string contentSha256,
        int rowCount,
        CancellationToken cancellationToken = default);
}

public enum AuditTrailExportFormat { Json, Csv, Pdf }

public sealed record AuditTrailExport(
    AuditTrailExportFormat Format,
    string ContentType,
    string FileName,
    byte[] Bytes,
    string Sha256);
