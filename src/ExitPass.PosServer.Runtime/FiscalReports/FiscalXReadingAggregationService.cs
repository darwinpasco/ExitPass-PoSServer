using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed record FiscalXReadingSourceLine(string LineType, long Gross, long Discount, long Tax, long Net, string Currency);
public sealed record FiscalXReadingSourceTender(string TenderType, long Amount, string Currency);
public sealed record FiscalXReadingSourceTax(string Classification, bool IsDocumentLevel, long TaxableAmount, long TaxAmount, string Currency);
public sealed record FiscalXReadingSourceDiscount(string Classification, long DiscountAmount, long VatPrivilegeAmount, string Currency);
public sealed record FiscalXReadingSourceStatutory(string Entitlement, long DiscountAmount, long VatAmount, long FinalAmount, string Currency);
public sealed record FiscalXReadingSourceDocument(
    Guid FiscalDocumentId,
    string DocumentType,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? SequencePolicyId,
    long? SequenceValue,
    string? FiscalDocumentNumber,
    string? FiscalSeries,
    IReadOnlyList<FiscalXReadingSourceLine> Lines,
    IReadOnlyList<FiscalXReadingSourceTender> Tenders,
    IReadOnlyList<FiscalXReadingSourceTax> Taxes,
    IReadOnlyList<FiscalXReadingSourceDiscount> Discounts,
    FiscalXReadingSourceStatutory? Statutory,
    string CompletionBasis = FiscalCompletionBasisCodes.PaymentFinality);

public sealed record FiscalXReadingSourceGap(Guid SourceSequenceGapAuditId, Guid SequencePolicyId, long SequenceValue, string Classification);

public sealed record FiscalXReadingAggregate(
    long QualifyingDocumentCount,
    Guid? BeginFiscalDocumentId,
    Guid? EndFiscalDocumentId,
    string? BeginningFiscalNumber,
    string? EndingFiscalNumber,
    long? FirstSequenceValue,
    long? LastSequenceValue,
    FiscalXReadingAmounts Amounts,
    IReadOnlyList<FiscalXReadingTenderBreakdown> Tenders,
    IReadOnlyList<FiscalXReadingDiscountBreakdown> Discounts,
    IReadOnlyList<FiscalXReadingFiscalNumberRange> FiscalNumberRanges);

public sealed class FiscalXReadingAggregationService
{
    private static readonly HashSet<string> ExcludedStatuses = new(StringComparer.Ordinal)
    {
        "failed", "requested", "processing", "in_progress", "uncertain", "unknown_commit_outcome", "rejected"
    };

    private static readonly HashSet<string> TenderClassifications = new(StringComparer.Ordinal)
    {
        "cash", "card", "digital_wallet", "bank_transfer", "other_non_cash"
    };

    public FiscalXReadingAggregate Aggregate(
        IReadOnlyList<FiscalXReadingSourceDocument> sourceDocuments,
        IReadOnlyList<FiscalXReadingSourceGap> sourceGaps,
        string currency)
    {
        try
        {
            return AggregateChecked(sourceDocuments, sourceGaps, currency);
        }
        catch (OverflowException)
        {
            throw new FiscalXReadingSafeException(FiscalXReadingOutcome.ArithmeticOverflow, "X Reading arithmetic exceeded supported minor-unit bounds.");
        }
    }

