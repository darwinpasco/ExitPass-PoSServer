namespace ExitPass.PosServer.Runtime.FiscalReports;

public enum StatutorySalesBookType { AnnexE2, AnnexE3 }
public enum StatutorySalesBookFormat { Xlsx, Pdf }
public enum StatutorySalesBookOutcome { Succeeded, InvalidRequest, ScopeNotFound, MissingBeneficiarySnapshot, ReconciliationFailure, PersistenceFailure }

public sealed record StatutorySalesBookQuery(
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    int CalendarYear,
    int CalendarMonth,
    StatutorySalesBookType Type,
    string RequestedByRef,
    string? RequestedByDisplayName = null);

public sealed record StatutorySalesBookHeader(
    string TaxpayerName,
    string TaxpayerAddress,
    string Tin,
    string SoftwareName,
    string SoftwareVersion,
    string ReleaseNumber,
    string ReleaseDate,
    string SerialNumber,
    string MachineIdentificationNumber,
    string PosServerCode,
    DateTimeOffset GeneratedAt,
    string UserId,
    string? UserDisplayName = null);

public sealed record StatutorySalesBookFiscalHeaderFact(
    string TaxpayerName,
    string TaxpayerAddress,
    string Tin,
    string SerialNumber,
    string MachineIdentificationNumber,
    string PosServerCode,
    string SoftwareName,
    string SoftwareVersion);

public sealed record StatutorySalesBookRow(
    Guid FiscalDocumentId,
    DateOnly TransactionDate,
    DateTimeOffset TransactionTimestamp,
    long FiscalSequence,
    string BeneficiaryFullName,
    string StatutoryIdType,
    string StatutoryIdNumber,
    string? BeneficiaryTin,
    string SalesInvoiceNumber,
    long SalesInclusiveVatMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesMinorUnits,
    long Discount5MinorUnits,
    long Discount20MinorUnits,
    long NetSalesMinorUnits,
    string OrdinanceReference,
    int PercentageRateBasisPoints,
    string BenefitClassification = "PERCENTAGE_DISCOUNT",
    string? PolicyCode = null,
    long OriginalVatExclusiveSalesMinorUnits = 0,
    long NonPercentageBenefitAmountMinorUnits = 0,
    long FinalVatableSalesMinorUnits = 0,
    long FinalVatAmountMinorUnits = 0,
    long FinalPayableAmountMinorUnits = 0,
    string? LguPrivilegeType = null,
    decimal? FreeHoursGranted = null,
    long OriginalParkingFeeVatInclusiveMinorUnits = 0,
    long LguWaiverVatInclusiveMinorUnits = 0,
    long LguFinalPayableVatInclusiveMinorUnits = 0,
    string? LguPolicyReference = null)
{
    public StatutorySalesBookFiscalHeaderFact? FiscalHeader { get; init; }
}

public sealed record StatutorySalesBookSummary(
    StatutorySalesBookType Type,
    Guid SitePosServerId,
    Guid FiscalIdentityId,
    string CurrencyCode,
    int CalendarYear,
    int CalendarMonth,
    string PosServerCode,
    int QualifyingTransactionCount,
    long SalesInclusiveVatMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesMinorUnits,
    long Discount5MinorUnits,
    long Discount20MinorUnits,
    long NetSalesMinorUnits,
    DateTimeOffset ReportAsOf,
    IReadOnlyList<string> Warnings,
    string ReportStatus = StatutorySalesBookStatuses.Preliminary,
    bool IsEmpty = false,
    int ScopedFiscalPeriodCount = 0,
    int ClosedFiscalPeriodCount = 0,
    int CommittedZPeriodCount = 0,
    long LguOriginalParkingFeeVatInclusiveMinorUnits = 0,
    long LguWaiverVatInclusiveMinorUnits = 0,
    long LguFinalPayableVatInclusiveMinorUnits = 0);

public sealed record StatutorySalesBookSection(
    StatutorySalesBookHeader Header,
    StatutorySalesBookSummary Summary,
    IReadOnlyList<StatutorySalesBookRow> Rows);

public sealed record StatutorySalesBookReport(
    StatutorySalesBookHeader Header,
    StatutorySalesBookSummary Summary,
    IReadOnlyList<StatutorySalesBookRow> Rows)
{
    public IReadOnlyList<StatutorySalesBookSection>? IdentitySections { get; init; }

    public IReadOnlyList<StatutorySalesBookSection> EffectiveSections =>
        IdentitySections is { Count: > 0 }
            ? IdentitySections
            : StatutorySalesBookSectionPlanner.Resolve(this);
}

