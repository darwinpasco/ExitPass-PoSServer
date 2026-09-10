using System.Text.Json;
using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Runtime.ElectronicJournal;

public sealed class CanonicalSalesInvoiceTextFactory
{
    public CanonicalSalesInvoiceText Create(FiscalDocumentDraft draft)
    {
        var assignedAt = draft.FiscalNumberAssignedAt ?? DateTimeOffset.MinValue;
        return Create(new DigitalSalesInvoiceRenderModel(
            draft.FiscalDocumentId, draft.SitePosServerId, draft.ChannelTerminalId, draft.ResolvedFiscalIdentityId,
            draft.FiscalDocumentTypeCodeId, draft.FiscalDocumentTypeCodeKey, draft.FiscalDocumentStatusCodeId, "issued",
            "assigned", draft.ResolvedFiscalSequencePolicyId, draft.FiscalSequenceValue, draft.FiscalDocumentNumber,
            draft.FiscalSeries, draft.FiscalNumberPrefixText, draft.FiscalNumberSuffixText, draft.FiscalNumberAssignedAt,
            draft.FiscalNumberAssignedByRef, draft.BusinessDayDate, draft.CentralPmsParkingSessionRef,
            draft.CentralPmsPaymentAttemptRef, draft.CentralPmsPaymentConfirmationRef, draft.PaymentFinalityRef,
            draft.VendorAckRef, null, null, null, "not_voided", null, null, assignedAt, assignedAt,
            draft.DocumentLines.Select(line => new DigitalSalesInvoiceLineRenderModel(line.LineSequence, line.LineTypeCodeId,
                line.LineStatusCodeId, line.Description, line.Quantity, line.UnitAmountMinorUnits, line.GrossAmountMinorUnits,
                line.DiscountAmountMinorUnits, line.TaxAmountMinorUnits, line.NetAmountMinorUnits, line.CurrencyCode, line.SourceRef)).ToArray(),
            draft.DiscountPrivilegeDetails.Select(value => new DigitalSalesInvoiceDiscountRenderModel(null, value.DiscountPrivilegeTypeCodeId,
                value.BasisAmountMinorUnits, value.DiscountAmountMinorUnits, value.VatPrivilegeAmountMinorUnits, value.CurrencyCode,
                value.BeneficiaryRef, value.EvidenceRef, value.ApprovalRef)).ToArray(),
            draft.TaxDetails.Select(value => new DigitalSalesInvoiceTaxDetailRenderModel(null, value.TaxTypeCodeId,
                value.TaxClassificationCodeId, value.TaxRate, value.TaxableAmountMinorUnits, value.TaxAmountMinorUnits, value.CurrencyCode)).ToArray(),
            draft.Tenders.Select(value => new DigitalSalesInvoiceTenderRenderModel(value.TenderTypeCodeId, null, value.AmountMinorUnits,
                value.CurrencyCode, value.CentralPmsPaymentAttemptRef, value.CentralPmsPaymentConfirmationRef, value.PaymentFinalityRef, value.ProviderRef)).ToArray(),
            draft.Totals.Select(value => new DigitalSalesInvoiceTotalRenderModel(value.TotalTypeCodeId, value.AmountMinorUnits, value.CurrencyCode)).ToArray(),
            new DigitalSalesInvoiceFooterRenderModel("canonical", []), draft.SalesInvoiceHeaderSnapshot,
            draft.AppliedStatutoryFiscalFacts, draft.CompletionBasis, draft.CompletionAuthorityRef,
            draft.InvoiceCustomerInformation, JsonSerializer.Serialize(new { reference_context = draft.ReferenceContext })));
    }

