using System.Text.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace ExitPass.PosServer.Api.Tests;

public sealed class FiscalDocumentReadScopeAuthorizationTests
{
    [Fact]
    public async Task ExactAuthorizedScopesPreserveAllThreeReadResponses()
    {
        var document = FiscalDocumentReadAuthorizationTestSupport.Document();
        var context = FiscalDocumentReadAuthorizationTestSupport.AuthorizedContext(
            document.SitePosServerId,
            document.FiscalIdentityId!.Value);
        var readService = new FiscalDocumentReadService(new StubReader(document));
        var renderService = new DigitalSalesInvoiceRenderService(readService);

        var documentResponse = await FiscalDocumentReadEndpoint.GetByIdAsync(
            document.FiscalDocumentId,
            readService,
            context,
            FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost);
        var invoiceResponse = await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            renderService,
            context,
            FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost);
        var presentationResponse = await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
            document.FiscalDocumentId,
            renderService,
            new DigitalSalesInvoicePresentationAdapter(),
            context,
            FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost);

        Assert.Equal(StatusCodes.Status200OK, documentResponse.HttpStatusCode);
        Assert.Equal(StatusCodes.Status200OK, invoiceResponse.HttpStatusCode);
        Assert.Equal(StatusCodes.Status200OK, presentationResponse.HttpStatusCode);
    }

    [Fact]
    public async Task IncorrectSiteIsConcealedAcrossAllThreeReadEndpoints()
    {
        var document = FiscalDocumentReadAuthorizationTestSupport.Document();
        var context = FiscalDocumentReadAuthorizationTestSupport.AuthorizedContext(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            document.FiscalIdentityId!.Value);
        var readService = new FiscalDocumentReadService(new StubReader(document));
        var renderService = new DigitalSalesInvoiceRenderService(readService);

        var responses = new object[]
        {
            await FiscalDocumentReadEndpoint.GetByIdAsync(
                document.FiscalDocumentId,
                readService,
                context,
                FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost),
            await DigitalSalesInvoiceEndpoint.GetByFiscalDocumentIdAsync(
                document.FiscalDocumentId,
                renderService,
                context,
                FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost),
            await DigitalSalesInvoicePresentationEndpoint.GetByFiscalDocumentIdAsync(
                document.FiscalDocumentId,
                renderService,
                new DigitalSalesInvoicePresentationAdapter(),
                context,
                FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost)
        };

        foreach (var response in responses)
        {
            var json = JsonSerializer.Serialize(response);
            Assert.Contains("fiscal_document_not_found", json, StringComparison.Ordinal);
            Assert.DoesNotContain("SENSITIVE CUSTOMER", json, StringComparison.Ordinal);
            Assert.DoesNotContain("sensitive-customer", json, StringComparison.Ordinal);
            Assert.DoesNotContain("12500", json, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("wrong-site", "exact-identity", null, false)]
    [InlineData("exact-site", "wrong-identity", null, false)]
    [InlineData(null, "exact-identity", null, false)]
    [InlineData("exact-site", null, null, false)]
    [InlineData("*", "exact-identity", null, false)]
    [InlineData("exact-site", "*", null, false)]
    [InlineData("exact-site", "exact-identity", null, false)]
    [InlineData("exact-site", "exact-identity", "TEST", false)]
    [InlineData("exact-site", "exact-identity", "PRODUCTION", true)]
    public void ScopeAndProductionAuthorityRulesAreExact(
        string? siteScope,
        string? fiscalIdentityScope,
        string? authorityClass,
        bool expected)
    {
        siteScope = siteScope switch
        {
            "exact-site" => FiscalDocumentReadAuthorizationTestSupport.SitePosServerId.ToString("D"),
            "wrong-site" => Guid.Parse("11111111-1111-1111-1111-111111111111").ToString("D"),
            _ => siteScope
        };
        fiscalIdentityScope = fiscalIdentityScope switch
        {
            "exact-identity" => FiscalDocumentReadAuthorizationTestSupport.FiscalIdentityId.ToString("D"),
            "wrong-identity" => Guid.Parse("22222222-2222-2222-2222-222222222222").ToString("D"),
            _ => fiscalIdentityScope
        };
        var context = FiscalDocumentReadAuthorizationTestSupport.Context(
            siteScope,
            fiscalIdentityScope,
            authorityClass);

        var actual = FiscalDocumentAuthorization.IsReadAllowed(
            context.User,
            FiscalDocumentReadAuthorizationTestSupport.ProductionHost,
            FiscalDocumentReadAuthorizationTestSupport.SitePosServerId,
            FiscalDocumentReadAuthorizationTestSupport.FiscalIdentityId);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public async Task DocumentWithMissingFiscalIdentityIsConcealed()
    {
        var document = FiscalDocumentReadAuthorizationTestSupport.Document() with { FiscalIdentityId = null };
        var service = new FiscalDocumentReadService(new StubReader(document));
        var context = FiscalDocumentReadAuthorizationTestSupport.AuthorizedContext(
            document.SitePosServerId,
            FiscalDocumentReadAuthorizationTestSupport.FiscalIdentityId);

        var response = await FiscalDocumentReadEndpoint.GetByIdAsync(
            document.FiscalDocumentId,
            service,
            context,
            FiscalDocumentReadAuthorizationTestSupport.DevelopmentHost);

        Assert.Equal(StatusCodes.Status404NotFound, response.HttpStatusCode);
        Assert.Equal("fiscal_document_not_found", response.Code);
        Assert.Null(response.Document);
    }

    private sealed class StubReader(FiscalDocumentReadModel? document) : IFiscalDocumentReader
    {
        public Task<FiscalDocumentReadModel?> GetByIdAsync(
            Guid fiscalDocumentId,
            CancellationToken cancellationToken) =>
            Task.FromResult(document?.FiscalDocumentId == fiscalDocumentId ? document : null);
    }
}
