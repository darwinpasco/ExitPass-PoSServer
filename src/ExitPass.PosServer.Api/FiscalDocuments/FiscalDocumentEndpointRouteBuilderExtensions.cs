using ExitPass.PosServer.Runtime.FiscalDocuments;

namespace ExitPass.PosServer.Api.FiscalDocuments;

public static class FiscalDocumentEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapFiscalDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/v1/fiscal-documents");

        group.MapPost("/", async (
            CreateFiscalDocumentRequest request,
            FiscalDocumentCreationService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentCreationEndpoint.CreateAsync(request, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentAuthorization.CreatePolicyName);

        group.MapGet("/{fiscalDocumentId:guid}", async (
            Guid fiscalDocumentId,
            FiscalDocumentReadService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentReadEndpoint.GetByIdAsync(fiscalDocumentId, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentAuthorization.ReadPolicyName);

        group.MapPost("/{fiscalDocumentId:guid}/void", async (
            Guid fiscalDocumentId,
            VoidFiscalDocumentRequest request,
            FiscalDocumentVoidService service,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentVoidEndpoint
                .VoidAsync(fiscalDocumentId, request, service, cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentAuthorization.VoidPolicyName);

        group.MapPost("/{fiscalDocumentId:guid}/reprints", async (
            Guid fiscalDocumentId,
            RecordFiscalDocumentReprintRequest request,
            FiscalDocumentReprintService service,
            HttpContext context,
            IHostEnvironment environment,
            CancellationToken cancellationToken) =>
        {
            var response = await FiscalDocumentReprintEndpoint.RecordAsync(
                fiscalDocumentId, request, service, context, environment, cancellationToken).ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentReprintAuthorization.RecordPolicyName);

        group.MapGet("/{fiscalDocumentId:guid}/digital-sales-invoice", async (
            Guid fiscalDocumentId,
            DigitalSalesInvoiceRenderService service,
            CancellationToken cancellationToken) =>
        {
            var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(
                    fiscalDocumentId,
                    service,
                    cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentAuthorization.ReadPolicyName);

        group.MapGet("/{fiscalDocumentId:guid}/digital-sales-invoice/presentation", async (
            Guid fiscalDocumentId,
            DigitalSalesInvoiceRenderService renderService,
            DigitalSalesInvoicePresentationAdapter presentationAdapter,
            CancellationToken cancellationToken) =>
        {
            var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
                    fiscalDocumentId,
                    renderService,
                    presentationAdapter,
                    cancellationToken)
                .ConfigureAwait(false);

            return Results.Json(response, statusCode: response.HttpStatusCode);
        }).RequireAuthorization(FiscalDocumentAuthorization.ReadPolicyName);

        return group;
    }

    public static IEndpointRouteBuilder MapSalesInvoiceHeaderProfileAdminEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var fiscalIdentityGroup = endpoints
            .MapGroup("/v1/admin/fiscal-identities")
            .RequireAuthorization(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName);

        fiscalIdentityGroup.MapPost("/", async (
            CreateFiscalIdentityRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .CreateFiscalIdentityAsync(request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        fiscalIdentityGroup.MapGet("/{fiscalIdentityId:guid}", async (
            Guid fiscalIdentityId,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .GetFiscalIdentityAsync(fiscalIdentityId, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        fiscalIdentityGroup.MapPatch("/{fiscalIdentityId:guid}", async (
            Guid fiscalIdentityId,
            UpdateFiscalIdentityRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .UpdateFiscalIdentityAsync(fiscalIdentityId, request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        var profileGroup = endpoints
            .MapGroup("/v1/admin/sales-invoice-header-profiles")
            .RequireAuthorization(SalesInvoiceHeaderProfileAdminAuthorization.PolicyName);

        profileGroup.MapPost("/", async (
            CreateSalesInvoiceHeaderProfileRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .CreateHeaderProfileAsync(request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapGet("/", async (
            Guid? siteId,
            Guid? sitePosServerId,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .ListHeaderProfilesAsync(siteId, sitePosServerId, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapGet("/effective-readiness", async (
            Guid siteId,
            Guid sitePosServerId,
            DateTimeOffset? effectiveAt,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .GetEffectiveReadinessAsync(siteId, sitePosServerId, effectiveAt, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapGet("/{salesInvoiceHeaderProfileId:guid}", async (
            Guid salesInvoiceHeaderProfileId,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .GetHeaderProfileAsync(salesInvoiceHeaderProfileId, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapPatch("/{salesInvoiceHeaderProfileId:guid}", async (
            Guid salesInvoiceHeaderProfileId,
            CreateSalesInvoiceHeaderProfileRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .UpdateHeaderProfileDraftAsync(salesInvoiceHeaderProfileId, request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapPost("/{salesInvoiceHeaderProfileId:guid}/validate", async (
            Guid salesInvoiceHeaderProfileId,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .ValidateHeaderProfileAsync(salesInvoiceHeaderProfileId, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapPost("/{salesInvoiceHeaderProfileId:guid}/approve", async (
            Guid salesInvoiceHeaderProfileId,
            ApproveSalesInvoiceHeaderProfileRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .ApproveHeaderProfileAsync(salesInvoiceHeaderProfileId, request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapPost("/{salesInvoiceHeaderProfileId:guid}/retire", async (
            Guid salesInvoiceHeaderProfileId,
            RetireSalesInvoiceHeaderProfileRequest request,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .RetireHeaderProfileAsync(salesInvoiceHeaderProfileId, request, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        profileGroup.MapGet("/{salesInvoiceHeaderProfileId:guid}/usage", async (
            Guid salesInvoiceHeaderProfileId,
            SalesInvoiceHeaderProfileAdminService service,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var response = await SalesInvoiceHeaderProfileAdminEndpoint
                .GetHeaderProfileUsageAsync(salesInvoiceHeaderProfileId, service, httpContext, cancellationToken)
                .ConfigureAwait(false);
            return Results.Json(response, statusCode: response.HttpStatusCode);
        });

        return endpoints;
    }
}