    private static FiscalXReadingAggregate AggregateChecked(
        IReadOnlyList<FiscalXReadingSourceDocument> sourceDocuments,
        IReadOnlyList<FiscalXReadingSourceGap> sourceGaps,
        string currency)
    {
        checked
        {
            var sales = new List<FiscalXReadingSourceDocument>();
            var voided = new List<FiscalXReadingSourceDocument>();
            foreach (var document in sourceDocuments)
            {
                if (!string.Equals(document.DocumentType, "sales_invoice", StringComparison.Ordinal))
                {
                    throw Unsupported("Unsupported fiscal document type is present in the reporting period.");
                }

                if (document.Status is "recorded" or "issued") sales.Add(document);
                else if (string.Equals(document.Status, "voided", StringComparison.Ordinal)) voided.Add(document);
                else if (!ExcludedStatuses.Contains(document.Status)) throw Unsupported("Unsupported fiscal document status is present in the reporting period.");
            }

            var qualifying = sales.Concat(voided).OrderBy(document => document.CreatedAt).ThenBy(document => document.FiscalDocumentId).ToArray();
            var gross = 0L; var net = 0L; var totalDiscount = 0L;
            var vatable = 0L; var vat = 0L; var vatExempt = 0L; var zeroRated = 0L;
            var senior = 0L; var pwd = 0L; var otherStatutory = 0L; var vatExemption = 0L;
            var coupon = 0L; var promotional = 0L; var voidAmount = 0L;
            var tenderTotals = new Dictionary<string, (long Count, long Amount)>(StringComparer.Ordinal);
            var discountTotals = new Dictionary<string, (long Count, long Discount, long Vat)>(StringComparer.Ordinal);

            foreach (var document in sales)
            {
                ValidateDocumentCurrency(document, currency);
                if (document.Lines.Count == 0)
                    throw Reconciliation("A recorded Sales Invoice is missing required line facts.");

                var zeroPayableStatutoryCompletion = string.Equals(
                    document.CompletionBasis,
                    FiscalCompletionBasisCodes.ZeroPayableStatutoryFinality,
                    StringComparison.Ordinal);
                if (zeroPayableStatutoryCompletion)
                {
                    if (document.Tenders.Count != 0 || document.Statutory is null)
                        throw Reconciliation("A zero-payable statutory Sales Invoice must have statutory facts and no tender facts.");
                }
                else if (string.Equals(
                             document.CompletionBasis,
                             FiscalCompletionBasisCodes.PaymentFinality,
                             StringComparison.Ordinal))
                {
                    if (document.Tenders.Count == 0)
                        throw Reconciliation("A payment-finality Sales Invoice is missing required tender facts.");
                }
                else
                {
                    throw Unsupported("Unsupported fiscal completion basis is present in the reporting period.");
                }

                if (document.Lines.Any(line => string.Equals(line.LineType, "service_charge", StringComparison.Ordinal)))
                    throw Unsupported("Service-charge reporting is not governed by the current contract.");

                var documentGross = document.Lines.SumChecked(line => line.Gross);
                var documentNet = document.Lines.SumChecked(line => line.Net);
                var documentDiscount = document.Lines.SumChecked(line => line.Discount);
                var documentTender = document.Tenders.SumChecked(tender => tender.Amount);
                if (documentTender != documentNet)
                    throw Reconciliation("Fiscal tender totals do not reconcile to the recorded Sales Invoice net amount.");

                gross += documentGross; net += documentNet; totalDiscount += documentDiscount;

                var taxModes = document.Taxes.GroupBy(tax => tax.Classification, StringComparer.Ordinal);
                foreach (var group in taxModes)
                {
                    if (group.Any(tax => tax.IsDocumentLevel) && group.Any(tax => !tax.IsDocumentLevel))
                        throw Reconciliation("Mixed document-level and line-level tax facts would double count a tax classification.");
                    var taxable = group.SumChecked(tax => tax.TaxableAmount);
                    var taxAmount = group.SumChecked(tax => tax.TaxAmount);
                    switch (group.Key)
                    {
                        case "vatable": vatable += taxable; vat += taxAmount; break;
                        case "vat_exempt": vatExempt += taxable; vat += taxAmount; break;
                        case "zero_rated": zeroRated += taxable; vat += taxAmount; break;
                        default: throw Unsupported("Unsupported fiscal tax classification is present in the reporting period.");
                    }
                }

                foreach (var tender in document.Tenders)
                {
                    if (!TenderClassifications.Contains(tender.TenderType)) throw Unsupported("Unsupported fiscal tender classification is present in the reporting period.");
                    var current = tenderTotals.GetValueOrDefault(tender.TenderType);
                    tenderTotals[tender.TenderType] = (current.Count + 1, current.Amount + tender.Amount);
                }

                var statutoryDiscount = 0L;
                if (document.Statutory is not null)
                {
                    statutoryDiscount = document.Statutory.DiscountAmount;
                    var classification = document.Statutory.Entitlement switch
                    {
                        "SENIOR_CITIZEN" => "senior_citizen_statutory",
                        "PWD" => "pwd_statutory",
                        _ => throw Unsupported("Unsupported statutory entitlement classification is present in the reporting period.")
                    };
                    if (document.Statutory.FinalAmount != documentNet) throw Reconciliation("Applied statutory final amount does not reconcile to the Sales Invoice net amount.");
                    if (classification == "senior_citizen_statutory") senior += statutoryDiscount; else pwd += statutoryDiscount;
                    AddDiscount(discountTotals, classification, statutoryDiscount, 0);
                }

                var privilegeDiscount = 0L;
                foreach (var detail in document.Discounts)
                {
                    if (document.Statutory is not null)
                    {
                        privilegeDiscount += detail.DiscountAmount;
                        vatExemption += detail.VatPrivilegeAmount;
                        if (detail.VatPrivilegeAmount > 0) AddDiscount(discountTotals, "vat_exemption_adjustment", 0, detail.VatPrivilegeAmount);
                    }
                    else if (detail.Classification is "coupon" or "promotional")
                    {
                        if (detail.Classification == "coupon") coupon += detail.DiscountAmount; else promotional += detail.DiscountAmount;
                        AddDiscount(discountTotals, detail.Classification, detail.DiscountAmount, detail.VatPrivilegeAmount);
                    }
                    else
                    {
                        throw Unsupported("Unsupported fiscal discount classification is present in the reporting period.");
                    }
                }

                if (document.Statutory is not null && privilegeDiscount != statutoryDiscount)
                    throw Reconciliation("Statutory privilege detail does not reconcile to the immutable applied statutory snapshot.");
                if (documentDiscount != statutoryDiscount + document.Discounts.Where(d => document.Statutory is null).SumChecked(d => d.DiscountAmount))
                    throw Reconciliation("Line discount totals do not reconcile to governed statutory and commercial discount facts.");
            }

            foreach (var document in voided)
            {
                ValidateDocumentCurrency(document, currency);
                voidAmount += document.Lines.SumChecked(line => line.Net);
            }

            var ranges = BuildRanges(qualifying, sourceGaps, currency);
            var first = qualifying.FirstOrDefault(); var last = qualifying.LastOrDefault();
            var amounts = new FiscalXReadingAmounts(gross, net, vatable, vat, vatExempt, zeroRated, totalDiscount,
                senior, pwd, otherStatutory, vatExemption, coupon, promotional, voidAmount, 0, 0, 0, 0);
            return new FiscalXReadingAggregate(
                qualifying.LongLength, first?.FiscalDocumentId, last?.FiscalDocumentId,
                first?.FiscalDocumentNumber, last?.FiscalDocumentNumber,
                ranges.Count == 1 ? ranges[0].FirstSequenceValue : null,
                ranges.Count == 1 ? ranges[0].LastSequenceValue : null,
                amounts,
                tenderTotals.OrderBy(pair => pair.Key).Select(pair => new FiscalXReadingTenderBreakdown(pair.Key, pair.Value.Count, pair.Value.Amount, currency)).ToArray(),
                discountTotals.OrderBy(pair => pair.Key).Select(pair => new FiscalXReadingDiscountBreakdown(pair.Key, pair.Value.Count, pair.Value.Discount, pair.Value.Vat, currency)).ToArray(),
                ranges);
        }
    }

