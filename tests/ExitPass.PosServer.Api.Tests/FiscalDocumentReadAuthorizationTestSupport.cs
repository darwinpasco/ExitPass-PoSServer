using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalDocuments;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace ExitPass.PosServer.Api.Tests;

internal static class FiscalDocumentReadAuthorizationTestSupport
{
    public static readonly Guid SitePosServerId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid FiscalIdentityId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    public static HttpContext AuthorizedContext(Guid sitePosServerId, Guid fiscalIdentityId) =>
        Context(sitePosServerId.ToString("D"), fiscalIdentityId.ToString("D"));

    public static HttpContext Context(string? siteScope, string? fiscalIdentityScope, string? authorityClass = null)
    {
        var claims = new List<Claim>();
        if (siteScope is not null)
        {
            claims.Add(new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, siteScope));
        }
        if (fiscalIdentityScope is not null)
        {
            claims.Add(new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, fiscalIdentityScope));
        }
        if (authorityClass is not null)
        {
            claims.Add(new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, authorityClass));
        }

        return new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"))
        };
    }

    public static IHostEnvironment DevelopmentHost { get; } = new TestHost(Environments.Development);
    public static IHostEnvironment ProductionHost { get; } = new TestHost(Environments.Production);

    public static FiscalDocumentReadModel Document() =>
        new(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            SitePosServerId,
            Guid.Parse("99999999-9999-9999-9999-999999999999"),
            FiscalIdentityId,
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            "sales_invoice",
            Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            "recorded",
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            1,
            "SI-00000001-A",
            "SI",
            "SI-",
            "-A",
            DateTimeOffset.Parse("2026-11-17T00:00:00Z"),
            "pos-server:test",
            "fiscal_document_creation:test",
            "sensitive-customer-idempotency-key",
            "upstream_finality_ref",
            new string('a', 64),
            "sha256:v1",
            "matched",
            "sensitive-customer-parking-reference",
            "payment-attempt-001",
            "payment-confirmation-001",
            "central-finality-001",
            "vendor-ack-001",
            new DateOnly(2026, 11, 17),
            null,
            null,
            null,
            "{\"customer\":\"SENSITIVE CUSTOMER\",\"amount\":12500}",
            true,
            DateTimeOffset.Parse("2026-11-17T00:00:00Z"),
            DateTimeOffset.Parse("2026-11-17T00:00:00Z"),
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private sealed class TestHost(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "ExitPass.PosServer.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
