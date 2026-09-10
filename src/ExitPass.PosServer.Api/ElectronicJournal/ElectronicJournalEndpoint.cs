using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.ElectronicJournal;

namespace ExitPass.PosServer.Api.ElectronicJournal;

public sealed class ElectronicJournalFilterRequest
{
    public Guid SitePosServerId { get; init; }
    public Guid FiscalIdentityId { get; init; }
    public string? CurrencyCode { get; init; }
    public Guid? FiscalReportingPeriodId { get; init; }
    public string? FiscalDocumentReference { get; init; }
    public string? FiscalDocumentNumber { get; init; }
    public string? ZReadingReference { get; init; }
    public string? EventType { get; init; }
    public DateTimeOffset? EffectiveFrom { get; init; }
    public DateTimeOffset? EffectiveTo { get; init; }
    public DateTimeOffset? RecordedFrom { get; init; }
    public DateTimeOffset? RecordedTo { get; init; }
    public string? CorrelationReference { get; init; }
    public int? PageSize { get; init; }
    public string? Cursor { get; init; }
    public long? ThroughSequence { get; init; }
}

public sealed record ElectronicJournalErrorResponse(bool Succeeded, string Code, string CorrelationId, string SupportReference, string Message);
public sealed record ElectronicJournalReadResponse(bool Succeeded, string Code, string CorrelationId, string SupportReference, ElectronicJournalPage Page);
public sealed record ElectronicJournalInvoiceTextResponse(bool Succeeded, string Code, string CorrelationId, string SupportReference, IReadOnlyList<ElectronicJournalInvoiceTextItem> Invoices);
public sealed record VerifyElectronicJournalIntegrityRequest(Guid SitePosServerId, Guid FiscalIdentityId, string? CurrencyCode, long? ThroughSequence = null);

public static class ElectronicJournalEndpoint
{
    public static async Task<IResult> ReadInvoiceTextAsync(
        ElectronicJournalFilterRequest request,
        ElectronicJournalInvoiceTextService invoiceTextService,
        ElectronicJournalService accessService,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(request, context, environment, ElectronicJournalContract.MaximumPageSize);
        if (preflight.Error is not null) return preflight.Error;
        var query = ToQuery(request);
        var result = await invoiceTextService.ReadAsync(query, false, cancellationToken).ConfigureAwait(false);
        var support = Support();
        if (result.Invoices is null) return MapFailure(context, result.Outcome, result.SafeMessage, preflight.Correlation!, support);
        try
        {
            await accessService.RecordAccessAsync(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode, "read", "allowed",
                preflight.Actor!, preflight.Actor!, preflight.Correlation!, support, result.Invoices.Count, cancellationToken).ConfigureAwait(false);
        }
        catch { return Unavailable(context, preflight.Correlation!, support); }
        NoStore(context);
        return Results.Json(new ElectronicJournalInvoiceTextResponse(true, "electronic_journal_invoice_text_read", preflight.Correlation!, support, result.Invoices));
    }

    public static async Task<IResult> ExportInvoiceTextAsync(
        ElectronicJournalFilterRequest request,
        ElectronicJournalInvoiceTextService invoiceTextService,
        ElectronicJournalService accessService,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(request, context, environment, ElectronicJournalContract.MaximumPageSize);
        if (preflight.Error is not null) return preflight.Error;
        var query = ToQuery(request);
        var result = await invoiceTextService.ReadAsync(query, true, cancellationToken).ConfigureAwait(false);
        var support = Support();
        if (result.Invoices is null) return MapFailure(context, result.Outcome, result.SafeMessage, preflight.Correlation!, support);
        if (result.Export is null)
            return Error(context, "electronic_journal_no_invoice_activity", "No issued Sales Invoice text exists for the requested period.", preflight.Correlation!, support, 404);
        try
        {
            await accessService.RecordAccessAsync(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode, "export", "allowed",
                preflight.Actor!, preflight.Actor!, preflight.Correlation!, support, result.Invoices.Count, cancellationToken).ConfigureAwait(false);
        }
        catch { return Unavailable(context, preflight.Correlation!, support); }
        NoStore(context);
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{result.Export.FileName}\"";
        context.Response.Headers["X-ExitPass-Output-Contract"] = "pos-server-electronic-journal-invoice-text:v1";
        return Results.Bytes(result.Export.Bytes, result.Export.ContentType);
    }

