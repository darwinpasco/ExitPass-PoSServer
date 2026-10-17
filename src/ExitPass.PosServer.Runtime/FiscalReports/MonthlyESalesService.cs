using System.Globalization;

namespace ExitPass.PosServer.Runtime.FiscalReports;

public sealed class MonthlyESalesService(IMonthlyESalesRepository repository)
{
    public Task<MonthlyESalesResult> ReadAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.SitePosServerId == Guid.Empty || query.FiscalIdentityId == Guid.Empty ||
            !string.Equals(query.CurrencyCode, MonthlyESalesContract.CurrencyCode, StringComparison.Ordinal) ||
            query.CalendarYear is < 2000 or > 9999 || query.CalendarMonth is < 1 or > 12)
        {
            return Task.FromResult(new MonthlyESalesResult(
                MonthlyESalesOutcome.InvalidRequest,
                SafeMessage: "The Monthly eSales request is incomplete or invalid."));
        }

        return repository.ReadAsync(query, cancellationToken);
    }

    public Task<MonthlyESalesPdfResult> ReadPdfAsync(MonthlyESalesQuery query, CancellationToken cancellationToken = default)
    {
        if (query.SitePosServerId == Guid.Empty || query.FiscalIdentityId == Guid.Empty ||
            !string.Equals(query.CurrencyCode, MonthlyESalesContract.CurrencyCode, StringComparison.Ordinal) ||
            query.CalendarYear is < 2000 or > 9999 || query.CalendarMonth is < 1 or > 12)
            return Task.FromResult(new MonthlyESalesPdfResult(MonthlyESalesOutcome.InvalidRequest,
                SafeMessage: "The Monthly eSales request is incomplete or invalid."));
        return repository.ReadPdfAsync(query, cancellationToken);
    }
}

public static class MonthlyESalesAggregator
{
    public static MonthlyESalesResult Build(
        MonthlyESalesQuery query,
        DateOnly currentSiteDate,
        IReadOnlyList<MonthlyESalesSourcePeriod> sources)
    {
        var requestedMonth = new DateOnly(query.CalendarYear, query.CalendarMonth, 1);
        var currentMonth = new DateOnly(currentSiteDate.Year, currentSiteDate.Month, 1);
        if (requestedMonth >= currentMonth)
        {
            return new(MonthlyESalesOutcome.MonthNotComplete,
                SafeMessage: "Monthly eSales can be finalized only after the selected calendar month has ended.");
        }
        if (sources.Count == 0)
        {
            return new(MonthlyESalesOutcome.MissingAuthoritativeSource,
                SafeMessage: "No configured fiscal reporting period exists for the selected calendar month.");
        }

        var missingZ = sources.Where(source => !source.PeriodClosed || !source.GoverningZCommitted)
            .Select(source => source.BusinessDayDate).Distinct().Order().ToArray();
        var missingBir = sources.Where(source => source.PeriodClosed && source.GoverningZCommitted && !source.BirSalesSummaryCommitted)
            .Select(source => source.BusinessDayDate).Distinct().Order().ToArray();
        if (missingZ.Length > 0 || missingBir.Length > 0)
        {
            var gaps = new List<string>(2);
            if (missingZ.Length > 0)
                gaps.Add($"committed Z Readings are missing for: {Dates(missingZ)}");
            if (missingBir.Length > 0)
                gaps.Add($"committed BIR Sales Summaries are missing for: {Dates(missingBir)}");
            return new(MonthlyESalesOutcome.IncompleteAuthoritativeSources,
                SafeMessage: $"Monthly eSales cannot be finalized because {string.Join("; ", gaps)}.");
        }

        var committed = sources.OrderBy(source => source.PeriodSequence).ToArray();
        var tinValues = committed.Select(source => source.TaxpayerTin?.Trim() ?? string.Empty)
            .Distinct(StringComparer.Ordinal).ToArray();
        if (tinValues.Length != 1)
        {
            return new(MonthlyESalesOutcome.UnsupportedSourceClassification,
                SafeMessage: "The governed taxpayer TIN is not stable across the selected month.");
        }
        if (committed.Any(source => string.IsNullOrWhiteSpace(source.MachineIdentificationNumber)))
        {
            return new(MonthlyESalesOutcome.UnsupportedSourceClassification,
                SafeMessage: "A governed Machine Identification Number is unavailable for the selected month.");
        }

        try
        {
            var rows = committed
                .GroupBy(source => source.MachineIdentificationNumber!.Trim(), StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => BuildRow(query, group.Key, group.OrderBy(source => source.PeriodSequence).ToArray()))
                .ToArray();
            var tin = tinValues[0];
            var digits = new string(tin.Where(char.IsAsciiDigit).ToArray());
            DateOnly? dueDate = null;
            string? basis = null;
            if (digits.Length >= 9)
            {
                var even = (digits[8] - '0') % 2 == 0;
                var followingMonth = requestedMonth.AddMonths(1);
                dueDate = new DateOnly(followingMonth.Year, followingMonth.Month, even ? 8 : 10);
                basis = even ? "EVEN_TIN" : "ODD_TIN";
            }

            var uat = rows.Any(row => row.MachineIdentificationNumber.Length != 9 ||
                !row.MachineIdentificationNumber.All(char.IsAsciiDigit)) ||
                committed.Any(source => source.PosSerialNumber?.Contains("TEST", StringComparison.OrdinalIgnoreCase) == true);
            var readiness = dueDate is null
                ? MonthlyESalesSubmissionReadiness.FilingMetadataIncomplete
                : uat
                    ? MonthlyESalesSubmissionReadiness.TechnicalUat
                    : MonthlyESalesSubmissionReadiness.TechnicalReportReady;
            var readinessMessage = readiness switch
            {
                MonthlyESalesSubmissionReadiness.FilingMetadataIncomplete => "The report is readable, but a valid governed 9-digit taxpayer TIN is required for filing-date readiness.",
                MonthlyESalesSubmissionReadiness.TechnicalUat => "UAT fiscal identifier - not for BIR submission.",
                _ => "The authoritative Monthly eSales report is prepared for operator review and export; ExitPass has not submitted it to BIR."
            };

            return new(MonthlyESalesOutcome.Succeeded, new(
                query.SitePosServerId, query.FiscalIdentityId, query.CalendarYear, query.CalendarMonth,
                query.CurrencyCode, tin, dueDate, basis, readiness, readinessMessage, rows));
        }
        catch (MonthlyESalesSafeException exception)
        {
            return new(exception.Outcome, SafeMessage: exception.Message);
        }
        catch (OverflowException)
        {
            return new(MonthlyESalesOutcome.ArithmeticOverflow,
                SafeMessage: "Monthly eSales checked minor-unit aggregation exceeded supported bounds.");
        }
    }

