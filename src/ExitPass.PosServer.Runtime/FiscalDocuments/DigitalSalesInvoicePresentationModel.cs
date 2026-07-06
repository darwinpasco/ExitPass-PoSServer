namespace ExitPass.PosServer.Runtime.FiscalDocuments;

public sealed record DigitalSalesInvoicePresentationModel(
    string PresentationVersion,
    string SourceTemplateContractVersion,
    string FiscalTemplateFamily,
    string RenderFormat,
    string NumberingState,
    string DocumentTitle,
    IReadOnlyList<DigitalSalesInvoicePresentationSectionModel> Sections,
    IReadOnlyList<DigitalSalesInvoicePresentationNoticeModel> Notices);

public sealed record DigitalSalesInvoicePresentationSectionModel(
    string Name,
    string Label,
    int SortOrder,
    string Posture,
    IReadOnlyList<DigitalSalesInvoicePresentationRowModel> Rows);

public sealed record DigitalSalesInvoicePresentationRowModel(
    string Key,
    string Label,
    string ValueKind,
    string Posture,
    string? DisplayValue = null,
    object? RawValue = null);

public sealed record DigitalSalesInvoicePresentationNoticeModel(
    string Code,
    string Severity,
    string Message);