    public static async Task<IResult> ReadAsync(
        ElectronicJournalFilterRequest request,
        ElectronicJournalService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(request, context, environment, ElectronicJournalContract.MaximumPageSize);
        if (preflight.Error is not null) return preflight.Error;
        var query = ToQuery(request);
        var result = await service.ReadAsync(query, cancellationToken).ConfigureAwait(false);
        var support = Support();
        if (result.Page is null) return MapFailure(context, result.Outcome, result.SafeMessage, preflight.Correlation!, support);
        try
        {
            await service.RecordAccessAsync(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode, "read", "allowed",
                preflight.Actor!, preflight.Actor!, preflight.Correlation!, support, result.Page.Events.Count, cancellationToken).ConfigureAwait(false);
        }
        catch { return Unavailable(context, preflight.Correlation!, support); }
        NoStore(context);
        return Results.Json(new ElectronicJournalReadResponse(true, "electronic_journal_read", preflight.Correlation!, support, result.Page));
    }

    public static async Task<IResult> ExportAsync(
        string format,
        ElectronicJournalFilterRequest request,
        ElectronicJournalService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var preflight = Preflight(request, context, environment, ElectronicJournalContract.MaximumPageSize);
        if (preflight.Error is not null) return preflight.Error;
        if (!TryFormat(format, out var outputFormat))
            return Error(context, "electronic_journal_export_format_unsupported", "The Electronic Journal export format is unsupported.", preflight.Correlation!, Support(), 400);
        var query = ToQuery(request) with { PageSize = ElectronicJournalContract.MaximumExportEvents, Cursor = null };
        var (result, export) = await service.ExportAsync(query, outputFormat, cancellationToken).ConfigureAwait(false);
        var support = Support();
        if (export is null) return MapFailure(context, result.Outcome, result.SafeMessage, preflight.Correlation!, support);
        try
        {
            await service.RecordAccessAsync(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode, "export", "allowed",
                preflight.Actor!, preflight.Actor!, preflight.Correlation!, support, result.Page!.Events.Count, cancellationToken).ConfigureAwait(false);
        }
        catch { return Unavailable(context, preflight.Correlation!, support); }
        NoStore(context); context.Response.Headers.ETag = export.ETag;
        context.Response.Headers["X-Content-SHA256"] = export.ContentSha256;
        context.Response.Headers["X-ExitPass-Output-Identity"] = export.OutputIdentity;
        context.Response.Headers["X-ExitPass-Output-Contract"] = ElectronicJournalContract.ExportVersion;
        context.Response.Headers.ContentDisposition = $"attachment; filename=\"{export.FileName}\"";
        return Results.Bytes(export.Bytes, export.ContentType);
    }

    public static async Task<IResult> VerifyAsync(
        VerifyElectronicJournalIntegrityRequest request,
        ElectronicJournalService service,
        HttpContext context,
        IHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var filters = new ElectronicJournalFilterRequest
        {
            SitePosServerId = request.SitePosServerId,
            FiscalIdentityId = request.FiscalIdentityId,
            CurrencyCode = request.CurrencyCode,
            ThroughSequence = request.ThroughSequence
        };
        var preflight = Preflight(filters, context, environment, ElectronicJournalContract.MaximumPageSize);
        if (preflight.Error is not null) return preflight.Error;
        var query = ToQuery(filters);
        var outcome = await service.VerifyIntegrityAsync(query, cancellationToken).ConfigureAwait(false);
        var support = outcome.Result?.SupportReference ?? Support();
        try
        {
            await service.RecordAccessAsync(query.SitePosServerId, query.FiscalIdentityId, query.CurrencyCode, "integrity_verify",
                outcome.Outcome == ElectronicJournalOutcome.Success ? "allowed" : "failed", preflight.Actor!, preflight.Actor!,
                preflight.Correlation!, support, checked((int)Math.Min(outcome.Result?.VerifiedEventCount ?? 0, int.MaxValue)), cancellationToken).ConfigureAwait(false);
        }
        catch { return Unavailable(context, preflight.Correlation!, support); }
        if (outcome.Result is null) return MapFailure(context, outcome.Outcome, outcome.SafeMessage, preflight.Correlation!, support);
        NoStore(context);
        return Results.Json(new { succeeded = outcome.Result.IsValid, code = outcome.Result.IsValid ? "electronic_journal_integrity_valid" : "electronic_journal_integrity_failed", correlationId = preflight.Correlation, result = outcome.Result },
            statusCode: outcome.Result.IsValid ? 200 : 409);
    }

