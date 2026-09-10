using System.Globalization;
using System.Text;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

/// <summary>
/// Canonical visible Sales Invoice text. Physical printer adapters and Electronic Journal
/// export must consume these exact UTF-8 bytes; printer-only control bytes are applied later.
/// </summary>
public sealed class CanonicalSalesInvoiceTextRenderer
{
    private const string Separator = "------------------------------------------------";

    public CanonicalSalesInvoiceTextOutput Render(CanonicalSalesInvoiceText invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        var currency = RequireCurrency(invoice.CurrencyCode);
        var lines = new List<string>
        {
            Center("SALES INVOICE"),
            Center(invoice.RegisteredBusinessName),
            Center(invoice.RegisteredBusinessAddress),
            $"TIN                : {invoice.SupplierTin}",
            $"POS Serial No.     : {invoice.PosSerialNumber}",
            $"MIN                : {invoice.MachineIdentificationNumber}",
            $"PTU No.            : {invoice.PtuNumber}",
            $"BIR Accreditation  : {invoice.BirAccreditationNumber}",
            $"Parking Location   : {invoice.Presentation?.ParkingLocation ?? string.Empty}",
            $"Terminal           : {invoice.Presentation?.TerminalIdentity ?? string.Empty}",
            Separator,
            $"SI No.             : {invoice.FiscalDocumentNumber}",
            $"Business Date      : {invoice.BusinessDayDate:yyyy-MM-dd}",
            $"Issued At          : {invoice.IssuedAt.ToUniversalTime():yyyy-MM-dd HH:mm:ss} UTC",
            Separator
        };

        AppendParkingDetails(lines, invoice.Presentation);

        lines.Add("Customer Information");
        lines.Add($"Customer Name      : {invoice.Customer.Name ?? string.Empty}");
        lines.Add($"Address            : {invoice.Customer.Address ?? string.Empty}");
        lines.Add($"TIN                : {invoice.Customer.Tin ?? string.Empty}");
        lines.Add($"Business Style     : {invoice.Customer.BusinessStyle ?? string.Empty}");

        if (!string.IsNullOrWhiteSpace(invoice.Customer.StatutoryIdLabel) ||
            !string.IsNullOrWhiteSpace(invoice.Customer.StatutoryIdNumber))
        {
            lines.Add($"{(invoice.Customer.StatutoryIdLabel ?? "OSCA ID No. / PWD ID No.").PadRight(19)}: {invoice.Customer.StatutoryIdNumber ?? string.Empty}");
        }

        if (invoice.Customer.ShowSignatureLine)
        {
            lines.Add("Customer Signature : ____________________");
        }

        lines.Add(Separator);
        lines.Add("Qty  Description                         Amount");
        foreach (var item in invoice.Lines.OrderBy(item => item.Sequence))
        {
            lines.Add($"{item.Quantity,3:0.##}  {Truncate(item.Description, 30),-30} {Money(item.AmountMinorUnits, currency),13}");
        }

        lines.Add(Separator);
        lines.Add($"Subtotal           : {Money(invoice.Presentation?.SubtotalAmountMinorUnits ?? invoice.TotalAmountMinorUnits, currency)}");
        foreach (var discount in invoice.Presentation?.Discounts ?? [])
            lines.Add($"{Truncate(discount.Label, 18).PadRight(19)}: -{Money(discount.AmountMinorUnits, currency)}");
        lines.Add($"VATable Sales      : {Money(invoice.VatableSalesMinorUnits, currency)}");
        lines.Add($"VAT Amount         : {Money(invoice.VatAmountMinorUnits, currency)}");
        lines.Add($"VAT Exempt Sales   : {Money(invoice.VatExemptSalesMinorUnits, currency)}");
        lines.Add($"Zero Rated Sales   : {Money(invoice.ZeroRatedSalesMinorUnits, currency)}");
        lines.Add($"TOTAL              : {Money(invoice.TotalAmountMinorUnits, currency)}");
        lines.Add(Separator);
        AppendPaymentDetails(lines, invoice.Presentation, currency, invoice.TotalAmountMinorUnits);
        if (!string.IsNullOrWhiteSpace(invoice.Presentation?.DeclarationText))
            lines.Add(invoice.Presentation.DeclarationText.Trim());
        lines.Add($"Print / Issued At  : {(invoice.Presentation?.PresentationTimestamp ?? invoice.IssuedAt).ToUniversalTime():yyyy-MM-dd HH:mm:ss} UTC");
        lines.Add(Separator);
        if (!string.IsNullOrWhiteSpace(invoice.FooterText)) lines.Add(invoice.FooterText.Trim());

        var text = string.Join("\r\n", lines) + "\r\n";
        return new(text, Encoding.UTF8.GetBytes(text));
    }

    private static string Money(long minorUnits, string currency) =>
        $"{currency} {(minorUnits / 100m).ToString("N2", CultureInfo.InvariantCulture)}";

    private static string Center(string value) =>
        value.Length >= 48 ? value : value.PadLeft(value.Length + ((48 - value.Length) / 2));

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    private static void AppendParkingDetails(List<string> lines, CanonicalSalesInvoicePresentation? presentation)
    {
        if (presentation is null) return;
        var details = new (string Label, string? Value)[]
        {
            ("Parking Ref", presentation.ParkingReference),
            ("Ticket No.", presentation.TicketNumber),
            ("Plate No.", presentation.PlateNumber),
            ("Entry Time", presentation.EntryTimeText),
            ("Payment Time", presentation.PaymentTimeText),
            ("Duration", presentation.DurationText)
        };
        if (!details.Any(value => !string.IsNullOrWhiteSpace(value.Value))) return;
        lines.Add("Parking Details");
        foreach (var detail in details.Where(value => !string.IsNullOrWhiteSpace(value.Value)))
            lines.Add($"{detail.Label.PadRight(19)}: {detail.Value}");
        lines.Add(Separator);
    }

    private static void AppendPaymentDetails(List<string> lines, CanonicalSalesInvoicePresentation? presentation, string currency, long total)
    {
        if (presentation is null) return;
        lines.Add("Payment Details");
        foreach (var tender in presentation.Tenders)
            lines.Add($"{Truncate(tender.Label, 18).PadRight(19)}: {Money(tender.AmountMinorUnits, currency)}");
        lines.Add($"Total Paid         : {Money(presentation.TotalPaidMinorUnits ?? total, currency)}");
        if (presentation.TenderedAmountMinorUnits.HasValue)
            lines.Add($"Tendered           : {Money(presentation.TenderedAmountMinorUnits.Value, currency)}");
        if (presentation.ChangeAmountMinorUnits.HasValue)
            lines.Add($"Change             : {Money(presentation.ChangeAmountMinorUnits.Value, currency)}");
        lines.Add(Separator);
    }

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
    long AmountMinorUnits,
    string CurrencyCode);

public sealed record CanonicalSalesInvoiceDiscount(string Label, long AmountMinorUnits);

public sealed record CanonicalSalesInvoiceTender(string Label, long AmountMinorUnits);

public sealed record CanonicalSalesInvoicePresentation(
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
    CanonicalSalesInvoicePresentation? Presentation = null);

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
