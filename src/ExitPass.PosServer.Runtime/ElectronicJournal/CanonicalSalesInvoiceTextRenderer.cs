using System.Globalization;
using System.Text;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

/// <summary>
/// Serializes the canonical Digital Sales Invoice model as visible plain text. Physical printer
/// adapters and Electronic Journal export share this serializer; printer control bytes are applied later.
/// </summary>
public sealed class CanonicalSalesInvoiceTextRenderer
{
    private const int ReceiptWidth = 35;
    private const int SeparatorWidth = 32;
    private const int CustomerLabelWidth = 13;
    private const int CustomerValueStart = CustomerLabelWidth + 1;
    private static readonly string Separator = new('-', SeparatorWidth);
    private const string ClosingText = "THANK YOU FOR CHOOSING OUR SERVICE";

    public CanonicalSalesInvoiceTextOutput Render(DigitalSalesInvoiceRenderModel invoice) =>
        Render(invoice, invoice.CopyDesignation);

    internal CanonicalSalesInvoiceTextOutput Render(
        DigitalSalesInvoiceRenderModel invoice,
        string copyDesignation)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var header = invoice.SalesInvoiceHeaderSnapshot ??
            throw new InvalidOperationException("The authoritative Sales Invoice header snapshot is unavailable.");
        var fiscal = invoice.FiscalContent ??
            throw new InvalidOperationException("The canonical Sales Invoice fiscal content is unavailable.");
        if (!string.Equals(invoice.FiscalNumberAssignmentState, "assigned", StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(invoice.FiscalDocumentNumber) ||
            invoice.FiscalNumberAssignedAt is null ||
            invoice.BusinessDayDate is null)
            throw new InvalidOperationException("Only complete, issued Sales Invoices can be rendered.");

        var currency = RequireCurrency(fiscal.CurrencyCode);
        var lines = new List<string>();
        AddCentered(lines, header.RegisteredBusinessName);
        AddCentered(lines, header.RegisteredBusinessAddress);
        AddValue(lines, "VAT REG TIN", header.Tin);
        AddValue(lines, "MIN", header.MachineIdentificationNumber);
        AddValue(lines, "S/N", header.PosSerialNumber);
        AddValue(lines, "BRANCH / SITE", fiscal.BranchOrSite);
        AddValue(lines, "PARKING LOCATION", header.ParkingLocationDisplay);

        lines.Add(string.Empty);
        AddCentered(lines, RequireDocumentDesignation(invoice.DocumentDesignation));
        AddCentered(lines, RequireCopyLabel(copyDesignation));
        lines.Add(string.Empty);
        AddValue(lines, "SI NO", invoice.FiscalDocumentNumber);
        AddValue(lines, "ISSUED", LocalTimestamp(invoice.FiscalNumberAssignedAt.Value));
        AddValue(lines, "BUSINESS DATE", invoice.BusinessDayDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddValue(lines, "CASHIER", fiscal.CashierDisplayName);

        lines.Add(string.Empty);
        AddValue(lines, "TICKET", fiscal.TicketNumber);
        AddValue(lines, "PLATE", fiscal.PlateNumber);
        AddValue(lines, "ENTRY", CompactTimestamp(fiscal.EntryTimeText));
        AddValue(lines, "PAYMENT", CompactTimestamp(fiscal.PaymentTimeText));
        AddValue(lines, "DURATION", CompactDuration(fiscal.ParkingDurationText));

        lines.Add(string.Empty);
        lines.Add(Separator);
        lines.Add("PARKING FEE");
        foreach (var item in invoice.Lines.OrderBy(item => item.LineSequence))
            AddCompactItem(lines, item, currency);
        lines.Add(Separator);
        AddValue(lines, "SUBTOTAL", Amount(fiscal.SubtotalAmountMinorUnits));
        AddValue(lines, "DISCOUNT", Amount(fiscal.DiscountAmountMinorUnits));
        AddValue(lines, "VATABLE SALES", Amount(fiscal.VatableSalesMinorUnits));
        AddValue(lines, "VAT AMOUNT", Amount(fiscal.VatAmountMinorUnits));
        AddValue(lines, "VAT EXEMPT SALES", Amount(fiscal.VatExemptSalesMinorUnits));
        AddValue(lines, "ZERO RATED SALES", Amount(fiscal.ZeroRatedSalesMinorUnits));
        AddValue(lines, "TOTAL", Amount(fiscal.TotalAmountMinorUnits));

        lines.Add(string.Empty);
        lines.Add("PAYMENT");
        AddValue(lines, "TYPE", PaymentType(invoice, fiscal));
        AddValue(lines, "AMOUNT DUE", Amount(fiscal.TotalAmountMinorUnits));
        AddValue(lines, "TOTAL PAID", Amount(fiscal.TotalPaidMinorUnits));
        if (IsCash(fiscal.PaymentMethod))
        {
            if (fiscal.TenderedAmountMinorUnits is { } tendered) AddValue(lines, "TENDERED", Amount(tendered));
            if (fiscal.ChangeAmountMinorUnits is { } change) AddValue(lines, "CHANGE", Amount(change));
        }

        lines.Add(string.Empty);
        lines.Add(Separator);
        AddCentered(lines, header.SalesInvoiceLegalStatement);
        AddValue(lines, "PRINT DATE", LocalTimestamp(invoice.FiscalNumberAssignedAt.Value));

        var customer = invoice.InvoiceCustomerInformation;
        lines.Add(string.Empty);
        lines.Add("CUSTOMER INFORMATION");
        AddCustomerValue(lines, "NAME", customer?.CustomerName);
        AddCustomerValue(lines, "ADDRESS", customer?.Address);
        AddCustomerValue(lines, "TIN", customer?.Tin);
        AddCustomerValue(lines, "BUS. STYLE", customer?.BusinessStyle);
        var statutoryId = invoice.AppliedStatutoryFiscalFacts is null ? null : customer?.StatutoryIdNumber;
        if (!string.IsNullOrWhiteSpace(statutoryId))
            AddCustomerValue(lines, StatutoryIdLabel(invoice.AppliedStatutoryFiscalFacts?.EntitlementType), statutoryId);
        if (fiscal.ShowCustomerSignatureLine) AddCustomerValue(lines, "Customer Sign", "____________________");

        lines.Add(string.Empty);
        lines.Add("POS SOFTWARE SUPPLIER / DEVELOPER");
        AddCentered(lines, header.SupplierDeveloperRegisteredName);
        AddCentered(lines, header.SupplierDeveloperAddress);
        AddValue(lines, "TIN", header.SupplierDeveloperTin);
        AddValue(lines, "ACCR NO", header.BirAccreditationNumber);
        AddValue(lines, "DATE ISSUED", header.BirAccreditationIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddValue(lines, "PTU", header.PtuNumber);
        AddValue(lines, "PTU DATE", header.PtuIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(header.CustomerServiceFooter) &&
            !string.Equals(header.CustomerServiceFooter.Trim(), ClosingText, StringComparison.OrdinalIgnoreCase))
            AddCentered(lines, header.CustomerServiceFooter.Trim());
        foreach (var closingLine in invoice.Footer.ClosingTextLines ?? [])
            if (!string.IsNullOrWhiteSpace(closingLine) &&
                !string.Equals(closingLine.Trim(), ClosingText, StringComparison.OrdinalIgnoreCase) &&
                !closingLine.Contains("NOTHING FOLLOWS", StringComparison.OrdinalIgnoreCase))
                AddCentered(lines, closingLine.Trim());
        AddCentered(lines, ClosingText);
        AddCentered(lines, "===== NOTHING FOLLOWS =====");

        var text = string.Join("\r\n", lines) + "\r\n";
        return new(text, Encoding.UTF8.GetBytes(text));
    }

    private static string StatutoryIdLabel(string? entitlementType) =>
        entitlementType?.Trim().ToUpperInvariant() switch
        {
            "PWD" or "PERSON_WITH_DISABILITY" => "PWD ID No.",
            "SENIOR_CITIZEN" or "SENIOR" => "OSCA ID No.",
            _ => "OSCA ID No. / PWD ID No."
        };

    private static string Money(long minorUnits, string currency) =>
        $"{currency} {(minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture)}";

    private static string Amount(long minorUnits) =>
        (minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture);

    private static void AddCompactItem(List<string> lines, DigitalSalesInvoiceLineRenderModel item, string currency)
    {
        var left = $"{item.Quantity:0.##} x {Money(item.UnitAmountMinorUnits, currency)}";
        var right = Amount(item.NetAmountMinorUnits);
        if (left.Length + right.Length + 1 <= ReceiptWidth)
            lines.Add(left + new string(' ', ReceiptWidth - left.Length - right.Length) + right);
        else
        {
            lines.Add(left);
            lines.Add(right.PadLeft(ReceiptWidth));
        }
    }

    private static string PaymentType(
        DigitalSalesInvoiceRenderModel invoice,
        DigitalSalesInvoiceFiscalContentRenderModel fiscal) =>
        !string.IsNullOrWhiteSpace(fiscal.PaymentMethod)
            ? fiscal.PaymentMethod.Trim().ToUpperInvariant()
            : invoice.Tenders.Count == 0 && fiscal.TotalAmountMinorUnits == 0
                ? "NO PAYMENT REQUIRED"
                : invoice.Tenders.Select(value => value.TenderTypeCodeKey?.Trim().ToUpperInvariant())
                    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "NOT RECORDED";

    private static bool IsCash(string? paymentMethod) =>
        string.Equals(paymentMethod?.Trim(), "CASH", StringComparison.OrdinalIgnoreCase);

    private static void AddValue(List<string> lines, string label, string? value)
    {
        var resolved = value?.Trim() ?? string.Empty;
        if (resolved.Length == 0)
        {
            lines.Add(label);
            return;
        }
        if (label.Length + resolved.Length + 1 <= ReceiptWidth)
        {
            lines.Add(label + new string(' ', ReceiptWidth - label.Length - resolved.Length) + resolved);
            return;
        }
        lines.Add(label);
        foreach (var line in Wrap(resolved, ReceiptWidth)) lines.Add(line.PadLeft(ReceiptWidth));
    }

    private static void AddCustomerValue(List<string> lines, string label, string? value)
    {
        var resolved = value?.Trim() ?? string.Empty;
        if (resolved.Length == 0)
        {
            lines.Add(label);
            return;
        }

        var valueIndent = new string(' ', CustomerValueStart);
        var wrappedValue = Wrap(resolved, ReceiptWidth - CustomerValueStart).ToArray();
        lines.Add(label.PadRight(CustomerValueStart) + wrappedValue[0]);
        foreach (var continuation in wrappedValue.Skip(1))
            lines.Add(valueIndent + continuation);
    }

    private static void AddCentered(List<string> lines, string value)
    {
        foreach (var line in Wrap(value, ReceiptWidth))
            lines.Add(line.Length >= ReceiptWidth ? line : line.PadLeft(line.Length + ((ReceiptWidth - line.Length) / 2)));
    }

    private static IEnumerable<string> Wrap(string value, int width)
    {
        var remaining = value.Trim();
        if (remaining.Length == 0) { yield return string.Empty; yield break; }
        while (remaining.Length > width)
        {
            var split = remaining.LastIndexOf(' ', width - 1, width);
            if (split <= 0) split = width;
            yield return remaining[..split].TrimEnd();
            remaining = remaining[split..].TrimStart();
        }
        yield return remaining;
    }

    private static string LocalTimestamp(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string? CompactTimestamp(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var normalized = value.Trim();
        if (normalized.EndsWith(" PHT", StringComparison.OrdinalIgnoreCase))
        {
            var localText = normalized[..^4].TrimEnd();
            return DateTime.TryParse(localText, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var local)
                ? local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                : localText;
        }
        if (normalized.EndsWith(" UTC", StringComparison.OrdinalIgnoreCase))
            normalized = normalized[..^4] + "Z";
        return DateTimeOffset.TryParse(
                normalized,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal,
                out var parsed)
            ? LocalTimestamp(parsed)
            : normalized;
    }

    private static string? CompactDuration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var normalized = value.Trim();
        if (TimeSpan.TryParse(normalized, CultureInfo.InvariantCulture, out var duration) && duration >= TimeSpan.Zero)
        {
            var totalHours = (long)Math.Floor(duration.TotalHours);
            return $"{totalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
        }
        return normalized;
    }

    private static string RequireDocumentDesignation(string value) =>
        string.Equals(value, "SALES INVOICE", StringComparison.Ordinal)
            ? value
            : throw new ArgumentException("The fiscal document designation must be SALES INVOICE.", nameof(value));

    private static string RequireCopyLabel(string value) => value is "ORIGINAL" or "REPRINT"
        ? value
        : throw new ArgumentException("Sales Invoice copy label must be ORIGINAL or REPRINT.", nameof(value));

    private static string RequireCurrency(string value)
    {
        var currency = value?.Trim().ToUpperInvariant();
        return currency is { Length: 3 } && currency.All(char.IsAsciiLetterUpper)
            ? currency
            : throw new ArgumentException("A three-letter uppercase currency code is required.", nameof(value));
    }
}

public sealed record CanonicalSalesInvoiceTextOutput(string Text, byte[] Bytes);

/// <summary>
/// Produces the visible Sales Invoice printer payload. Device-specific feed/cut bytes may
/// be appended by a printer adapter, but the human-readable bytes come only from the
/// same canonical renderer used by the Electronic Journal.
/// </summary>
public sealed class SalesInvoicePrinterPayloadRenderer(CanonicalSalesInvoiceTextRenderer canonicalRenderer)
{
    public CanonicalSalesInvoiceTextOutput RenderVisibleText(DigitalSalesInvoiceRenderModel invoice) =>
        canonicalRenderer.Render(invoice);

    public CanonicalSalesInvoiceTextOutput RenderReprintVisibleText(
        DigitalSalesInvoiceRenderModel invoice,
        FiscalDocumentReprintRecord reprint)
    {
        ArgumentNullException.ThrowIfNull(reprint);
        if (reprint.FiscalDocumentId != invoice.FiscalDocumentId ||
            !string.Equals(reprint.FiscalDocumentNumber, invoice.FiscalDocumentNumber, StringComparison.Ordinal) ||
            !string.Equals(reprint.ReprintStatus, "committed", StringComparison.Ordinal) ||
            !reprint.ReprintLabelApplied)
            throw new InvalidOperationException("A committed matching governed reprint record is required.");
        return canonicalRenderer.Render(invoice, "REPRINT");
    }
}

public sealed record ElectronicJournalInvoiceTextExport(
    string ContentType,
    string FileName,
    string Text,
    byte[] Bytes);

public sealed class ElectronicJournalInvoiceTextRenderer(CanonicalSalesInvoiceTextRenderer canonicalRenderer)
{
    public ElectronicJournalInvoiceTextExport Render(IEnumerable<DigitalSalesInvoiceRenderModel> invoices)
    {
        var ordered = invoices
            .GroupBy(invoice => invoice.FiscalDocumentId)
            .Select(group => group.OrderBy(invoice => invoice.FiscalNumberAssignedAt).First())
            .OrderBy(invoice => invoice.FiscalNumberAssignedAt)
            .ThenBy(invoice => invoice.FiscalDocumentNumber, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length == 0) throw new ArgumentException("At least one Sales Invoice is required.", nameof(invoices));
        if (ordered.Any(invoice => invoice.BusinessDayDate is null))
            throw new InvalidOperationException("Every Sales Invoice requires an immutable business date.");

        var outputs = ordered.Select(canonicalRenderer.Render).ToArray();
        var text = string.Join("\r\n", outputs.Select(output => output.Text));
        var firstDate = ordered[0].BusinessDayDate!.Value;
        var lastDate = ordered[^1].BusinessDayDate!.Value;
        var fileDate = firstDate == lastDate
            ? firstDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : $"{firstDate:yyyyMMdd}-{lastDate:yyyyMMdd}";
        return new("text/plain; charset=utf-8", $"electronic-journal-{fileDate}.txt", text, Encoding.UTF8.GetBytes(text));
    }

    public ElectronicJournalInvoiceTextExport RenderPersisted(IEnumerable<ElectronicJournalInvoiceTextItem> invoices)
    {
        var ordered = invoices.GroupBy(value => value.FiscalDocumentId)
            .Select(group => group.OrderBy(value => value.IssuedAt).First())
            .OrderBy(value => value.IssuedAt).ThenBy(value => value.FiscalDocumentNumber, StringComparer.Ordinal).ToArray();
        if (ordered.Length == 0) throw new ArgumentException("At least one Sales Invoice is required.", nameof(invoices));
        var text = string.Join("\r\n", ordered.Select(value => value.PrintableText));
        var fileDate = ordered[0].BusinessDayDate == ordered[^1].BusinessDayDate
            ? ordered[0].BusinessDayDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : $"{ordered[0].BusinessDayDate:yyyyMMdd}-{ordered[^1].BusinessDayDate:yyyyMMdd}";
        return new("text/plain; charset=utf-8", $"electronic-journal-{fileDate}.txt", text, Encoding.UTF8.GetBytes(text));
    }
}
