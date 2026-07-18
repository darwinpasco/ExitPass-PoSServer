using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class SalesInvoiceHeaderProfileAdminEndpoint
{
    public static async Task<SalesInvoiceHeaderProfileAdminResponse> CreateFiscalIdentityAsync(
        CreateFiscalIdentityRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var identity = await service.CreateFiscalIdentityAsync(
                new CreateFiscalIdentityProfileCommand(
                    request.RegisteredBusinessName ?? string.Empty,
                    request.RegisteredBusinessAddress ?? string.Empty,
                    request.Tin ?? string.Empty,
                    request.TaxpayerClassification,
                    request.Status,
                    request.ActorRef ?? string.Empty,
                    DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
            return Success("fiscal_identity_created", StatusCodes.Status201Created, correlationId, identity);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> GetFiscalIdentityAsync(
        Guid fiscalIdentityId,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        var identity = await service.GetFiscalIdentityAsync(fiscalIdentityId, cancellationToken).ConfigureAwait(false);
        return identity is null
            ? Error("fiscal_identity_not_found", StatusCodes.Status404NotFound, correlationId, "Fiscal Identity was not found.")
            : Success("fiscal_identity_found", StatusCodes.Status200OK, correlationId, identity);
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> UpdateFiscalIdentityAsync(
        Guid fiscalIdentityId,
        UpdateFiscalIdentityRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var identity = await service.UpdateFiscalIdentityAsync(
                new UpdateFiscalIdentityProfileCommand(
                    fiscalIdentityId,
                    request.RegisteredBusinessName ?? string.Empty,
                    request.RegisteredBusinessAddress ?? string.Empty,
                    request.Tin ?? string.Empty,
                    request.TaxpayerClassification,
                    request.Status,
                    request.ActorRef ?? string.Empty,
                    DateTimeOffset.UtcNow),
                cancellationToken).ConfigureAwait(false);
            return Success("fiscal_identity_updated", StatusCodes.Status200OK, correlationId, identity);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> CreateHeaderProfileAsync(
        CreateSalesInvoiceHeaderProfileRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var profile = await service.CreateHeaderProfileAsync(ToCreateCommand(request), cancellationToken).ConfigureAwait(false);
            return Success("sales_invoice_header_profile_created", StatusCodes.Status201Created, correlationId, profile);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> GetHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        var profile = await service.GetHeaderProfileAsync(salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false);
        return profile is null
            ? Error("sales_invoice_header_profile_not_found", StatusCodes.Status404NotFound, correlationId, "Sales Invoice header profile was not found.")
            : Success("sales_invoice_header_profile_found", StatusCodes.Status200OK, correlationId, profile);
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> ListHeaderProfilesAsync(
        Guid? siteId,
        Guid? sitePosServerId,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        var profiles = await service.ListHeaderProfilesAsync(siteId, sitePosServerId, cancellationToken).ConfigureAwait(false);
        return Success("sales_invoice_header_profiles_found", StatusCodes.Status200OK, correlationId, profiles);
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> UpdateHeaderProfileDraftAsync(
        Guid salesInvoiceHeaderProfileId,
        CreateSalesInvoiceHeaderProfileRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var profile = await service.UpdateHeaderProfileDraftAsync(ToUpdateCommand(salesInvoiceHeaderProfileId, request), cancellationToken).ConfigureAwait(false);
            return Success("sales_invoice_header_profile_updated", StatusCodes.Status200OK, correlationId, profile);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> ValidateHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var validation = await service.ValidateHeaderProfileAsync(salesInvoiceHeaderProfileId, DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            return Success("sales_invoice_header_profile_validated", StatusCodes.Status200OK, correlationId, validation);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> ApproveHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        ApproveSalesInvoiceHeaderProfileRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var profile = await service.ApproveHeaderProfileAsync(
                salesInvoiceHeaderProfileId,
                request.ApprovedAt ?? DateTimeOffset.UtcNow,
                request.ApprovedByRef ?? string.Empty,
                cancellationToken).ConfigureAwait(false);
            return Success("sales_invoice_header_profile_approved", StatusCodes.Status200OK, correlationId, profile);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> RetireHeaderProfileAsync(
        Guid salesInvoiceHeaderProfileId,
        RetireSalesInvoiceHeaderProfileRequest request,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        try
        {
            var profile = await service.RetireHeaderProfileAsync(
                salesInvoiceHeaderProfileId,
                request.RetiredAt ?? DateTimeOffset.UtcNow,
                request.RetiredByRef ?? string.Empty,
                cancellationToken).ConfigureAwait(false);
            return Success("sales_invoice_header_profile_retired", StatusCodes.Status200OK, correlationId, profile);
        }
        catch (SalesInvoiceHeaderProfileAdminException ex)
        {
            return Error(ex.Code, ToStatusCode(ex.Code), correlationId, ex.Message);
        }
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> GetEffectiveReadinessAsync(
        Guid siteId,
        Guid sitePosServerId,
        DateTimeOffset? effectiveAt,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        var readiness = await service.GetEffectiveReadinessAsync(
            siteId,
            sitePosServerId,
            effectiveAt ?? DateTimeOffset.UtcNow,
            cancellationToken).ConfigureAwait(false);
        return Success("sales_invoice_header_profile_readiness_reported", StatusCodes.Status200OK, correlationId, readiness);
    }

    public static async Task<SalesInvoiceHeaderProfileAdminResponse> GetHeaderProfileUsageAsync(
        Guid salesInvoiceHeaderProfileId,
        SalesInvoiceHeaderProfileAdminService service,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetCorrelationId(httpContext, out var correlationId, out var failure))
        {
            return failure;
        }

        var usage = await service.GetHeaderProfileUsageAsync(salesInvoiceHeaderProfileId, cancellationToken).ConfigureAwait(false);
        return Success("sales_invoice_header_profile_usage_reported", StatusCodes.Status200OK, correlationId, usage);
    }

    private static bool TryGetCorrelationId(
        HttpContext httpContext,
        out string correlationId,
        out SalesInvoiceHeaderProfileAdminResponse failure)
    {
        correlationId = httpContext.Request.Headers.TryGetValue(SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName, out var value)
            ? value.ToString().Trim()
            : string.Empty;
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            failure = Error("missing_correlation_id", StatusCodes.Status400BadRequest, string.Empty, "X-Correlation-Id is required.");
            return false;
        }

        failure = null!;
        return true;
    }

    private static CreateSalesInvoiceHeaderProfileCommand ToCreateCommand(CreateSalesInvoiceHeaderProfileRequest request) =>
        new(
            request.FiscalIdentityId,
            request.SiteId,
            request.SitePosServerId,
            request.ProfileVersion ?? string.Empty,
            request.TemplateVersion ?? string.Empty,
            request.PresentationVersion ?? string.Empty,
            request.PosSerialNumber,
            request.MachineIdentificationNumber,
            request.ParkingLocationDisplay,
            request.BirAccreditationNumber,
            request.BirAccreditationIssuedDate,
            request.BirAccreditationValidUntil,
            request.PtuNumber,
            request.PtuIssuedDate,
            request.SalesInvoiceLegalStatement,
            request.CustomerServiceFooter,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.CreatedByRef ?? request.ActorRef ?? string.Empty,
            DateTimeOffset.UtcNow);

    private static UpdateSalesInvoiceHeaderProfileDraftCommand ToUpdateCommand(Guid id, CreateSalesInvoiceHeaderProfileRequest request) =>
        new(
            id,
            request.FiscalIdentityId,
            request.SiteId,
            request.SitePosServerId,
            request.ProfileVersion ?? string.Empty,
            request.TemplateVersion ?? string.Empty,
            request.PresentationVersion ?? string.Empty,
            request.PosSerialNumber,
            request.MachineIdentificationNumber,
            request.ParkingLocationDisplay,
            request.BirAccreditationNumber,
            request.BirAccreditationIssuedDate,
            request.BirAccreditationValidUntil,
            request.PtuNumber,
            request.PtuIssuedDate,
            request.SalesInvoiceLegalStatement,
            request.CustomerServiceFooter,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.UpdatedByRef ?? request.ActorRef ?? string.Empty,
            DateTimeOffset.UtcNow);

    private static SalesInvoiceHeaderProfileAdminResponse Success(string code, int statusCode, string correlationId, object? resource) =>
        new(true, code, statusCode, correlationId, resource, []);

    private static SalesInvoiceHeaderProfileAdminResponse Error(string code, int statusCode, string correlationId, params string[] errors) =>
        new(false, code, statusCode, correlationId, null, errors);

    private static int ToStatusCode(string code) =>
        code switch
        {
            "fiscal_identity_not_found" or "sales_invoice_header_profile_not_found" => StatusCodes.Status404NotFound,
            "persistence_not_configured" => StatusCodes.Status503ServiceUnavailable,
            "registered_business_name_missing" or
            "registered_business_address_missing" or
            "tin_missing" or
            "created_by_ref_required" or
            "updated_by_ref_required" or
            "approved_by_ref_required" or
            "retired_by_ref_required" or
            "profile_version_missing" or
            "template_version_missing" or
            "presentation_version_missing" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status409Conflict
        };
}

public sealed record SalesInvoiceHeaderProfileAdminResponse(
    bool Succeeded,
    string Code,
    int HttpStatusCode,
    string CorrelationId,
    object? Resource,
    IReadOnlyList<string> Errors);

public sealed record CreateFiscalIdentityRequest(
    string? RegisteredBusinessName,
    string? RegisteredBusinessAddress,
    string? Tin,
    string? TaxpayerClassification,
    string? Status,
    string? ActorRef);

public sealed record UpdateFiscalIdentityRequest(
    string? RegisteredBusinessName,
    string? RegisteredBusinessAddress,
    string? Tin,
    string? TaxpayerClassification,
    string? Status,
    string? ActorRef);

public sealed record CreateSalesInvoiceHeaderProfileRequest(
    Guid FiscalIdentityId,
    Guid SiteId,
    Guid SitePosServerId,
    string? ProfileVersion,
    string? TemplateVersion,
    string? PresentationVersion,
    string? PosSerialNumber,
    string? MachineIdentificationNumber,
    string? ParkingLocationDisplay,
    string? BirAccreditationNumber,
    DateOnly? BirAccreditationIssuedDate,
    DateOnly? BirAccreditationValidUntil,
    string? PtuNumber,
    DateOnly? PtuIssuedDate,
    string? SalesInvoiceLegalStatement,
    string? CustomerServiceFooter,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? CreatedByRef,
    string? UpdatedByRef = null,
    string? ActorRef = null);

public sealed record ApproveSalesInvoiceHeaderProfileRequest(
    string? ApprovedByRef,
    DateTimeOffset? ApprovedAt = null);

public sealed record RetireSalesInvoiceHeaderProfileRequest(
    string? RetiredByRef,
    DateTimeOffset? RetiredAt = null);
