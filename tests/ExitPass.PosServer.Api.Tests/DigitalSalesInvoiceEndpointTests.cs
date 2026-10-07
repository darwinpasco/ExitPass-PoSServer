using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Persistence.Postgres.FiscalDocuments;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class DigitalSalesInvoiceEndpointTests
{
    [Fact]
    public async Task ExistingAssignedFiscalDocumentMapsToRenderedResponse()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service);

        Assert.True(response.Succeeded);
        Assert.Equal("rendered", response.Code);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Equal("assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(document.FiscalDocumentStatusCodeId, response.FiscalDocumentStatusCodeId);
        Assert.NotNull(response.TemplateContract);
        Assert.Equal("digital-sales-invoice-json-v1", response.TemplateContract.TemplateContractVersion);
        Assert.Equal("PH_DIGITAL_SALES_INVOICE", response.TemplateContract.FiscalTemplateFamily);
        Assert.Equal("application/json", response.TemplateContract.RenderFormat);
        Assert.Equal("structured_fiscal_data_only", response.TemplateContract.DisplayResponsibility);
        Assert.Contains(response.TemplateContract.SupportedPostures, posture => posture.Name == "not_available");
        Assert.NotNull(response.Render);
        Assert.Equal(document.FiscalDocumentId, response.Render.FiscalDocumentId);
        Assert.Equal("SI-00000001-A", response.Render.FiscalDocumentNumber);
        Assert.Single(response.Render.Lines);
        Assert.Single(response.Render.Tenders);
        Assert.Single(response.Render.TaxDetails);
        Assert.Single(response.Render.Discounts);
        Assert.Single(response.Render.Totals);
        Assert.Equal(new string('a', 64), response.Render.SemanticRequestHash);
        Assert.Equal("sha256:v1", response.Render.SemanticRequestHashVersion);
        Assert.Equal("matched", response.Render.SemanticRequestHashStatus);
        Assert.Equal("placeholder_only", response.Render.Footer.RenderingStatus);
    }

    [Fact]
    public async Task AssignedFiscalDocumentIncludesRequiredTemplateSectionsAndPostures()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service);

        Assert.NotNull(response.TemplateContract);
        var sectionNames = response.TemplateContract.Sections.Select(section => section.Name).ToArray();
        Assert.Contains("header", sectionNames);
        Assert.Contains("seller_site_pos_identity", sectionNames);
        Assert.Contains("document_identity", sectionNames);
        Assert.Contains("fiscal_numbering", sectionNames);
        Assert.Contains("parking_payment_references", sectionNames);
        Assert.Contains("customer_information", sectionNames);
        Assert.Contains("line_items", sectionNames);
        Assert.Contains("discounts", sectionNames);
        Assert.Contains("taxes", sectionNames);
        Assert.Contains("vat_breakdown", sectionNames);
        Assert.Contains("tenders", sectionNames);
        Assert.Contains("totals", sectionNames);
        Assert.Contains("audit_hash_status", sectionNames);
        Assert.Contains("footer_disclaimers", sectionNames);
        Assert.Contains("deferred_placeholders", sectionNames);

        Assert.Contains(response.TemplateContract.Sections, section => section.Name == "line_items" && section.Posture == "required");
        Assert.Contains(response.TemplateContract.Sections, section => section.Name == "discounts" && section.Posture == "optional");
        Assert.Contains(response.TemplateContract.Sections, section => section.Name == "footer_disclaimers" && section.Posture == "placeholder");
        Assert.Contains(response.TemplateContract.Sections, section => section.Name == "deferred_placeholders" && section.Posture == "deferred");
        Assert.Contains(response.TemplateContract.Fields, field => field.Path == "header.templateContractVersion" && field.Posture == "required");
        Assert.Contains(response.TemplateContract.Fields, field => field.Path == "footerDisclaimers" && field.Posture == "placeholder");
        Assert.Contains(response.TemplateContract.Fields, field => field.Path == "deferredPlaceholders.pdfDocument" && field.Posture == "deferred");
    }

    [Fact]
    public async Task ExistingUnassignedFiscalDocumentMapsToNotAssignedRender()
    {
        var document = ValidReadModel(assignedNumber: false);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service);

        Assert.True(response.Succeeded);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.NotNull(response.Render);
        Assert.Equal("not_assigned", response.Render.FiscalNumberAssignmentState);
        Assert.Null(response.Render.FiscalDocumentNumber);
        Assert.Null(response.Render.FiscalSequenceValue);
        Assert.Null(response.Render.FiscalNumberAssignedAt);
        Assert.NotNull(response.TemplateContract);
        Assert.Contains(response.TemplateContract.Fields, field => field.Path == "fiscalNumbering.fiscalNumberAssignmentState" && field.Posture == "required");
        Assert.Contains(response.TemplateContract.Fields, field => field.Path == "fiscalNumbering.fiscalDocumentNumber" && field.Posture == "optional");
    }

    [Fact]
    public async Task MissingFiscalDocumentMapsToNotFound()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(null)));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(documentId, service);

        Assert.False(response.Succeeded);
        Assert.Equal("fiscal_document_not_found", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(StatusCodes.Status404NotFound, response.HttpStatusCode);
        Assert.Null(response.TemplateContract);
        Assert.Null(response.Render);
    }

    [Fact]
    public async Task PersistenceNotConfiguredFailsClosed()
    {
        var service = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new PersistenceNotConfiguredFiscalDocumentReader()));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(Guid.NewGuid(), service);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.Null(response.TemplateContract);
    }

    [Fact]
    public void DependencyInjectionRegistersRenderService()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddPosServerFiscalDocumentApi(configuration);

        using var provider = services.BuildServiceProvider();
        var service = provider.GetRequiredService<DigitalSalesInvoiceRenderService>();
        var adapter = provider.GetRequiredService<DigitalSalesInvoicePresentationAdapter>();
        Assert.NotNull(service);
        Assert.NotNull(adapter);
    }

    [Fact]
    public void EndpointRouteBuilderMapsDigitalSalesInvoiceRoute()
    {
        var source = File.ReadAllText(FindApiSourcePath("FiscalDocumentEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("/{fiscalDocumentId:guid}/digital-sales-invoice", source, StringComparison.Ordinal);
        Assert.Contains("/{fiscalDocumentId:guid}/digital-sales-invoice/presentation", source, StringComparison.Ordinal);
        Assert.Contains("DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync", source, StringComparison.Ordinal);
        Assert.Contains("DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync", source, StringComparison.Ordinal);
        Assert.Contains("Results.Json(response, statusCode: response.HttpStatusCode)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost(\"/{fiscalDocumentId:guid}/digital-sales-invoice", source, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderResponseDoesNotExposeAuthorityLeakingFields()
    {
        var forbiddenNames = new[]
        {
            "FinalizePayment",
            "PaymentLifecycle",
            "ExitAuthorization",
            "GateExecution",
            "OpenGate",
            "Refund",
            "Reversal",
            "XRead",
            "ZRead",
            "Annex"
        };
        var responseTypes = new[]
        {
            typeof(GetDigitalSalesInvoiceResponse),
            typeof(DigitalSalesInvoiceRenderModel),
            typeof(DigitalSalesInvoiceLineRenderModel),
            typeof(DigitalSalesInvoiceTenderRenderModel),
            typeof(DigitalSalesInvoiceTaxDetailRenderModel),
            typeof(DigitalSalesInvoiceDiscountRenderModel),
            typeof(DigitalSalesInvoiceTotalRenderModel),
            typeof(GetDigitalSalesInvoicePresentationResponse),
            typeof(DigitalSalesInvoicePresentationModel),
            typeof(DigitalSalesInvoicePresentationSectionModel),
            typeof(DigitalSalesInvoicePresentationRowModel),
            typeof(DigitalSalesInvoicePresentationNoticeModel)
        };

        foreach (var type in responseTypes)
        {
            foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (var forbiddenName in forbiddenNames)
                {
                    Assert.DoesNotContain(forbiddenName, member.Name, StringComparison.OrdinalIgnoreCase);
                }
            }
        }
    }

    [Fact]
    public async Task TemplateContractDoesNotExposeGeneratedPdfHtmlOrQrOutputs()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service);

        Assert.NotNull(response.TemplateContract);
        Assert.NotNull(response.Render);
        var renderProperties = typeof(DigitalSalesInvoiceRenderModel)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .ToArray();
        Assert.DoesNotContain("PdfDocument", renderProperties);
        Assert.DoesNotContain("HtmlDocument", renderProperties);
        Assert.DoesNotContain("QrCode", renderProperties);
        Assert.Contains(response.TemplateContract.DeferredPlaceholders, placeholder => placeholder.Name == "pdfDocument" && placeholder.Posture == "deferred");
        Assert.Contains(response.TemplateContract.DeferredPlaceholders, placeholder => placeholder.Name == "htmlDocument" && placeholder.Posture == "deferred");
        Assert.Contains(response.TemplateContract.DeferredPlaceholders, placeholder => placeholder.Name == "qrCode" && placeholder.Posture == "deferred");
        Assert.Contains(response.TemplateContract.DisplayResponsibilityNotes, note => note.Contains("structured fiscal data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.TemplateContract.DisplayResponsibilityNotes, note => note.Contains("renderer", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PresentationEndpointMapsAssignedFiscalDocumentToPresentationResponse()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service, adapter);

        Assert.True(response.Succeeded);
        Assert.Equal("presented", response.Code);
        Assert.Equal(StatusCodes.Status200OK, response.HttpStatusCode);
        Assert.Equal("assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(document.FiscalDocumentStatusCodeId, response.FiscalDocumentStatusCodeId);
        Assert.Equal(document.FiscalDocumentStatusCodeKey, response.FiscalDocumentStatus);
        Assert.Equal(document.FiscalDocumentTypeCodeId, response.FiscalDocumentTypeCodeId);
        Assert.Equal(document.FiscalDocumentTypeCodeKey, response.FiscalDocumentType);
        Assert.Equal(document.FiscalDocumentId, response.FiscalDocumentId);
        Assert.Equal(document.FiscalDocumentNumber, response.FiscalDocumentNumber);
        Assert.Equal(document.FiscalSeries, response.FiscalSeries);
        Assert.Equal(document.FiscalNumberPrefixText, response.FiscalNumberPrefixText);
        Assert.Equal(document.FiscalNumberSuffixText, response.FiscalNumberSuffixText);
        Assert.Equal(document.FiscalNumberAssignedAt, response.FiscalNumberAssignedAt);
        Assert.Equal(document.CreatedAt, response.RecordedAt);
        Assert.Equal("digital-sales-invoice-presentation-json-v1", response.PresentationVersion);
        Assert.Equal("digital-sales-invoice-json-v1", response.TemplateVersion);
        Assert.Equal("application/json", response.ContentType);
        Assert.NotNull(response.TemplateContract);
        Assert.NotNull(response.Presentation);
        Assert.Equal("digital-sales-invoice-presentation-json-v1", response.Presentation.PresentationVersion);
        Assert.Equal("digital-sales-invoice-json-v1", response.Presentation.SourceTemplateContractVersion);
        Assert.Equal("PH_DIGITAL_SALES_INVOICE", response.Presentation.FiscalTemplateFamily);
        Assert.Equal("application/json", response.Presentation.RenderFormat);
        Assert.Equal(
            [
                "header",
                "salesInvoiceHeaderSnapshot",
                "sellerSitePosIdentity",
                "documentIdentity",
                "fiscalNumbering",
                "parkingPaymentReferences",
                "customerInformation",
                "lineItems",
                "discounts",
                "taxes",
                "vatBreakdown",
                "tenders",
                "totals",
                "auditHashStatus",
                "footerDisclaimers",
                "deferredPlaceholders"
            ],
            response.Presentation.Sections.Select(section => section.Name).ToArray());
        Assert.Contains(
            response.Presentation.Sections.Single(section => section.Name == "fiscalNumbering").Rows,
            row => row.Key == "fiscalNumbering.fiscalDocumentNumber" &&
                   row.DisplayValue == "SI-00000001-A");
        Assert.Contains(
            response.Presentation.Sections.Single(section => section.Name == "tenders").Rows,
            row => row.Key == "tenders[0000].tenderTypeCodeKey" &&
                   row.DisplayValue == "cash");
    }

    [Fact]
    public async Task PresentationEndpointIncludesCanonicalPrintableTextFromCurrentRenderModel()
    {
        var document = ValidReadModel(assignedNumber: true) with
        {
            SalesInvoiceHeaderSnapshot = ValidHeaderSnapshot(),
            DocumentContextJson = """
                {"reference_context":{"branch_site":"PITX Level 3","ticket_number":"1474119573117","plate_number":"ABC1117","entry_time":"2026-09-29T00:42:30.7855730+00:00","payment_time":"2026-09-30T14:39:48.6742010+00:00","parking_duration":"37:57:17"}}
                """
        };
        var service = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            new DigitalSalesInvoicePresentationAdapter());

        Assert.NotNull(response.CanonicalText);
        Assert.Contains("Branch / Site", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("PITX Level 3", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("1474119573117", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("ABC1117", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("2026-09-30 22:39", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-09-30T14:39:48.6742010+00:00", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("PHT", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("37:57:17", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("CASH", response.CanonicalText, StringComparison.Ordinal);
        Assert.Null(response.CanonicalTextAuthority);
        Assert.Null(response.CanonicalTextHash);
    }

    [Fact]
    public async Task PresentationEndpointReturnsPersistedIssuanceTimeOriginalWithoutRerendering()
    {
        const string persistedOriginal = "PERSISTED ORIGINAL\r\nSI No: SI-000001\r\nNOTHING FOLLOWS\r\n";
        var document = ValidReadModel(assignedNumber: true) with
        {
            SalesInvoiceHeaderSnapshot = ValidHeaderSnapshot(),
            OriginalCanonicalText = persistedOriginal
        };
        var service = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            new DigitalSalesInvoicePresentationAdapter());

        Assert.Equal(persistedOriginal, response.CanonicalText);
        Assert.Equal("digital-sales-invoice-presentation-json-v2-persisted-original", response.PresentationVersion);
        Assert.Equal("persisted_original_electronic_journal", response.CanonicalTextAuthority);
        Assert.Equal(
            $"sha256:{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(persistedOriginal))).ToLowerInvariant()}",
            response.CanonicalTextHash);
    }

    [Fact]
    public async Task PresentationEndpointRerendersLegacyOriginalThatContainsInternalCompletionMetadata()
    {
        const string authority = "ee536680-e869-4065-9778-32e901f53784";
        var document = ValidReadModel(assignedNumber: true) with
        {
            SalesInvoiceHeaderSnapshot = ValidHeaderSnapshot(),
            CompletionBasis = FiscalCompletionBasisCodes.PaymentFinality,
            CompletionAuthorityRef = authority,
            OriginalCanonicalText = $"PERSISTED ORIGINAL\r\nCompletion Basis PAYMENT_FINALITY\r\nCompletion Authority {authority}\r\n"
        };
        var service = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            service,
            new DigitalSalesInvoicePresentationAdapter());

        Assert.True(response.Succeeded);
        Assert.DoesNotContain("Completion Basis", response.CanonicalText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Completion Authority", response.CanonicalText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(authority, response.CanonicalText, StringComparison.OrdinalIgnoreCase);
        Assert.Null(response.CanonicalTextAuthority);
        Assert.Null(response.CanonicalTextHash);
        Assert.Equal(authority, document.CompletionAuthorityRef);
        Assert.Contains(authority, document.OriginalCanonicalText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GovernedReprintReturnsCanonicalReprintTextForCompleteDocument()
    {
        var document = ValidReadModel(assignedNumber: true) with
        {
            SalesInvoiceHeaderSnapshot = ValidHeaderSnapshot(),
            OriginalCanonicalText = "ORIGINAL ISSUANCE-TIME TEXT THAT MUST NOT BECOME THE REPRINT BODY",
            DocumentContextJson = """
                {"reference_context":{"branch_site":"PITX Level 3","ticket_number":"1474119573117","plate_number":"ABC1117","entry_time":"2026-09-29T00:42:30.7855730+00:00","payment_time":"2026-09-30T14:39:48.6742010+00:00","parking_duration":"37:57:17"}}
                """
        };
        var renderService = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));
        var context = new DefaultHttpContext
        {
            User = ReprintPrincipal(document.SitePosServerId, document.FiscalIdentityId!.Value, "PHP")
        };
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] =
            "canonical-reprint-test";

        var response = await FiscalDocumentReprintEndpoint.RecordCanonicalAsync(
            document.FiscalDocumentId,
            new RecordFiscalDocumentReprintRequest(
                "canonical-reprint-operation",
                document.SitePosServerId,
                document.FiscalIdentityId,
                "PHP",
                "operator_request"),
            new FiscalDocumentReprintService(new RecordingReprintRepository(document)),
            renderService,
            context,
            new TestHost(Environments.Production));

        Assert.True(response.Succeeded);
        Assert.NotNull(response.Reprint);
        Assert.Contains("REPRINT", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("ORIGINAL", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("1474119573117", response.CanonicalText, StringComparison.Ordinal);
        Assert.Contains("2026-09-30 22:39", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-09-30T14:39:48.6742010+00:00", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("PHT", response.CanonicalText, StringComparison.Ordinal);
        Assert.DoesNotContain("ISSUANCE-TIME", response.CanonicalText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PresentationEndpointMapsUnassignedFiscalDocumentToWarningNotice()
    {
        var document = ValidReadModel(assignedNumber: false);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service, adapter);

        Assert.True(response.Succeeded);
        Assert.NotNull(response.Presentation);
        Assert.Equal("not_assigned", response.Presentation.NumberingState);
        Assert.Contains(response.Presentation.Notices, notice =>
            notice.Code == "fiscal_number_not_assigned" &&
            notice.Severity == "warning");
        Assert.Contains(
            response.Presentation.Sections.Single(section => section.Name == "fiscalNumbering").Rows,
            row => row.Key == "fiscalNumbering.fiscalDocumentNumber" &&
                   row.Posture == "not_available" &&
                   row.RawValue is null);
    }

    [Fact]
    public async Task PresentationEndpointMissingFiscalDocumentFailsClosed()
    {
        var documentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(null)));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(documentId, service, adapter);

        Assert.False(response.Succeeded);
        Assert.Equal("fiscal_document_not_found", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(StatusCodes.Status404NotFound, response.HttpStatusCode);
        Assert.Null(response.TemplateContract);
        Assert.Null(response.Presentation);
    }

    [Fact]
    public async Task PresentationEndpointDoesNotMutateOrAllocateFiscalNumbering()
    {
        var document = ValidReadModel(assignedNumber: false);
        var originalUpdatedAt = document.UpdatedAt;
        var reader = new StubFiscalDocumentReader(document);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(reader));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service, adapter);

        Assert.True(response.Succeeded);
        Assert.Equal(1, reader.ReadCount);
        Assert.Equal(originalUpdatedAt, document.UpdatedAt);
        Assert.Null(document.FiscalDocumentNumber);
        Assert.Null(document.FiscalSequenceValue);
        Assert.Null(document.FiscalNumberAssignedAt);
    }

    [Fact]
    public async Task PresentationEndpointPersistenceNotConfiguredFailsClosed()
    {
        var service = new DigitalSalesInvoiceRenderService(
            new FiscalDocumentReadService(new PersistenceNotConfiguredFiscalDocumentReader()));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(Guid.NewGuid(), service, adapter);

        Assert.False(response.Succeeded);
        Assert.Equal("persistence_not_configured", response.Code);
        Assert.Equal("not_assigned", response.FiscalNumberAssignmentState);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.HttpStatusCode);
        Assert.Null(response.TemplateContract);
        Assert.Null(response.Presentation);
    }

    [Fact]
    public async Task PresentationEndpointDoesNotExposeGeneratedPdfHtmlOrQrOutputs()
    {
        var document = ValidReadModel(assignedNumber: true);
        var service = new DigitalSalesInvoiceRenderService(new FiscalDocumentReadService(new StubFiscalDocumentReader(document)));
        var adapter = new DigitalSalesInvoicePresentationAdapter();

        var response = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(document.FiscalDocumentId, service, adapter);

        Assert.NotNull(response.Presentation);
        Assert.DoesNotContain(
            response.Presentation.Sections.SelectMany(section => section.Rows),
            row => row.Key.EndsWith(".generatedContent", StringComparison.OrdinalIgnoreCase) ||
                   row.Key.EndsWith(".documentBytes", StringComparison.OrdinalIgnoreCase));
        Assert.All(
            response.Presentation.Sections.Single(section => section.Name == "deferredPlaceholders").Rows,
            row =>
            {
                Assert.Equal("deferred", row.Posture);
                Assert.Equal("deferred", row.ValueKind);
            });
    }

    private static FiscalDocumentReadModel ValidReadModel(bool assignedNumber) =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            assignedNumber ? Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") : null,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "sales_invoice",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            assignedNumber ? "recorded" : null,
            assignedNumber ? Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff") : null,
            assignedNumber ? 1 : null,
            assignedNumber ? "SI-00000001-A" : null,
            assignedNumber ? "sales_invoice_policy" : null,
            assignedNumber ? "SI-" : null,
            assignedNumber ? "-A" : null,
            assignedNumber ? DateTimeOffset.Parse("2026-07-01T08:15:00Z") : null,
            assignedNumber ? "pos-server:system" : null,
            "fiscal_document_creation:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb:cccccccccccccccccccccccccccccccc",
            "central-finality-001",
            "upstream_finality_ref",
            new string('a', 64),
            "sha256:v1",
            "matched",
            "parking-session-001",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 7, 1),
            null,
            null,
            null,
            "{\"source_system\":\"central_pms\"}",
            true,
            DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
            DateTimeOffset.Parse("2026-07-01T08:01:00Z"),
            [],
            [],
            [
                new FiscalDocumentLineReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    1,
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    null,
                    "Parking fee",
                    1,
                    12500,
                    12500,
                    1000,
                    0,
                    11500,
                    "PHP",
                    "line-source-001",
                    "{\"source_system\":\"central_pms\",\"paymentMethod\":\"CASH\"}",
                    true,
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTenderReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000301"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "cash",
                    12500,
                    "PHP",
                    "payment-attempt-001",
                    "payment-confirmation-001",
                    "central-finality-001",
                    "provider-ref-001",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTaxDetailReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000401"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    0,
                    12500,
                    0,
                    "PHP",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalDiscountPrivilegeDetailReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000501"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("10000000-0000-0000-0000-000000000201"),
                    Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    12500,
                    1000,
                    0,
                    "PHP",
                    "beneficiary-ref-001",
                    "evidence-ref-001",
                    "discount-validation-001",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ],
            [
                new FiscalTotalReadModel(
                    Guid.Parse("10000000-0000-0000-0000-000000000601"),
                    Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                    Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    12500,
                    "PHP",
                    "{\"source_system\":\"central_pms\"}",
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"),
                    DateTimeOffset.Parse("2026-07-01T08:00:00Z"))
            ]);

    private static SalesInvoiceHeaderSnapshot ValidHeaderSnapshot() =>
        new(
            Guid.Parse("eeeeeeee-0000-0000-0000-000000000001"),
            Guid.Parse("eeeeeeee-0000-0000-0000-000000000002"),
            "v1",
            "ExitPass Parking Corporation",
            "PITX Level 3, Paranaque City",
            "123-456-789-000",
            "POS-SN-001",
            "MIN-001",
            "PITX Level 3",
            "PITX-L3-APT",
            "ACC-001",
            new DateOnly(2026, 1, 1),
            new DateOnly(2030, 1, 1),
            "PTU-001",
            new DateOnly(2026, 1, 1),
            "THIS DOCUMENT SERVES AS YOUR SALES INVOICE.",
            "Thank you.",
            "digital-sales-invoice-json-v1",
            "digital-sales-invoice-presentation-json-v1",
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
            "ExitPass Systems, Inc.",
            "Makati City",
            "987-654-321-000");

    private static string FindApiSourcePath(string fileName)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(
                current.FullName,
                "src",
                "ExitPass.PosServer.Api",
                "FiscalDocuments",
                fileName);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new FileNotFoundException($"Could not locate {fileName}.");
    }

    private static ClaimsPrincipal ReprintPrincipal(Guid sitePosServerId, Guid fiscalIdentityId, string currency) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "central-pms-test"),
            new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType,
                FiscalDocumentReprintAuthorization.RecordPermission),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, sitePosServerId.ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, fiscalIdentityId.ToString("D")),
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, currency),
            new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, "PRODUCTION")
        ], "test"));

    private sealed class RecordingReprintRepository(FiscalDocumentReadModel document) : IFiscalDocumentReprintRepository
    {
        public Task<FiscalDocumentReprintResult> RecordAsync(
            FiscalDocumentReprintCommand command,
            string semanticRequestHash,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new FiscalDocumentReprintResult(
                FiscalDocumentReprintOutcome.Created,
                new FiscalDocumentReprintRecord(
                    Guid.Parse("eeeeeeee-0000-0000-0000-000000000101"),
                    "RPR-TEST",
                    command.FiscalDocumentId,
                    document.FiscalDocumentNumber!,
                    document.FiscalSequenceValue!.Value,
                    command.SitePosServerId,
                    command.FiscalIdentityId,
                    command.CurrencyCode,
                    Guid.Parse("eeeeeeee-0000-0000-0000-000000000102"),
                    document.FiscalSequencePolicyId!.Value,
                    document.BusinessDayDate!.Value,
                    1,
                    "fiscal_document_copy",
                    "committed",
                    command.ReasonCode,
                    "RPO-TEST",
                    "print_presentation",
                    true,
                    DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
                    DateTimeOffset.Parse("2026-10-01T00:00:00Z"),
                    command.ActorReference,
                    command.ServiceIdentityReference,
                    command.CorrelationReference,
                    command.OperationKey,
                    "EJ-REPRINT-TEST")));
    }

    private sealed class TestHost(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class StubFiscalDocumentReader : IFiscalDocumentReader
    {
        private readonly FiscalDocumentReadModel? document;

        public StubFiscalDocumentReader(FiscalDocumentReadModel? document)
        {
            this.document = document;
        }

        public int ReadCount { get; private set; }

        public Task<FiscalDocumentReadModel?> GetByIdAsync(Guid fiscalDocumentId, CancellationToken cancellationToken)
        {
            ReadCount++;
            return Task.FromResult(document?.FiscalDocumentId == fiscalDocumentId ? document : null);
        }
    }
}