    public CanonicalSalesInvoiceText Create(DigitalSalesInvoiceRenderModel source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var header = source.SalesInvoiceHeaderSnapshot ??
            throw new InvalidOperationException("The authoritative Sales Invoice header snapshot is unavailable.");
        if (source.FiscalNumberAssignmentState != "assigned" || string.IsNullOrWhiteSpace(source.FiscalDocumentNumber) ||
            source.FiscalNumberAssignedAt is null || source.BusinessDayDate is null)
        {
            throw new InvalidOperationException("Only complete, issued Sales Invoices can be rendered for the Electronic Journal.");
        }

        var currencies = source.Lines.Select(line => line.CurrencyCode)
            .Concat(source.TaxDetails.Select(tax => tax.CurrencyCode))
            .Concat(source.Tenders.Select(tender => tender.CurrencyCode))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var currency = source.AppliedStatutoryFiscalFacts?.Currency?.Trim().ToUpperInvariant()
            ?? currencies.SingleOrDefault()
            ?? throw new InvalidOperationException("The Sales Invoice currency is unavailable.");
        if (currencies.Any(value => !string.Equals(value, currency, StringComparison.Ordinal)))
            throw new InvalidOperationException("Mixed-currency Sales Invoice text is prohibited.");

        var statutory = source.AppliedStatutoryFiscalFacts;
        var vatable = statutory is null
            ? source.TaxDetails.Where(tax => tax.TaxRate > 0).Sum(tax => tax.TaxableAmountMinorUnits)
            : 0;
        var vatAmount = statutory is null
            ? source.TaxDetails.Where(tax => tax.TaxRate > 0).Sum(tax => tax.TaxAmountMinorUnits)
            : statutory.VatAmountMinorUnits;
        var exempt = statutory is null ? 0 : statutory.VatExclusiveBasisAmountMinorUnits;
        var zeroRated = statutory is null
            ? source.TaxDetails.Where(tax => tax.TaxRate == 0).Sum(tax => tax.TaxableAmountMinorUnits)
            : 0;
        var total = statutory?.FinalPayableAmountMinorUnits ?? source.Lines.Sum(line => line.NetAmountMinorUnits);
        var context = ReadContext(source.DocumentContextJson);
        var customer = ReadCustomer(context, statutory, source.Discounts, source.InvoiceCustomerInformation);
        var footer = string.Join("\r\n", new[]
        {
            header.CustomerServiceFooter,
            $"Software Supplier  : {header.SupplierDeveloperRegisteredName}",
            $"Supplier Address   : {header.SupplierDeveloperAddress}",
            $"Supplier TIN       : {header.SupplierDeveloperTin}",
            $"BIR Accreditation  : {header.BirAccreditationNumber}",
            $"PTU No.            : {header.PtuNumber}"
        }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var presentation = new CanonicalSalesInvoicePresentation(
            Value(context, "parking_location", "parkingLocation") ?? header.ParkingLocationDisplay,
            Value(context, "terminal_id", "terminalId", "runtime_terminal_ref") ?? header.TerminalId ?? source.ChannelTerminalId?.ToString("D"),
            source.CentralPmsParkingSessionRef,
            Value(context, "ticket_number", "ticketNumber", "ticket_ref", "ticketRef"),
            Value(context, "plate_number", "plateNumber", "license_plate", "licensePlate"),
            Value(context, "entry_time", "entryTime", "entry_at", "entryAt"),
            Value(context, "payment_time", "paymentTime", "paid_at", "paidAt"),
            Value(context, "duration", "durationText", "parking_duration"),
            source.Lines.Sum(line => line.GrossAmountMinorUnits),
            source.Discounts.Select((discount, index) => new CanonicalSalesInvoiceDiscount(
                statutory?.EntitlementType is { Length: > 0 } entitlement ? $"{entitlement} Discount" : $"Discount {index + 1}",
                discount.DiscountAmountMinorUnits)).ToArray(),
            source.Tenders.Select(tender => new CanonicalSalesInvoiceTender(
                tender.TenderTypeCodeKey ?? tender.ProviderRef ?? "Tender",
                tender.AmountMinorUnits)).ToArray(),
            source.Tenders.Count == 0 ? total : source.Tenders.Sum(tender => tender.AmountMinorUnits),
            LongValue(context, "tendered_amount_minor_units", "tenderedAmountMinorUnits"),
            LongValue(context, "change_amount_minor_units", "changeAmountMinorUnits"),
            header.SalesInvoiceLegalStatement,
            source.FiscalNumberAssignedAt.Value);

        return new(
            source.FiscalDocumentId,
            source.FiscalDocumentNumber,
            source.FiscalNumberAssignedAt.Value,
            source.BusinessDayDate.Value,
            header.RegisteredBusinessName,
            header.RegisteredBusinessAddress,
            header.Tin,
            header.PosSerialNumber,
            header.MachineIdentificationNumber,
            header.PtuNumber,
            header.BirAccreditationNumber,
            customer,
            source.Lines.OrderBy(line => line.LineSequence)
                .Select(line => new CanonicalSalesInvoiceLine(line.LineSequence, line.Description, line.Quantity, line.NetAmountMinorUnits, line.CurrencyCode))
                .ToArray(),
            vatable,
            vatAmount,
            exempt,
            zeroRated,
            total,
            currency,
            footer,
            presentation);
    }

    private static CanonicalSalesInvoiceCustomer ReadCustomer(
        IReadOnlyDictionary<string, string?> values,
        AppliedStatutoryFiscalFactsSnapshot? statutory,
        IReadOnlyList<DigitalSalesInvoiceDiscountRenderModel> discounts,
        InvoiceCustomerInformationSnapshot? snapshot)
    {
        var entitlement = statutory?.EntitlementType?.Trim().ToUpperInvariant();
        var statutoryLabel = entitlement switch
        {
            "PWD" or "PERSON_WITH_DISABILITY" => "PWD ID No.",
            "SENIOR_CITIZEN" or "SENIOR" => "OSCA ID No.",
            _ => null
        };
        var statutoryReference = snapshot?.StatutoryIdNumber ??
            discounts.Select(value => value.BeneficiaryRef).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        return new(
            snapshot?.CustomerName ?? Value(values, "customer_name", "customerName"),
            snapshot?.Address ?? Value(values, "customer_address", "customerAddress"),
            snapshot?.Tin ?? Value(values, "customer_tin", "customerTin"),
            snapshot?.BusinessStyle ?? Value(values, "business_style", "businessStyle"),
            statutoryLabel,
            statutoryReference,
            true);
    }

    private static IReadOnlyDictionary<string, string?> ReadContext(string? json)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in document.RootElement.EnumerateObject())
                        if (property.Value.ValueKind == JsonValueKind.String) values[property.Name] = property.Value.GetString();
                    if (document.RootElement.TryGetProperty("reference_context", out var referenceContext) && referenceContext.ValueKind == JsonValueKind.Object)
                        foreach (var property in referenceContext.EnumerateObject())
                            if (property.Value.ValueKind == JsonValueKind.String) values[property.Name] = property.Value.GetString();
                }
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("The persisted Sales Invoice customer context is invalid.");
            }
        }
        return values;
    }

    private static string? Value(IReadOnlyDictionary<string, string?> values, params string[] names) =>
        names.Select(name => values.TryGetValue(name, out var value) ? value : null)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private static long? LongValue(IReadOnlyDictionary<string, string?> values, params string[] names) =>
        long.TryParse(Value(values, names), out var value) ? value : null;
}