    private static IReadOnlyList<FiscalXReadingFiscalNumberRange> BuildRanges(
        IReadOnlyList<FiscalXReadingSourceDocument> documents,
        IReadOnlyList<FiscalXReadingSourceGap> gaps,
        string currency)
    {
        if (documents.Any(document => document.SequencePolicyId is null || document.SequenceValue is null ||
            string.IsNullOrWhiteSpace(document.FiscalDocumentNumber) || string.IsNullOrWhiteSpace(document.FiscalSeries)))
            throw Reconciliation("A qualifying Sales Invoice is missing assigned fiscal-number facts.");

        var result = new List<FiscalXReadingFiscalNumberRange>();
        foreach (var group in documents.GroupBy(document => (document.SequencePolicyId!.Value, document.FiscalSeries!), new RangeKeyComparer()))
        {
            var ordered = group.OrderBy(document => document.SequenceValue).ToArray();
            var first = ordered[0].SequenceValue!.Value; var last = ordered[^1].SequenceValue!.Value;
            var expectedMissing = Enumerable.Range(0, checked((int)(last - first + 1))).Select(offset => first + offset)
                .Except(ordered.Select(document => document.SequenceValue!.Value)).ToArray();
            var matchingGaps = gaps.Where(gap => gap.SequencePolicyId == group.Key.Item1 && gap.SequenceValue >= first && gap.SequenceValue <= last).OrderBy(gap => gap.SequenceValue).ToArray();
            if (!expectedMissing.SequenceEqual(matchingGaps.Select(gap => gap.SequenceValue)))
                throw Reconciliation("Fiscal sequence range contains an unexplained or inconsistent gap.");
            if (matchingGaps.Any(gap => gap.Classification is not ("voided_document" or "failed_issuance" or "reserved_not_issued" or "unexplained")))
                throw Unsupported("Unsupported fiscal sequence-gap classification is present.");
            if (matchingGaps.Any(gap => gap.Classification == "unexplained"))
                throw Reconciliation("An unexplained fiscal sequence gap blocks X Reading generation.");

            result.Add(new FiscalXReadingFiscalNumberRange(group.Key.Item1, group.Key.Item2, first, last,
                ordered[0].FiscalDocumentNumber!, ordered[^1].FiscalDocumentNumber!, ordered.LongLength,
                matchingGaps.Select(gap => new FiscalXReadingSequenceGap(gap.SequenceValue, gap.Classification)).ToArray(), currency));
        }
        return result;
    }