public sealed record StatutorySalesBookResult(
    StatutorySalesBookOutcome Outcome,
    StatutorySalesBookReport? Report = null,
    string? SafeMessage = null);

public sealed record StatutorySalesBookArtifact(
    byte[] Bytes,
    string ContentType,
    string FileName);

public interface IStatutorySalesBookRepository
{
    Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default);
}

public static class StatutorySalesBookContract
{
    public const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string PdfContentType = "application/pdf";
    public const string SoftwareName = "ExitPass POS Server";
    public const string SoftwareVersion = "V1.0.0";
    public const string ReleaseNumber = "v1.3";
    public const string ReleaseDate = "2026-10-17";
    public const string ArtifactScopeVersion = "pos-server-v2";
    public const string ArtifactLayoutVersion = "annex-e2-e3-16-column-identity-sections-v1";
    public const string ArtifactContractProfile = "pos-server-statutory-sales-book-rmo24-2023:pos-server-scope-v2:" + ArtifactLayoutVersion;

    public static string Entitlement(StatutorySalesBookType type) => type == StatutorySalesBookType.AnnexE2 ? "SENIOR_CITIZEN" : "PWD";
    public static string AnnexLabel(StatutorySalesBookType type) => type == StatutorySalesBookType.AnnexE2 ? "ANNEX \"E-2\"" : "ANNEX \"E-3\"";
    public static string Title(StatutorySalesBookType type) => type == StatutorySalesBookType.AnnexE2
        ? "Senior Citizen Sales Book/Report"
        : "Persons with Disability Sales Book/Report";

    public static string SourceFingerprint(StatutorySalesBookReport report) =>
        FiscalReportArtifactContract.SourceFingerprint(new
        {
            LayoutVersion = ArtifactLayoutVersion,
            Header = new
            {
                report.Header.TaxpayerName,
                report.Header.TaxpayerAddress,
                report.Header.Tin,
                report.Header.SoftwareName,
                report.Header.SoftwareVersion,
                report.Header.ReleaseNumber,
                report.Header.ReleaseDate,
                report.Header.SerialNumber,
                report.Header.MachineIdentificationNumber,
                report.Header.PosServerCode
            },
            Summary = new
            {
                report.Summary.Type,
                report.Summary.SitePosServerId,
                report.Summary.FiscalIdentityId,
                report.Summary.CurrencyCode,
                report.Summary.CalendarYear,
                report.Summary.CalendarMonth,
                report.Summary.PosServerCode,
                report.Summary.QualifyingTransactionCount,
                report.Summary.SalesInclusiveVatMinorUnits,
                report.Summary.VatAmountMinorUnits,
                report.Summary.VatExemptSalesMinorUnits,
                report.Summary.Discount5MinorUnits,
                report.Summary.Discount20MinorUnits,
                report.Summary.NetSalesMinorUnits,
                report.Summary.Warnings,
                report.Summary.ReportStatus,
                report.Summary.IsEmpty,
                report.Summary.ScopedFiscalPeriodCount,
                report.Summary.ClosedFiscalPeriodCount,
                report.Summary.CommittedZPeriodCount,
                report.Summary.LguOriginalParkingFeeVatInclusiveMinorUnits,
                report.Summary.LguWaiverVatInclusiveMinorUnits,
                report.Summary.LguFinalPayableVatInclusiveMinorUnits
            },
            Sections = report.EffectiveSections.Select(section => new
            {
                Header = new
                {
                    section.Header.TaxpayerName,
                    section.Header.TaxpayerAddress,
                    section.Header.Tin,
                    section.Header.SoftwareName,
                    section.Header.SoftwareVersion,
                    section.Header.SerialNumber,
                    section.Header.MachineIdentificationNumber,
                    section.Header.PosServerCode
                },
                Rows = section.Rows
            })
        });
}

