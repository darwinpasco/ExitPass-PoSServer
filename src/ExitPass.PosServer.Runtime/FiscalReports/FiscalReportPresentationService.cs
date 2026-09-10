using System.Globalization;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class FiscalReportPresentationService
{
    public FiscalReportPresentationResult Present(FiscalXReadingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var validation = ValidateCommon(
            record.ReportKind,
            FiscalXReadingContract.ReportKind,
            record.ContractVersion,
            record.ReportStatus,
            record.FiscalReportReference,
            record.OperationKey,
            record.SitePosServerId,
            record.FiscalIdentityId,
            record.FiscalReportingPeriodId,
            record.PeriodSequence,
            record.PeriodStartAt,
            record.PeriodEndAt,
            record.GeneratedAt,
            record.CommittedAt,
            record.CurrencyCode,
            record.QualifyingDocumentCount,
            record.Amounts,
            record.Tenders,
            record.Discounts,
            record.FiscalNumberRanges,
            record.Immutable);
        if (validation is not null)
        {
            return validation;
        }

        if (record.GeneratedAt < record.PeriodStartAt)
        {
            return Malformed();
        }

        return Build(
            FiscalReportPresentationContract.XPresentationVersion,
            record.ReportKind,
            "X READING",
            record.ReportStatus,
            "OPEN_AT_OBSERVATION",
            "INTERIM_READ_ONLY",
            record.FiscalReportReference,
            record.OperationKey,
            record.SitePosServerId,
            record.FiscalIdentityId,
            record.FiscalReportingPeriodId,
            null,
            record.PeriodSequence,
            record.BusinessDayDate,
            record.PeriodStartAt,
            record.PeriodEndAt,
            record.ReportingTimezoneName,
            record.BusinessDayCutoffLocalTime,
            record.CurrencyCode,
            record.GeneratedAt,
            record.CommittedAt,
            null,
            record.ContractVersion,
            record.SemanticHashVersion,
            record.QualifyingDocumentCount,
            record.Amounts,
            record.Tenders,
            record.Discounts,
            record.FiscalNumberRanges,
            null,
            record.CorrelationId,
            record.SupportReference,
            [
                new("siteIdentity", "not_recorded"),
                new("closeTimestamp", "not_applicable"),
                new("zCounter", "not_recorded"),
                new("resetCounter", "not_recorded"),
                new("grandTotalObservation", "not_recorded")
            ],
            [
                "INTERIM READ-ONLY OBSERVATION",
                "THIS X READING DOES NOT CLOSE THE FISCAL PERIOD",
                "FISCAL COUNTERS AND GRAND TOTAL STATE ARE UNCHANGED"
            ],
            record.Immutable);
    }

    public FiscalReportPresentationResult Present(FiscalZReadingRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var validation = ValidateCommon(
            record.ReportKind,
            FiscalZReadingContract.ReportKind,
            record.ContractVersion,
            record.ReportStatus,
            record.FiscalReportReference,
            record.OperationKey,
            record.SitePosServerId,
            record.FiscalIdentityId,
            record.FiscalReportingPeriodId,
            record.PeriodSequence,
            record.PeriodStartAt,
            record.PeriodEndAt,
            record.GeneratedAt,
            record.CommittedAt,
            record.CurrencyCode,
            record.QualifyingDocumentCount,
            record.Amounts,
            record.Tenders,
            record.Discounts,
            record.FiscalNumberRanges,
            record.Immutable);
        if (validation is not null)
        {
            return validation;
        }

        var counter = record.CounterSnapshot;
        if (!string.Equals(record.PeriodStatus, "CLOSED", StringComparison.Ordinal) ||
            record.GeneratedAt < record.PeriodEndAt ||
            record.ClosedAt < record.PeriodEndAt ||
            counter.ResultingResetCounterValue != counter.PreviousResetCounterValue ||
            counter.ResultingZCounterValue != counter.PreviousZCounterValue + 1 ||
            counter.ResultingStateVersion != counter.ExpectedStateVersion + 1 ||
            counter.CurrentPeriodGrandTotalAmountMinorUnits != record.Amounts.NetSalesAmountMinorUnits ||
            !TryAdd(counter.PreviousGrandTotalAmountMinorUnits, counter.CurrentPeriodGrandTotalAmountMinorUnits, out var gta) ||
            gta != counter.ResultingGrandTotalAmountMinorUnits)
        {
            return Malformed();
        }

        return Build(
            FiscalReportPresentationContract.ZPresentationVersion,
            record.ReportKind,
            "Z READING",
            record.ReportStatus,
            record.PeriodStatus,
            "IMMUTABLE_CLOSED",
            record.FiscalReportReference,
            record.OperationKey,
            record.SitePosServerId,
            record.FiscalIdentityId,
            record.FiscalReportingPeriodId,
            record.PriorFiscalReportingPeriodId,
            record.PeriodSequence,
            record.BusinessDayDate,
            record.PeriodStartAt,
            record.PeriodEndAt,
            record.ReportingTimezoneName,
            record.BusinessDayCutoffLocalTime,
            record.CurrencyCode,
            record.GeneratedAt,
            record.CommittedAt,
            record.ClosedAt,
            record.ContractVersion,
            record.SemanticHashVersion,
            record.QualifyingDocumentCount,
            record.Amounts,
            record.Tenders,
            record.Discounts,
            record.FiscalNumberRanges,
            new(
                counter.PreviousResetCounterValue,
                counter.ResultingResetCounterValue,
                counter.PreviousZCounterValue,
                counter.ResultingZCounterValue,
                counter.PreviousGrandTotalAmountMinorUnits,
                counter.CurrentPeriodGrandTotalAmountMinorUnits,
                counter.ResultingGrandTotalAmountMinorUnits,
                counter.ExpectedStateVersion,
                counter.ResultingStateVersion),
            record.CorrelationId,
            record.SupportReference,
            [new("siteIdentity", "not_recorded")],
            [
                "IMMUTABLE CLOSED-PERIOD SNAPSHOT",
                "ONE-TIME Z CLOSE COMMITTED",
                "READBACK AND EXPORT DO NOT RECOMPUTE FISCAL FACTS"
            ],
            record.Immutable);
    }

    private static FiscalReportPresentationResult? ValidateCommon(
        string actualKind,
        string expectedKind,
        string contractVersion,
        string reportStatus,
        string reportReference,
        string operationReference,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        Guid periodId,
        long periodSequence,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        DateTimeOffset generatedAt,
        DateTimeOffset committedAt,
        string currency,
        long documentCount,
        FiscalXReadingAmounts amounts,
        IReadOnlyList<FiscalXReadingTenderBreakdown> tenders,
        IReadOnlyList<FiscalXReadingDiscountBreakdown> discounts,
        IReadOnlyList<FiscalXReadingFiscalNumberRange> ranges,
        bool immutable)
    {
        if (!string.Equals(actualKind, expectedKind, StringComparison.Ordinal))
        {
            return FiscalReportPresentationResult.Failure(
                FiscalReportPresentationErrorCode.WrongReportKind,
                "The stored fiscal report kind does not match the requested output contract.");
        }

        if (!string.Equals(contractVersion, FiscalXReadingContract.ContractVersion, StringComparison.Ordinal))
        {
            return FiscalReportPresentationResult.Failure(
                FiscalReportPresentationErrorCode.UnsupportedVersion,
                "The stored fiscal report contract version is not supported for presentation.");
        }

        if (!string.Equals(reportStatus, "COMMITTED", StringComparison.Ordinal))
        {
            return FiscalReportPresentationResult.Failure(
                FiscalReportPresentationErrorCode.NotFinalized,
                "The stored fiscal report is not finalized for presentation.");
        }

        if (!immutable || string.IsNullOrWhiteSpace(reportReference) || string.IsNullOrWhiteSpace(operationReference) ||
            sitePosServerId == Guid.Empty || fiscalIdentityId == Guid.Empty || periodId == Guid.Empty ||
            periodSequence < 1 || periodEnd <= periodStart || committedAt < generatedAt ||
            currency.Length != 3 || currency.Any(character => character is < 'A' or > 'Z') ||
            documentCount < 0 || !AmountsAreNonNegative(amounts))
        {
            return Malformed();
        }

        try
        {
            var tenderTotal = tenders.Aggregate(0L, (total, item) => checked(total + item.AmountMinorUnits));
            var discountTotal = discounts.Aggregate(0L, (total, item) => checked(total + item.DiscountAmountMinorUnits));
            var vatExemptionTotal = discounts.Aggregate(0L, (total, item) => checked(total + item.VatExemptionAmountMinorUnits));
            var rangeCount = ranges.Aggregate(0L, (total, item) => checked(total + item.QualifyingDocumentCount));
            var discountByClass = discounts
                .GroupBy(item => item.Classification, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            long Discount(string classification) => discountByClass.GetValueOrDefault(classification)?.DiscountAmountMinorUnits ?? 0;
            long VatExemption(string classification) => discountByClass.GetValueOrDefault(classification)?.VatExemptionAmountMinorUnits ?? 0;
            var classificationsValid = discountByClass.Count == discounts.Count &&
                discountByClass.Keys.All(key => key is "senior_citizen_statutory" or "pwd_statutory" or "other_statutory" or
                    "vat_exemption_adjustment" or "coupon" or "promotional") &&
                Discount("senior_citizen_statutory") == amounts.SeniorCitizenDiscountAmountMinorUnits &&
                Discount("pwd_statutory") == amounts.PwdDiscountAmountMinorUnits &&
                Discount("other_statutory") == amounts.OtherStatutoryDiscountAmountMinorUnits &&
                Discount("coupon") == amounts.CouponDiscountAmountMinorUnits &&
                Discount("promotional") == amounts.PromotionalDiscountAmountMinorUnits &&
                Discount("vat_exemption_adjustment") == 0 &&
                VatExemption("vat_exemption_adjustment") == amounts.VatExemptionAmountMinorUnits;
            if (tenders.Any(item => string.IsNullOrWhiteSpace(item.Classification) || item.TransactionCount < 0 ||
                                    item.AmountMinorUnits < 0 || item.CurrencyCode != currency) ||
                discounts.Any(item => string.IsNullOrWhiteSpace(item.Classification) || item.QualifyingDocumentCount < 0 ||
                                      item.DiscountAmountMinorUnits < 0 || item.VatExemptionAmountMinorUnits < 0 ||
                                      item.CurrencyCode != currency) ||
                ranges.Any(item => string.IsNullOrWhiteSpace(item.FiscalSeries) ||
                                   string.IsNullOrWhiteSpace(item.FirstFiscalNumber) ||
                                   string.IsNullOrWhiteSpace(item.LastFiscalNumber) ||
                                   item.FirstSequenceValue < 1 || item.LastSequenceValue < item.FirstSequenceValue ||
                                   item.QualifyingDocumentCount < 1 || item.CurrencyCode != currency ||
                                   item.Gaps.Any(gap => gap.SequenceValue < item.FirstSequenceValue ||
                                                        gap.SequenceValue > item.LastSequenceValue ||
                                                        string.IsNullOrWhiteSpace(gap.Classification))) ||
                (tenders.Count > 0 && tenderTotal != amounts.NetSalesAmountMinorUnits) ||
                (discounts.Count > 0 &&
                    (discountTotal != amounts.DiscountAmountMinorUnits ||
                     vatExemptionTotal != amounts.VatExemptionAmountMinorUnits ||
                     !classificationsValid)) ||
                (ranges.Count > 0 && rangeCount != documentCount))
            {
                return Malformed();
            }
        }
        catch (OverflowException)
        {
            return Malformed();
        }

        return null;
    }

    private static FiscalReportPresentationResult Build(
        string presentationVersion,
        string reportKind,
        string title,
        string reportStatus,
        string periodStatus,
        string finality,
        string reportReference,
        string operationReference,
        Guid sitePosServerId,
        Guid fiscalIdentityId,
        Guid periodId,
        Guid? priorPeriodId,
        long periodSequence,
        DateOnly businessDate,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        string timezone,
        TimeOnly cutoff,
        string currency,
        DateTimeOffset generated,
        DateTimeOffset committed,
        DateTimeOffset? closed,
        string sourceContractVersion,
        string sourceHashVersion,
        long documentCount,
        FiscalXReadingAmounts amounts,
        IReadOnlyList<FiscalXReadingTenderBreakdown> tenders,
        IReadOnlyList<FiscalXReadingDiscountBreakdown> discounts,
        IReadOnlyList<FiscalXReadingFiscalNumberRange> ranges,
        FiscalReportCounterPresentation? counters,
        string correlation,
        string support,
        IReadOnlyList<FiscalReportFieldPosture> postures,
        IReadOnlyList<string> statements,
        bool immutable)
    {
        var tenderRows = tenders
            .OrderBy(item => item.Classification, StringComparer.Ordinal)
            .Select(item => new FiscalReportTenderPresentation(item.Classification, item.TransactionCount, item.AmountMinorUnits, item.CurrencyCode))
            .ToArray();
        var discountRows = discounts
            .OrderBy(item => item.Classification, StringComparer.Ordinal)
            .Select(item => new FiscalReportDiscountPresentation(item.Classification, item.QualifyingDocumentCount, item.DiscountAmountMinorUnits, item.VatExemptionAmountMinorUnits, item.CurrencyCode))
            .ToArray();
        var rangeRows = ranges
            .OrderBy(item => item.FiscalSeries, StringComparer.Ordinal)
            .ThenBy(item => item.FirstSequenceValue)
            .Select(item => new FiscalReportRangePresentation(
                item.FiscalSeries,
                item.FirstSequenceValue,
                item.LastSequenceValue,
                item.FirstFiscalNumber,
                item.LastFiscalNumber,
                item.QualifyingDocumentCount,
                item.Gaps.OrderBy(gap => gap.SequenceValue)
                    .Select(gap => new FiscalReportGapPresentation(gap.SequenceValue, gap.Classification))
                    .ToArray(),
                item.CurrencyCode))
            .ToArray();

        var tenderTotal = tenderRows.Aggregate(0L, (total, item) => checked(total + item.AmountMinorUnits));
        var discountTotal = discountRows.Aggregate(0L, (total, item) => checked(total + item.DiscountAmountMinorUnits));
        var vatExemptionTotal = discountRows.Aggregate(0L, (total, item) => checked(total + item.VatExemptionAmountMinorUnits));
        var rangeCount = rangeRows.Aggregate(0L, (total, item) => checked(total + item.QualifyingDocumentCount));
        var gapCount = rangeRows.Aggregate(0L, (total, item) => checked(total + item.Gaps.Count));
        var reconciliation = new FiscalReportReconciliationPresentation(
            "RECONCILED",
            amounts.NetSalesAmountMinorUnits,
            tenderTotal,
            discountTotal,
            rangeCount,
            gapCount,
            tenderRows.Length == 0 || tenderTotal == amounts.NetSalesAmountMinorUnits,
            discountRows.Length == 0 ||
                (discountTotal == amounts.DiscountAmountMinorUnits &&
                 vatExemptionTotal == amounts.VatExemptionAmountMinorUnits),
            rangeRows.Length == 0 || rangeCount == documentCount,
            counters is null || counters.CurrentPeriodGrandTotalAmountMinorUnits == amounts.NetSalesAmountMinorUnits);

        var model = new FiscalReportPresentationModel(
            presentationVersion,
            reportKind,
            title,
            reportStatus,
            periodStatus,
            finality,
            reportReference,
            operationReference,
            sitePosServerId,
            "not_recorded",
            fiscalIdentityId,
            periodId,
            priorPeriodId,
            periodSequence,
            businessDate,
            periodStart,
            periodEnd,
            timezone,
            cutoff,
            currency,
            generated,
            committed,
            closed,
            sourceContractVersion,
            sourceHashVersion,
            string.Empty,
            documentCount,
            amounts,
            tenderRows,
            discountRows,
            rangeRows,
            counters,
            reconciliation,
            correlation,
            support,
            postures.OrderBy(item => item.Field, StringComparer.Ordinal).ToArray(),
            statements,
            immutable);
        var snapshotIdentity = FiscalReportOutputRenderer.ComputeSnapshotIdentity(model);
        return FiscalReportPresentationResult.Success(model with { SnapshotIdentity = snapshotIdentity });
    }

    private static bool AmountsAreNonNegative(FiscalXReadingAmounts amounts) =>
        amounts.GetType().GetProperties()
            .Where(property => property.PropertyType == typeof(long))
            .All(property => (long)property.GetValue(amounts)! >= 0);

    private static bool TryAdd(long left, long right, out long result)
    {
        try
        {
            result = checked(left + right);
            return true;
        }
        catch (OverflowException)
        {
            result = 0;
            return false;
        }
    }

    private static FiscalReportPresentationResult Malformed() =>
        FiscalReportPresentationResult.Failure(
            FiscalReportPresentationErrorCode.MalformedSnapshot,
            "The stored fiscal report snapshot failed presentation integrity validation.");
}