    private static (string? Correlation, string? Actor, IResult? Error) Preflight(
        ElectronicJournalFilterRequest request, HttpContext context, IHostEnvironment environment, int maxPage)
    {
        var correlation = Correlation(context);
        if (correlation is null) return (null, null, Error(context, "missing_correlation_id", "A single safe X-Correlation-Id header is required.", "unavailable", Support(), 400));
        if (!ElectronicJournalAuthorization.IsHostingAuthorityAllowed(context.User, environment))
            return (correlation, null, Hidden(context, correlation));
        var currency = request.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var pageSize = request.PageSize ?? ElectronicJournalContract.DefaultPageSize;
        if (request.SitePosServerId == Guid.Empty || request.FiscalIdentityId == Guid.Empty || currency.Length != 3 ||
            !ElectronicJournalAuthorization.IsInScope(context.User, request.SitePosServerId, request.FiscalIdentityId, currency))
            return (correlation, null, Hidden(context, correlation));
        if (pageSize < 1 || pageSize > maxPage)
            return (correlation, null, Error(context, "electronic_journal_page_size_invalid", "The Electronic Journal page size is outside the governed limit.", correlation, Support(), 400));
        if (new[] { request.FiscalDocumentReference, request.FiscalDocumentNumber, request.ZReadingReference, request.EventType, request.CorrelationReference }
            .Any(value => value?.Length > 200 || value?.Contains('\r') == true || value?.Contains('\n') == true))
            return (correlation, null, Error(context, "electronic_journal_filter_invalid", "An Electronic Journal filter is invalid.", correlation, Support(), 400));
        if (request.FiscalDocumentReference is not null &&
            (!Guid.TryParse(request.FiscalDocumentReference, out var documentId) || documentId == Guid.Empty))
            return (correlation, null, Error(context, "electronic_journal_filter_invalid", "An Electronic Journal filter is invalid.", correlation, Support(), 400));
        return (correlation, context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unavailable", null);
    }

    private static ElectronicJournalQuery ToQuery(ElectronicJournalFilterRequest r) => new(
        r.SitePosServerId, r.FiscalIdentityId, r.CurrencyCode!.Trim().ToUpperInvariant(), r.FiscalReportingPeriodId,
        r.FiscalDocumentReference, r.FiscalDocumentNumber, r.ZReadingReference, r.EventType,
        r.EffectiveFrom, r.EffectiveTo, r.RecordedFrom, r.RecordedTo, r.CorrelationReference,
        r.PageSize ?? ElectronicJournalContract.DefaultPageSize, r.Cursor, r.ThroughSequence);

    private static IResult MapFailure(HttpContext c, ElectronicJournalOutcome outcome, string? message, string correlation, string support) => outcome switch
    {
        ElectronicJournalOutcome.NotFound => Hidden(c, correlation),
        ElectronicJournalOutcome.MalformedCursor => Error(c, "electronic_journal_cursor_invalid", message, correlation, support, 400),
        ElectronicJournalOutcome.RangeTooLarge => Error(c, "electronic_journal_range_too_large", message, correlation, support, 413),
        ElectronicJournalOutcome.IntegrityFailure => Error(c, "electronic_journal_integrity_failed", message, correlation, support, 409),
        ElectronicJournalOutcome.PersistenceFailure => Unavailable(c, correlation, support),
        _ => Error(c, "electronic_journal_request_invalid", message, correlation, support, 400)
    };

    private static bool TryFormat(string value, out ElectronicJournalExportFormat format)
    { if (value.Equals("json", StringComparison.OrdinalIgnoreCase)) { format = ElectronicJournalExportFormat.Json; return true; } if (value.Equals("csv", StringComparison.OrdinalIgnoreCase)) { format = ElectronicJournalExportFormat.Csv; return true; } format = default; return false; }
    private static string? Correlation(HttpContext context) { var v=context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName]; if(v.Count!=1)return null;var s=v[0]?.Trim();return string.IsNullOrWhiteSpace(s)||s.Length>200||s.Contains('\r')||s.Contains('\n')?null:s; }
    private static IResult Hidden(HttpContext c, string correlation) => Error(c, "electronic_journal_not_found", "The requested Electronic Journal resource is unavailable.", correlation, Support(), 404);
    private static IResult Unavailable(HttpContext c, string correlation, string support) => Error(c, "electronic_journal_unavailable", "The Electronic Journal is temporarily unavailable.", correlation, support, 503);
    private static IResult Error(HttpContext c, string code, string? message, string correlation, string support, int status) { NoStore(c); return Results.Json(new ElectronicJournalErrorResponse(false,code,correlation,support,message??"The Electronic Journal operation failed safely."),statusCode:status); }
    private static void NoStore(HttpContext c) { c.Response.Headers.CacheControl="private, no-store";c.Response.Headers.Pragma="no-cache";c.Response.Headers["X-Content-Type-Options"]="nosniff"; }
    private static string Support() => $"EJ-{Guid.NewGuid():N}".ToUpperInvariant();
}