public static class StatutorySalesBookSectionPlanner
{
    public static IReadOnlyList<StatutorySalesBookSection> Resolve(StatutorySalesBookReport report)
    {
        if (report.Rows.Count == 0)
            return [new(report.Header, report.Summary, report.Rows)];

        var fallback = new StatutorySalesBookFiscalHeaderFact(
            report.Header.TaxpayerName, report.Header.TaxpayerAddress, report.Header.Tin,
            report.Header.SerialNumber, report.Header.MachineIdentificationNumber, report.Header.PosServerCode,
            report.Header.SoftwareName, report.Header.SoftwareVersion);
        var attributed = report.Rows.Select(row => (Row: row, Header: row.FiscalHeader ?? fallback)).ToArray();
        var common = attributed[0].Header;
        if (attributed.Any(item =>
                item.Header.TaxpayerName != common.TaxpayerName ||
                item.Header.TaxpayerAddress != common.TaxpayerAddress ||
                item.Header.Tin != common.Tin ||
                item.Header.PosServerCode != common.PosServerCode ||
                item.Header.SoftwareName != common.SoftwareName ||
                item.Header.SoftwareVersion != common.SoftwareVersion))
            throw new InvalidOperationException("The statutory sales-book transactions span incompatible taxpayer or fiscal identity scopes.");

        var sections = attributed
            .GroupBy(item => new { item.Header.SerialNumber, item.Header.MachineIdentificationNumber })
            .OrderBy(group => group.Min(item => item.Row.TransactionTimestamp))
            .Select(group =>
            {
                var rows = group.Select(item => item.Row)
                    .OrderBy(row => row.TransactionTimestamp).ThenBy(row => row.FiscalSequence).ThenBy(row => row.FiscalDocumentId)
                    .ToArray();
                var header = report.Header with
                {
                    TaxpayerName = common.TaxpayerName,
                    TaxpayerAddress = common.TaxpayerAddress,
                    Tin = common.Tin,
                    SoftwareName = common.SoftwareName,
                    SoftwareVersion = common.SoftwareVersion,
                    SerialNumber = group.Key.SerialNumber,
                    MachineIdentificationNumber = group.Key.MachineIdentificationNumber,
                    PosServerCode = common.PosServerCode
                };
                return new StatutorySalesBookSection(header, Summary(report.Summary, rows), rows);
            })
            .ToArray();

        if (!StatutorySalesBookReconciliation.HasUniqueFiscalEvidence(sections.SelectMany(section => section.Rows).ToArray()) ||
            sections.Sum(section => section.Rows.Count) != report.Rows.Count ||
            !TotalsEqual(report.Summary, sections.Select(section => section.Summary)))
            throw new InvalidOperationException("The statutory sales-book identity sections do not reconcile to the monthly fiscal evidence.");
        return sections;
    }

    private static StatutorySalesBookSummary Summary(StatutorySalesBookSummary source, IReadOnlyList<StatutorySalesBookRow> rows) =>
        source with
        {
            QualifyingTransactionCount = rows.Count,
            SalesInclusiveVatMinorUnits = rows.Sum(row => row.SalesInclusiveVatMinorUnits),
            VatAmountMinorUnits = rows.Sum(row => row.VatAmountMinorUnits),
            VatExemptSalesMinorUnits = rows.Sum(row => row.VatExemptSalesMinorUnits),
            Discount5MinorUnits = rows.Sum(row => row.Discount5MinorUnits),
            Discount20MinorUnits = rows.Sum(row => row.Discount20MinorUnits),
            NetSalesMinorUnits = rows.Sum(row => row.NetSalesMinorUnits),
            IsEmpty = rows.Count == 0,
            LguOriginalParkingFeeVatInclusiveMinorUnits = rows.Sum(row => row.OriginalParkingFeeVatInclusiveMinorUnits),
            LguWaiverVatInclusiveMinorUnits = rows.Sum(row => row.LguWaiverVatInclusiveMinorUnits),
            LguFinalPayableVatInclusiveMinorUnits = rows.Sum(row => row.LguFinalPayableVatInclusiveMinorUnits)
        };

    private static bool TotalsEqual(StatutorySalesBookSummary monthly, IEnumerable<StatutorySalesBookSummary> sections)
    {
        var values = sections.ToArray();
        return values.Sum(section => section.QualifyingTransactionCount) == monthly.QualifyingTransactionCount &&
               values.Sum(section => section.SalesInclusiveVatMinorUnits) == monthly.SalesInclusiveVatMinorUnits &&
               values.Sum(section => section.VatAmountMinorUnits) == monthly.VatAmountMinorUnits &&
               values.Sum(section => section.VatExemptSalesMinorUnits) == monthly.VatExemptSalesMinorUnits &&
               values.Sum(section => section.Discount5MinorUnits) == monthly.Discount5MinorUnits &&
               values.Sum(section => section.Discount20MinorUnits) == monthly.Discount20MinorUnits &&
               values.Sum(section => section.NetSalesMinorUnits) == monthly.NetSalesMinorUnits &&
               values.Sum(section => section.LguOriginalParkingFeeVatInclusiveMinorUnits) == monthly.LguOriginalParkingFeeVatInclusiveMinorUnits &&
               values.Sum(section => section.LguWaiverVatInclusiveMinorUnits) == monthly.LguWaiverVatInclusiveMinorUnits &&
               values.Sum(section => section.LguFinalPayableVatInclusiveMinorUnits) == monthly.LguFinalPayableVatInclusiveMinorUnits;
    }
}