    private static void ValidateDocumentCurrency(FiscalXReadingSourceDocument document, string currency)
    {
        if (document.Lines.Any(line => line.Currency != currency) || document.Tenders.Any(tender => tender.Currency != currency) ||
            document.Taxes.Any(tax => tax.Currency != currency) || document.Discounts.Any(detail => detail.Currency != currency) ||
            (document.Statutory is not null && document.Statutory.Currency != currency))
            throw new FiscalXReadingSafeException(FiscalXReadingOutcome.MixedCurrency, "Mixed-currency fiscal facts cannot be aggregated into one X Reading.");
    }

    private static void AddDiscount(Dictionary<string, (long Count, long Discount, long Vat)> totals, string key, long discount, long vat)
    {
        var current = totals.GetValueOrDefault(key);
        totals[key] = (current.Count + 1, checked(current.Discount + discount), checked(current.Vat + vat));
    }

    private static FiscalXReadingSafeException Unsupported(string message) => new(FiscalXReadingOutcome.UnsupportedSourceClassification, message);
    private static FiscalXReadingSafeException Reconciliation(string message) => new(FiscalXReadingOutcome.ReconciliationFailure, message);

    private sealed class RangeKeyComparer : IEqualityComparer<(Guid, string)>
    {
        public bool Equals((Guid, string) x, (Guid, string) y) => x.Item1 == y.Item1 && string.Equals(x.Item2, y.Item2, StringComparison.Ordinal);
        public int GetHashCode((Guid, string) value) => HashCode.Combine(value.Item1, StringComparer.Ordinal.GetHashCode(value.Item2));
    }
}

internal static class CheckedEnumerableExtensions
{
    public static long SumChecked<T>(this IEnumerable<T> source, Func<T, long> selector)
    {
        var total = 0L;
        checked { foreach (var item in source) total += selector(item); }
        return total;
    }
}
