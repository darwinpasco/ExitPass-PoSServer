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
    private const string Separator = "------------------------------------------------";
    private const int ReceiptWidth = 48;

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
        lines.Add(string.Empty);
        AddValue(lines, "VAT REG TIN", header.Tin);
        AddValue(lines, "MIN", header.MachineIdentificationNumber);
        AddValue(lines, "S/N", header.PosSerialNumber);
        AddValue(lines, "Branch / Site", fiscal.BranchOrSite);
        AddValue(lines, "Parking Location", header.ParkingLocationDisplay);

        AddSection(lines, RequireDocumentDesignation(invoice.DocumentDesignation));
        AddCentered(lines, RequireCopyLabel(copyDesignation));
        lines.Add(string.Empty);
        AddValue(lines, "SI No", invoice.FiscalDocumentNumber);
        AddValue(lines, "Issued Date", PhtTimestamp(invoice.FiscalNumberAssignedAt.Value));
        AddValue(lines, "Business Date", invoice.BusinessDayDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

        AddSection(lines, "PARKING DETAILS");
        AddValue(lines, "Ticket Number", fiscal.TicketNumber);
        AddValue(lines, "Plate Number", fiscal.PlateNumber);
        AddValue(lines, "Entry Time", fiscal.EntryTimeText);
        lines.Add(string.Empty);
        AddValue(lines, "Payment", fiscal.PaymentTimeText);
        AddValue(lines, "Duration", fiscal.ParkingDurationText);

        AddSection(lines, "ITEMS");
        lines.Add("# Description      Qty        Unit       Amount");
        foreach (var item in invoice.Lines.OrderBy(item => item.LineSequence))
            AddItem(lines, item, currency);
        lines.Add(string.Empty);
        AddValue(lines, "Subtotal", Money(fiscal.SubtotalAmountMinorUnits, currency));

        AddSection(lines, "DISCOUNTS");
        if (invoice.Discounts.Count == 0)
        {
            AddValue(lines, "Discount Reason", "NONE");
            AddValue(lines, "Discount Amount", Money(0, currency));
        }
        else
        {
            foreach (var discount in invoice.Discounts)
            {
                AddValue(lines, "Discount Reason", discount.Reason ?? "NOT RECORDED");
                AddValue(lines, "Discount Amount", $"-{Money(discount.DiscountAmountMinorUnits, currency)}");
            }
        }

        AddSection(lines, "VAT BREAKDOWN");
        AddValue(lines, "VATable Sales", Money(fiscal.VatableSalesMinorUnits, currency));
        AddValue(lines, "VAT Amount", Money(fiscal.VatAmountMinorUnits, currency));
        AddValue(lines, "VAT Exempt Sales", Money(fiscal.VatExemptSalesMinorUnits, currency));
        AddValue(lines, "Zero Rated Sales", Money(fiscal.ZeroRatedSalesMinorUnits, currency));
        AddValue(lines, "Total Amount", Money(fiscal.TotalAmountMinorUnits, currency));

        AddSection(lines, "PAYMENT DETAILS");
        lines.Add("Type       Provider                      Amount");
        foreach (var tender in invoice.Tenders) AddTender(lines, tender, fiscal.PaymentMethod, currency);
        AddValue(lines, "Total Paid", Money(fiscal.TotalPaidMinorUnits, currency));
        if (fiscal.TenderedAmountMinorUnits is { } tendered) AddValue(lines, "Tendered", Money(tendered, currency));
        if (fiscal.ChangeAmountMinorUnits is { } change) AddValue(lines, "Change", Money(change, currency));
        AddValue(lines, "Completion Basis", invoice.CompletionBasis);
        AddValue(lines, "Completion Authority", invoice.CompletionAuthorityRef);

        lines.Add(Separator);
        AddCentered(lines, header.SalesInvoiceLegalStatement);

        var customer = invoice.InvoiceCustomerInformation;
        if (HasCustomer(customer, fiscal.ShowCustomerSignatureLine))
        {
            AddSection(lines, "Customer Information");
            AddValue(lines, "NAME", customer?.CustomerName);
            AddValue(lines, "ADDRESS", customer?.Address);
            AddValue(lines, "TIN", customer?.Tin);
            AddValue(lines, "BUS. STYLE", customer?.BusinessStyle);
            var statutoryId = invoice.AppliedStatutoryFiscalFacts is null ? null : customer?.StatutoryIdNumber;
            if (!string.IsNullOrWhiteSpace(statutoryId))
                AddValue(lines, StatutoryIdLabel(invoice.AppliedStatutoryFiscalFacts?.EntitlementType), statutoryId);
            if (fiscal.ShowCustomerSignatureLine) lines.Add("Customer Sign : __________________________");
        }

        AddSection(lines, "POS SOFTWARE SUPPLIER / DEVELOPER");
        AddCentered(lines, header.SupplierDeveloperRegisteredName);
        AddCentered(lines, header.SupplierDeveloperAddress);
        lines.Add(string.Empty);
        AddValue(lines, "TIN", header.SupplierDeveloperTin);
        AddValue(lines, "ACCR. NO.", header.BirAccreditationNumber);
        AddValue(lines, "DATE ISSUED", header.BirAccreditationIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        AddValue(lines, "PTU", header.PtuNumber);
        AddValue(lines, "PTU Date", header.PtuIssuedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(header.CustomerServiceFooter)) AddCentered(lines, header.CustomerServiceFooter.Trim());
        foreach (var closingLine in invoice.Footer.ClosingTextLines ?? [])
        {
            if (closingLine.Contains("NOTHING FOLLOWS", StringComparison.Ordinal)) lines.Add(string.Empty);
            AddCentered(lines, closingLine);
        }

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

    private static void AddSection(List<string> lines, string title)
    {
        lines.Add(Separator);
        AddCentered(lines, title);
        lines.Add(Separator);
    }

    private static void AddItem(List<string> lines, DigitalSalesInvoiceLineRenderModel item, string currency)
    {
        var descriptions = Wrap(item.Description, 14).ToArray();
        var row = $"{item.LineSequence,2} {descriptions[0],-14} {item.Quantity,4:0.##} {Money(item.UnitAmountMinorUnits, currency),11} {Money(item.NetAmountMinorUnits, currency),12}";
        if (row.Length <= ReceiptWidth)
        {
            lines.Add(row);
        }
        else
        {
            AddValue(lines, $"{item.LineSequence} {descriptions[0]}", Money(item.NetAmountMinorUnits, currency));
            AddValue(lines, "Qty / Unit", $"{item.Quantity:0.##} / {Money(item.UnitAmountMinorUnits, currency)}");
        }
        foreach (var description in descriptions.Skip(1)) lines.Add($"   {description}");
    }

    private static void AddTender(
        List<string> lines,
        DigitalSalesInvoiceTenderRenderModel tender,
        string? paymentMethod,
        string currency)
    {
        var label = (tender.TenderTypeCodeKey ?? paymentMethod ?? "NOT RECORDED").ToUpperInvariant();
        var providers = Wrap(tender.ProviderRef ?? string.Empty, 18).ToArray();
        var amount = Money(tender.AmountMinorUnits, currency);
        if (label.Length <= 10 && amount.Length <= 16)
        {
            lines.Add($"{label,-10} {providers[0],-18} {amount,16}");
            foreach (var provider in providers.Skip(1)) lines.Add($"           {provider}");
            return;
        }

        AddValue(lines, "Type", label);
        AddValue(lines, "Provider", tender.ProviderRef);
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

    private static bool HasCustomer(InvoiceCustomerInformationSnapshot? customer, bool showSignatureLine) =>
        showSignatureLine || customer is not null && new[]
        {
            customer.CustomerName, customer.Address, customer.Tin, customer.BusinessStyle, customer.StatutoryIdNumber
        }.Any(value => !string.IsNullOrWhiteSpace(value));

    private static string PhtTimestamp(DateTimeOffset value) =>
        value.ToOffset(TimeSpan.FromHours(8)).ToString("yyyy-MM-dd HH:mm:ss 'PHT'", CultureInfo.InvariantCulture);

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