public static class StatutorySalesBookStatuses
{
    public const string Preliminary = "PRELIMINARY";
    public const string Final = "FINAL";

    public static string Resolve(
        int calendarYear,
        int calendarMonth,
        DateOnly currentSiteDate,
        int scopedPeriods,
        int closedPeriods,
        int committedZPeriods)
    {
        var requestedMonth = new DateOnly(calendarYear, calendarMonth, 1);
        var currentSiteMonth = new DateOnly(currentSiteDate.Year, currentSiteDate.Month, 1);
        return requestedMonth < currentSiteMonth && scopedPeriods > 0 &&
               scopedPeriods == closedPeriods && scopedPeriods == committedZPeriods
            ? Final
            : Preliminary;
    }
}

public static class StatutorySalesBookQualification
{
    public const string PercentageComponentNotSeparatelyProven = "E2_E3_PERCENTAGE_COMPONENT_NOT_SEPARATELY_PROVEN";

    public static bool IsQualifyingPercentageRow(
        string? entitlementType,
        StatutorySalesBookType reportType,
        string? policyResolutionBasis,
        int? percentageRateBasisPoints,
        long percentageDiscountAmountMinorUnits,
        bool percentageComponentSeparatelyProven) =>
        string.Equals(entitlementType?.Trim(), StatutorySalesBookContract.Entitlement(reportType), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(policyResolutionBasis?.Trim(), "LOCAL_ORDINANCE", StringComparison.OrdinalIgnoreCase) &&
        percentageRateBasisPoints is 500 or 2000 &&
        percentageDiscountAmountMinorUnits > 0 &&
        percentageComponentSeparatelyProven;

    public static string? SafeExclusionClassification(
        string? entitlementType,
        StatutorySalesBookType reportType,
        string? policyResolutionBasis,
        int? percentageRateBasisPoints,
        bool percentageComponentSeparatelyProven) =>
        string.Equals(entitlementType?.Trim(), StatutorySalesBookContract.Entitlement(reportType), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(policyResolutionBasis?.Trim(), "LOCAL_ORDINANCE", StringComparison.OrdinalIgnoreCase) &&
        percentageRateBasisPoints is 500 or 2000 &&
        !percentageComponentSeparatelyProven
            ? PercentageComponentNotSeparatelyProven
            : null;

    public static bool IsQualifyingFullFeeExemptionRow(
        string? entitlementType,
        StatutorySalesBookType reportType,
        string? policyResolutionBasis,
        string? benefitClassification,
        int? percentageRateBasisPoints,
        long statutoryDiscountAmountMinorUnits,
        long nonPercentageBenefitAmountMinorUnits,
        long finalPayableAmountMinorUnits) =>
        string.Equals(entitlementType?.Trim(), StatutorySalesBookContract.Entitlement(reportType), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(policyResolutionBasis?.Trim(), "LOCAL_ORDINANCE", StringComparison.OrdinalIgnoreCase) &&
        string.Equals(benefitClassification?.Trim(), "FREE_PARKING", StringComparison.OrdinalIgnoreCase) &&
        percentageRateBasisPoints is null &&
        statutoryDiscountAmountMinorUnits > 0 &&
        (nonPercentageBenefitAmountMinorUnits == 0 ||
         nonPercentageBenefitAmountMinorUnits == statutoryDiscountAmountMinorUnits) &&
        finalPayableAmountMinorUnits == 0;
}

public static class StatutorySalesBookReconciliation
{
    public static bool HasUniqueFiscalEvidence(IReadOnlyList<StatutorySalesBookRow> rows) =>
        rows.Select(row => row.FiscalDocumentId).Distinct().Count() == rows.Count &&
        rows.Select(row => row.SalesInvoiceNumber).Distinct(StringComparer.OrdinalIgnoreCase).Count() == rows.Count;

    public static bool IsValid(StatutorySalesBookRow row)
    {
        if (row.SalesInclusiveVatMinorUnits < 0 || row.VatAmountMinorUnits < 0 ||
            row.VatExemptSalesMinorUnits < 0 || row.Discount5MinorUnits < 0 ||
            row.Discount20MinorUnits < 0 || row.NetSalesMinorUnits < 0 ||
            row.OriginalParkingFeeVatInclusiveMinorUnits < 0 ||
            row.LguWaiverVatInclusiveMinorUnits < 0 ||
            row.LguFinalPayableVatInclusiveMinorUnits < 0)
            return false;

        var percentageDiscount = checked(row.Discount5MinorUnits + row.Discount20MinorUnits);
        var commonFactsReconcile = row.VatAmountMinorUnits <= row.SalesInclusiveVatMinorUnits &&
               row.VatExemptSalesMinorUnits <= row.SalesInclusiveVatMinorUnits &&
               row.NetSalesMinorUnits <= row.SalesInclusiveVatMinorUnits &&
               row.FinalVatableSalesMinorUnits >= 0 &&
               row.FinalVatAmountMinorUnits >= 0 &&
               row.FinalPayableAmountMinorUnits >= 0;
        if (!commonFactsReconcile)
            return false;

        if (string.Equals(row.BenefitClassification, "FREE_PARKING", StringComparison.OrdinalIgnoreCase))
            return row.PercentageRateBasisPoints == 0 &&
                   percentageDiscount == 0 &&
                   row.NonPercentageBenefitAmountMinorUnits > 0 &&
                   row.NonPercentageBenefitAmountMinorUnits <= row.OriginalVatExclusiveSalesMinorUnits &&
                   row.FinalVatableSalesMinorUnits == 0 &&
                   row.FinalVatAmountMinorUnits == 0 &&
                   row.FinalPayableAmountMinorUnits == 0 &&
                   row.NetSalesMinorUnits == 0 &&
                   string.Equals(row.LguPrivilegeType, "FULL_FEE_EXEMPTION", StringComparison.Ordinal) &&
                   row.FreeHoursGranted is null &&
                   row.OriginalParkingFeeVatInclusiveMinorUnits == row.SalesInclusiveVatMinorUnits &&
                   row.OriginalParkingFeeVatInclusiveMinorUnits >= row.LguFinalPayableVatInclusiveMinorUnits &&
                   row.LguWaiverVatInclusiveMinorUnits ==
                       row.OriginalParkingFeeVatInclusiveMinorUnits - row.LguFinalPayableVatInclusiveMinorUnits &&
                   row.LguFinalPayableVatInclusiveMinorUnits == row.FinalPayableAmountMinorUnits &&
                   !string.IsNullOrWhiteSpace(row.LguPolicyReference);

        return row.LguPrivilegeType is null && row.FreeHoursGranted is null &&
               row.OriginalParkingFeeVatInclusiveMinorUnits == 0 &&
               row.LguWaiverVatInclusiveMinorUnits == 0 &&
               row.LguFinalPayableVatInclusiveMinorUnits == 0 &&
               row.LguPolicyReference is null &&
               percentageDiscount > 0 && percentageDiscount <= row.SalesInclusiveVatMinorUnits &&
               (row.PercentageRateBasisPoints == 500
                   ? row.Discount5MinorUnits > 0 && row.Discount20MinorUnits == 0
                   : row.PercentageRateBasisPoints == 2000 && row.Discount20MinorUnits > 0 && row.Discount5MinorUnits == 0);
    }
}

public sealed class StatutorySalesBookService(IStatutorySalesBookRepository repository)
{
    public Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default)
    {
        if (query.SitePosServerId == Guid.Empty || query.FiscalIdentityId == Guid.Empty ||
            query.CalendarYear is < 2000 or > 9999 || query.CalendarMonth is < 1 or > 12 ||
            !string.Equals(query.CurrencyCode?.Trim(), "PHP", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(query.RequestedByRef))
            return Task.FromResult(new StatutorySalesBookResult(StatutorySalesBookOutcome.InvalidRequest, SafeMessage: "The statutory sales-book scope is invalid."));
        return repository.ReadAsync(query with { CurrencyCode = "PHP", RequestedByRef = query.RequestedByRef.Trim() }, cancellationToken);
    }
}
