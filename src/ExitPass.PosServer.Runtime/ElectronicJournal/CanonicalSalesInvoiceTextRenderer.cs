using System.Globalization;
using System.Text;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

/// <summary>
/// Canonical visible Sales Invoice text. Physical printer adapters and Electronic Journal
/// export must consume these exact UTF-8 bytes; printer-only control bytes are applied later.
/// </summary>
public sealed class CanonicalSalesInvoiceTextRenderer
{
    private const string Separator = "------------------------------------------------";
    private const int ReceiptWidth = 48;

    public CanonicalSalesInvoiceTextOutput Render(CanonicalSalesInvoiceText invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var currency = RequireCurrency(invoice.CurrencyCode);
        var lines = new List<string>();
        AddCentered(lines, invoice.RegisteredBusinessName);
        AddCentered(lines, invoice.RegisteredBusinessAddress);
        lines.Add(string.Empty);
        AddValue(lines, "VAT REG TIN", invoice.SupplierTin);
        AddValue(lines, "MIN", invoice.MachineIdentificationNumber);
        AddValue(lines, "S/N", invoice.PosSerialNumber);
        AddValue(lines, "Branch / Site", invoice.Presentation?.BranchOrSite);
        AddValue(lines, "Parking Location", invoice.Presentation?.ParkingLocation);

        AddSection(lines, "SALES INVOICE");
        AddCentered(lines, RequireCopyLabel(invoice.CopyLabel));
        lines.Add(string.Empty);
        AddValue(lines, "SI No", invoice.FiscalDocumentNumber);
        AddValue(lines, "Issued Date", PhtTimestamp(invoice.IssuedAt));
        AddValue(lines, "Business Date", invoice.BusinessDayDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        AddSection(lines, "PARKING DETAILS");
        AddValue(lines, "Ticket Number", invoice.Presentation?.TicketNumber);
        AddValue(lines, "Plate Number", invoice.Presentation?.PlateNumber);
        AddValue(lines, "Entry Time", invoice.Presentation?.EntryTimeText);
        lines.Add(string.Empty);
        AddValue(lines, "Payment", invoice.Presentation?.PaymentTimeText);
        AddValue(lines, "Duration", invoice.Presentation?.DurationText);

        AddSection(lines, "ITEMS");
        lines.Add("# Description      Qty        Unit       Amount");
        foreach (var item in invoice.Lines.OrderBy(item => item.Sequence))
            AddItem(lines, item, currency);
        lines.Add(string.Empty);
        AddValue(lines, "Subtotal", Money(invoice.Presentation?.SubtotalAmountMinorUnits ?? invoice.TotalAmountMinorUnits, currency));

        AddSection(lines, "DISCOUNTS");
        var discounts = invoice.Presentation?.Discounts ?? [];
        if (discounts.Count == 0)
        {
            AddValue(lines, "Discount Reason", "NONE");
            AddValue(lines, "Discount Amount", Money(0, currency));
        }
        else
        {
            foreach (var discount in discounts)
            {
                AddValue(lines, "Discount Reason", discount.Label);
                AddValue(lines, "Discount Amount", $"-{Money(discount.AmountMinorUnits, currency)}");
            }
        }

        AddSection(lines, "VAT BREAKDOWN");
        AddValue(lines, "VATable Sales", Money(invoice.VatableSalesMinorUnits, currency));
        AddValue(lines, "VAT Amount", Money(invoice.VatAmountMinorUnits, currency));
        AddValue(lines, "VAT Exempt Sales", Money(invoice.VatExemptSalesMinorUnits, currency));
        AddValue(lines, "Zero Rated Sales", Money(invoice.ZeroRatedSalesMinorUnits, currency));

        AddSection(lines, "PAYMENT DETAILS");
        lines.Add("Type       Provider                      Amount");
        AppendPaymentDetails(lines, invoice.Presentation, currency, invoice.TotalAmountMinorUnits);

        lines.Add(Separator);
        AddCentered(lines, invoice.Presentation?.DeclarationText ?? string.Empty);
        lines.Add(string.Empty);
        AddValue(lines, "Print Date", PhtTimestamp(invoice.Presentation?.PresentationTimestamp ?? invoice.IssuedAt));

        if (HasCustomer(invoice.Customer))
        {
            AddSection(lines, "Customer Information");
            AddValue(lines, "NAME", invoice.Customer.Name);
            AddValue(lines, "ADDRESS", invoice.Customer.Address);
            AddValue(lines, "TIN", invoice.Customer.Tin);
            AddValue(lines, "BUS. STYLE", invoice.Customer.BusinessStyle);
            if (!string.IsNullOrWhiteSpace(invoice.Customer.StatutoryIdLabel) ||
                !string.IsNullOrWhiteSpace(invoice.Customer.StatutoryIdNumber))
                AddValue(lines, invoice.Customer.StatutoryIdLabel ?? "OSCA ID No. / PWD ID No.", invoice.Customer.StatutoryIdNumber);
            if (invoice.Customer.ShowSignatureLine) lines.Add("Customer Sign : __________________________");
        }

        AddSection(lines, "POS SOFTWARE SUPPLIER / DEVELOPER");
        if (invoice.Supplier is { } supplier)
        {
            AddCentered(lines, supplier.RegisteredName);
            AddCentered(lines, supplier.Address);
            lines.Add(string.Empty);
            AddValue(lines, "TIN", supplier.Tin);
            AddValue(lines, "ACCR. NO.", supplier.AccreditationNumber);
            AddValue(lines, "DATE ISSUED", supplier.AccreditationIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            AddValue(lines, "PTU", supplier.PtuNumber);
            AddValue(lines, "PTU Date", supplier.PtuIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        }
        if (!string.IsNullOrWhiteSpace(invoice.FooterText)) AddCentered(lines, invoice.FooterText.Trim());
        AddCentered(lines, "THANK YOU FOR CHOOSING OUR SERVICE");
        lines.Add(string.Empty);
        AddCentered(lines, "===== NOTHING FOLLOWS =====");

        var text = string.Join("\r\n", lines) + "\r\n";
        return new(text, Encoding.UTF8.GetBytes(text));
    }

    private static string Money(long minorUnits, string currency) =>
        $"{currency} {(minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture)}";

    private static void AddSection(List<string> lines, string title)
    {
        lines.Add(Separator);
        AddCentered(lines, title);
        lines.Add(Separator);
    }

    private static void AppendPaymentDetails(List<string> lines, CanonicalSalesInvoicePresentation? presentation, string currency, long total)
    {
        foreach (var tender in presentation?.Tenders ?? []) AddTender(lines, tender, currency);
        AddValue(lines, "Total Paid", Money(presentation?.TotalPaidMinorUnits ?? total, currency));
        if (presentation?.TenderedAmountMinorUnits is { } tendered) AddValue(lines, "Tendered", Money(tendered, currency));
        if (presentation?.ChangeAmountMinorUnits is { } change) AddValue(lines, "Change", Money(change, currency));
    }

    private static void AddItem(List<string> lines, CanonicalSalesInvoiceLine item, string currency)
    {
        var descriptions = Wrap(item.Description, 14).ToArray();
        var row = $"{item.Sequence,2} {descriptions[0],-14} {item.Quantity,4:0.##} {Money(item.UnitAmountMinorUnits, currency),11} {Money(item.AmountMinorUnits, currency),12}";
        if (row.Length <= ReceiptWidth)
        {
            lines.Add(row);
        }
        else
        {
            AddValue(lines, $"{item.Sequence} {descriptions[0]}", Money(item.AmountMinorUnits, currency));
            AddValue(lines, "Qty / Unit", $"{item.Quantity:0.##} / {Money(item.UnitAmountMinorUnits, currency)}");
        }
        foreach (var description in descriptions.Skip(1)) lines.Add($"   {description}");
    }

    private static void AddTender(List<string> lines, CanonicalSalesInvoiceTender tender, string currency)
    {
        var providers = Wrap(tender.Provider ?? string.Empty, 18).ToArray();
        var amount = Money(tender.AmountMinorUnits, currency);
        if (tender.Label.Length <= 10 && amount.Length <= 16)
        {
            lines.Add($"{tender.Label,-10} {providers[0],-18} {amount,16}");
            foreach (var provider in providers.Skip(1)) lines.Add($"           {provider}");
            return;
        }

        AddValue(lines, "Type", tender.Label);
        AddValue(lines, "Provider", tender.Provider);
        AddValue(lines, "Amount", amount);
    }

    private static void AddValue(List<string> lines, string label, string? value)
    {
        var resolved = value ?? string.Empty;
        if (label.Length + resolved.Length + 1 <= ReceiptWidth)
        {
            lines.Add(label + new string(' ', ReceiptWidth - label.Length - resolved.Length) + resolved);
            return;
        }
        lines.Add(label);
        foreach (var line in Wrap(resolved, ReceiptWidth)) lines.Add(line);
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

    private static bool HasCustomer(CanonicalSalesInvoiceCustomer customer) =>
        customer.ShowSignatureLine || new[] { customer.Name, customer.Address, customer.Tin, customer.BusinessStyle,
            customer.StatutoryIdLabel, customer.StatutoryIdNumber }.Any(value => !string.IsNullOrWhiteSpace(value));

    private static string PhtTimestamp(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss 'PHT'", CultureInfo.InvariantCulture);

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
    public CanonicalSalesInvoiceTextOutput RenderVisibleText(CanonicalSalesInvoiceText invoice) =>
        canonicalRenderer.Render(invoice);

    public CanonicalSalesInvoiceTextOutput RenderReprintVisibleText(
        CanonicalSalesInvoiceText invoice,
        FiscalDocumentReprintRecord reprint)
    {
        ArgumentNullException.ThrowIfNull(reprint);
        if (reprint.FiscalDocumentId != invoice.FiscalDocumentId ||
            !string.Equals(reprint.FiscalDocumentNumber, invoice.FiscalDocumentNumber, StringComparison.Ordinal) ||
            !string.Equals(reprint.ReprintStatus, "committed", StringComparison.Ordinal) ||
            !reprint.ReprintLabelApplied)
            throw new InvalidOperationException("A committed matching governed reprint record is required.");
        return canonicalRenderer.Render(invoice with { CopyLabel = "REPRINT" });
    }
}

public sealed record CanonicalSalesInvoiceCustomer(
    string? Name,
    string? Address,
    string? Tin,
    string? BusinessStyle,
    string? StatutoryIdLabel,
    string? StatutoryIdNumber,
    bool ShowSignatureLine);

public sealed record CanonicalSalesInvoiceLine(
    int Sequence,
    string Description,
    decimal Quantity,
    long UnitAmountMinorUnits,
    long AmountMinorUnits,
    string CurrencyCode);

public sealed record CanonicalSalesInvoiceDiscount(string Label, long AmountMinorUnits);

public sealed record CanonicalSalesInvoiceTender(string Label, string? Provider, long AmountMinorUnits);

public sealed record CanonicalSalesInvoicePresentation(
    string? BranchOrSite,
    string? ParkingLocation,
    string? TerminalIdentity,
    string? ParkingReference,
    string? TicketNumber,
    string? PlateNumber,
    string? EntryTimeText,
    string? PaymentTimeText,
    string? DurationText,
    long SubtotalAmountMinorUnits,
    IReadOnlyList<CanonicalSalesInvoiceDiscount> Discounts,
    IReadOnlyList<CanonicalSalesInvoiceTender> Tenders,
    long? TotalPaidMinorUnits,
    long? TenderedAmountMinorUnits,
    long? ChangeAmountMinorUnits,
    string? DeclarationText,
    DateTimeOffset PresentationTimestamp);

public sealed record CanonicalSalesInvoiceSupplier(
    string RegisteredName,
    string Address,
    string Tin,
    string AccreditationNumber,
    DateOnly AccreditationIssuedDate,
    string PtuNumber,
    DateOnly PtuIssuedDate);

public sealed record CanonicalSalesInvoiceText(
    Guid FiscalDocumentId,
    string FiscalDocumentNumber,
    DateTimeOffset IssuedAt,
    DateOnly BusinessDayDate,
    string RegisteredBusinessName,
    string RegisteredBusinessAddress,
    string SupplierTin,
    string PosSerialNumber,
    string MachineIdentificationNumber,
    string PtuNumber,
    string BirAccreditationNumber,
    CanonicalSalesInvoiceCustomer Customer,
    IReadOnlyList<CanonicalSalesInvoiceLine> Lines,
    long VatableSalesMinorUnits,
    long VatAmountMinorUnits,
    long VatExemptSalesMinorUnits,
    long ZeroRatedSalesMinorUnits,
    long TotalAmountMinorUnits,
    string CurrencyCode,
    string? FooterText,
    CanonicalSalesInvoicePresentation? Presentation = null,
    CanonicalSalesInvoiceSupplier? Supplier = null,
    string CopyLabel = "ORIGINAL");

public sealed record ElectronicJournalInvoiceTextExport(
    string ContentType,
    string FileName,
    string Text,
    byte[] Bytes);

public sealed class ElectronicJournalInvoiceTextRenderer(CanonicalSalesInvoiceTextRenderer canonicalRenderer)
{
    public ElectronicJournalInvoiceTextExport Render(IEnumerable<CanonicalSalesInvoiceText> invoices)
    {
        var ordered = invoices
            .GroupBy(invoice => invoice.FiscalDocumentId)
            .Select(group => group.OrderBy(invoice => invoice.IssuedAt).First())
            .OrderBy(invoice => invoice.IssuedAt)
            .ThenBy(invoice => invoice.FiscalDocumentNumber, StringComparer.Ordinal)
            .ToArray();
        if (ordered.Length == 0) throw new ArgumentException("At least one Sales Invoice is required.", nameof(invoices));

        var outputs = ordered.Select(canonicalRenderer.Render).ToArray();
        var text = string.Join("\r\n", outputs.Select(output => output.Text));
        var fileDate = ordered[0].BusinessDayDate == ordered[^1].BusinessDayDate
            ? ordered[0].BusinessDayDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
            : $"{ordered[0].BusinessDayDate:yyyyMMdd}-{ordered[^1].BusinessDayDate:yyyyMMdd}";
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