    private static MonthlyESalesRow BuildRow(MonthlyESalesQuery query, string min, IReadOnlyList<MonthlyESalesSourcePeriod> sources)
    {
        long transactions = 0, gross = 0, vatable = 0, vat = 0, exempt = 0, zeroRated = 0;
        foreach (var source in sources)
        {
            transactions = checked(transactions + source.TransactionCount);
            gross = checked(gross + source.GrossSalesAmountMinorUnits);
            vatable = checked(vatable + source.VatableSalesAmountMinorUnits);
            vat = checked(vat + source.VatAmountMinorUnits);
            exempt = checked(exempt + source.VatExemptSalesAmountMinorUnits);
            zeroRated = checked(zeroRated + source.ZeroRatedSalesAmountMinorUnits);
        }
        var ending = sources[^1].EndingSalesInvoiceReference;
        if (transactions > 0 && string.IsNullOrWhiteSpace(ending))
            throw new MonthlyESalesSafeException(MonthlyESalesOutcome.MissingEndRange,
                $"Monthly eSales cannot be finalized because the ending Sales Invoice reference is unavailable for MIN {min}.");
        return new(min, query.CalendarMonth, query.CalendarYear, transactions, gross, Amount(gross), ending,
            vatable, vat, exempt, zeroRated, sources.Count, sources[0].BusinessDayDate, sources[^1].BusinessDayDate);
    }

    private static string Amount(long minorUnits) => (minorUnits / 100m).ToString("0.00", CultureInfo.InvariantCulture);
    private static string Dates(IEnumerable<DateOnly> dates) => string.Join(", ", dates.Select(date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));

    private sealed class MonthlyESalesSafeException(MonthlyESalesOutcome outcome, string message) : Exception(message)
    {
        public MonthlyESalesOutcome Outcome { get; } = outcome;
    }
}
