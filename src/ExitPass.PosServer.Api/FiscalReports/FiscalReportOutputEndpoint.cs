using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalReports;

namespace ExitPass.PosServer.Api.FiscalReports;

public sealed record FiscalReportOutputErrorResponse(
    bool Succeeded,
    string Code,
    string CorrelationId,
    string Message);

public sealed class FiscalReportOutputAudit;

public static class FiscalReportOutputEndpoint
{
    public static async Task<IResult> GetXPresentationAsync(
        string reference,
        FiscalXReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken = default) =>
        await GetXAsync(reference, null, null, readService, presentationService, renderer, context, logger, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<IResult> GetXExportAsync(
        string reference,
        string format,
        string? width,
        FiscalXReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken = default) =>
        await GetXAsync(reference, format, width, readService, presentationService, renderer, context, logger, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<IResult> GetZPresentationAsync(
        string reference,
        FiscalZReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken = default) =>
        await GetZAsync(reference, null, null, readService, presentationService, renderer, context, logger, cancellationToken)
            .ConfigureAwait(false);

    public static async Task<IResult> GetZExportAsync(
        string reference,
        string format,
        string? width,
        FiscalZReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken = default) =>
        await GetZAsync(reference, format, width, readService, presentationService, renderer, context, logger, cancellationToken)
            .ConfigureAwait(false);

    private static async Task<IResult> GetXAsync(
        string reference,
        string? format,
        string? width,
        FiscalXReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error(context, "missing_correlation_id", "A single safe X-Correlation-Id header is required.", "unavailable", StatusCodes.Status400BadRequest);
        if (!TryResolveOutput(format, width, out var outputFormat, out var profile, out var error))
            return Error(context, error!.Value.Code, error.Value.Message, correlation, StatusCodes.Status400BadRequest);

        var read = await readService.GetByReferenceAsync(reference, cancellationToken).ConfigureAwait(false);
        if (read.Record is null)
        {
            return read.Outcome is FiscalXReadingOutcome.PersistenceFailure or FiscalXReadingOutcome.UnknownCommitOutcome
                ? Unavailable(context, correlation, logger)
                : Hidden(context, correlation);
        }
        if (!FiscalXReadingAuthorization.IsInScope(context.User, read.Record.SitePosServerId, read.Record.FiscalIdentityId))
        {
            logger.LogWarning("Fiscal report output denied or hidden. Kind={ReportKind} Reference={ReportReference} Correlation={Correlation}", "X_READING", Safe(reference), correlation);
            return Hidden(context, correlation);
        }

        var presented = presentationService.Present(read.Record);
        return Complete(context, presented, renderer, outputFormat, profile, correlation, logger);
    }

    private static async Task<IResult> GetZAsync(
        string reference,
        string? format,
        string? width,
        FiscalZReadingService readService,
        FiscalReportPresentationService presentationService,
        FiscalReportOutputRenderer renderer,
        HttpContext context,
        ILogger<FiscalReportOutputAudit> logger,
        CancellationToken cancellationToken)
    {
        var correlation = ResolveCorrelation(context);
        if (correlation is null) return Error(context, "missing_correlation_id", "A single safe X-Correlation-Id header is required.", "unavailable", StatusCodes.Status400BadRequest);
        if (!TryResolveOutput(format, width, out var outputFormat, out var profile, out var error))
            return Error(context, error!.Value.Code, error.Value.Message, correlation, StatusCodes.Status400BadRequest);

        var read = await readService.GetByReferenceAsync(reference, cancellationToken).ConfigureAwait(false);
        if (read.Record is null)
        {
            return read.Outcome is FiscalZReadingOutcome.PersistenceFailure or FiscalZReadingOutcome.UnknownCommitOutcome
                ? Unavailable(context, correlation, logger)
                : Hidden(context, correlation);
        }
        if (!FiscalZReadingAuthorization.IsInScope(context.User, read.Record.SitePosServerId, read.Record.FiscalIdentityId, read.Record.CurrencyCode))
        {
            logger.LogWarning("Fiscal report output denied or hidden. Kind={ReportKind} Reference={ReportReference} Correlation={Correlation}", "Z_READING", Safe(reference), correlation);
            return Hidden(context, correlation);
        }

        var presented = presentationService.Present(read.Record);
        return Complete(context, presented, renderer, outputFormat, profile, correlation, logger);
    }

    private static IResult Complete(
        HttpContext context,
        FiscalReportPresentationResult result,
        FiscalReportOutputRenderer renderer,
        FiscalReportOutputFormat format,
        FiscalReportPrintWidthProfile profile,
        string correlation,
        ILogger logger)
    {
        if (!result.Succeeded || result.Presentation is null)
        {
            var response = result.ErrorCode switch
            {
                FiscalReportPresentationErrorCode.UnsupportedVersion => ("fiscal_report_presentation_version_unsupported", StatusCodes.Status409Conflict),
                FiscalReportPresentationErrorCode.NotFinalized => ("fiscal_report_not_finalized", StatusCodes.Status409Conflict),
                FiscalReportPresentationErrorCode.WrongReportKind => ("fiscal_report_not_found", StatusCodes.Status404NotFound),
                _ => ("fiscal_report_snapshot_invalid", StatusCodes.Status422UnprocessableEntity)
            };
            logger.LogWarning("Fiscal report output failed safely. Code={Code} Correlation={Correlation}", response.Item1, correlation);
            return Error(context, response.Item1, result.SafeMessage, correlation, response.Item2);
        }

        var artifact = format == FiscalReportOutputFormat.PresentationJson
            ? renderer.CreatePresentationJson(result.Presentation)
            : renderer.CreateExport(result.Presentation, format, profile);
        ApplyHeaders(context, artifact);
        logger.LogInformation(
            "Fiscal report output produced. Kind={ReportKind} Reference={ReportReference} Format={Format} OutputIdentity={OutputIdentity} Correlation={Correlation}",
            result.Presentation.ReportKind,
            result.Presentation.ReportReference,
            artifact.Format,
            artifact.OutputIdentity,
            correlation);
        return Results.Bytes(artifact.Content, artifact.ContentType);
    }

    private static bool TryResolveOutput(
        string? format,
        string? width,
        out FiscalReportOutputFormat outputFormat,
        out FiscalReportPrintWidthProfile profile,
        out (string Code, string Message)? error)
    {
        outputFormat = FiscalReportOutputFormat.PresentationJson;
        profile = FiscalReportPrintWidthProfile.Standard;
        error = null;
        if (format is null) return true;
        if (!FiscalReportOutputRenderer.TryParseFormat(format, out outputFormat))
        {
            error = ("fiscal_report_export_format_unsupported", "The requested fiscal report export format is not supported.");
            return false;
        }
        if (outputFormat == FiscalReportOutputFormat.Text && !FiscalReportOutputRenderer.TryParseWidthProfile(width, out profile))
        {
            error = ("fiscal_report_print_width_unsupported", "The requested fiscal report print width profile is not supported.");
            return false;
        }
        return true;
    }

    private static void ApplyHeaders(HttpContext context, FiscalReportOutputArtifact artifact)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.ETag = artifact.ETag;
        context.Response.Headers.ContentDisposition = artifact.ContentDisposition;
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-ExitPass-Output-Identity"] = artifact.OutputIdentity;
        context.Response.Headers["X-ExitPass-Output-Contract"] = artifact.ContractVersion;
    }

    private static IResult Hidden(HttpContext context, string correlation) =>
        Error(context, "fiscal_report_not_found", "Fiscal report was not found.", correlation, StatusCodes.Status404NotFound);

    private static IResult Unavailable(HttpContext context, string correlation, ILogger logger)
    {
        logger.LogError("Fiscal report output read failed safely. Correlation={Correlation}", correlation);
        return Error(
            context,
            "fiscal_report_output_unavailable",
            "Fiscal report output is temporarily unavailable.",
            correlation,
            StatusCodes.Status503ServiceUnavailable);
    }

    private static IResult Error(HttpContext context, string code, string message, string correlation, int status)
    {
        context.Response.Headers.CacheControl = "private, no-store";
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        return Results.Json(new FiscalReportOutputErrorResponse(false, code, correlation, message), statusCode: status);
    }

    private static string? ResolveCorrelation(HttpContext context)
    {
        var values = context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName];
        if (values.Count != 1) return null;
        var value = values[0]?.Trim();
        return string.IsNullOrWhiteSpace(value) || value.Length > 200 || value.Contains('\r') || value.Contains('\n') ? null : value;
    }

    private static string Safe(string value) =>
        string.IsNullOrWhiteSpace(value) || value.Length > 160 || value.Contains('\r') || value.Contains('\n')
            ? "unavailable"
            : value;
}
