using System.Security.Claims;
using System.Text;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class FiscalReportOutputApiTests
{
    private static readonly Guid SitePosServerId = Guid.Parse("83000000-0000-4000-8000-000000000001");
    private static readonly Guid FiscalIdentityId = Guid.Parse("83000000-0000-4000-8000-000000000002");

    [Fact]
    public void RoutesAreReadOnlyBoundedAndSeparatelyAuthorized()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Api", "FiscalReports", "FiscalReportOutputEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("/v1/fiscal-reports/x-readings", source, StringComparison.Ordinal);
        Assert.Contains("/v1/fiscal-reports/z-readings", source, StringComparison.Ordinal);
        Assert.Contains("/{fiscalReportReference}/presentation", source, StringComparison.Ordinal);
        Assert.Contains("/{zReadingReference}/presentation", source, StringComparison.Ordinal);
        Assert.Contains("/exports/{format}", source, StringComparison.Ordinal);
        Assert.Contains("XExportPolicyName", source, StringComparison.Ordinal);
        Assert.Contains("ZExportPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExportPermissionsAreServerDerivedAndDistinctFromReadAndClose()
    {
        var configuration = Configuration();
        var export = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("output-key"), StringValues.Empty);
        var readOnly = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("read-key"), StringValues.Empty);
        Assert.True(export.Succeeded);
        Assert.Contains(FiscalReportOutputAuthorization.XExportPermission, export.Permissions);
        Assert.Contains(FiscalReportOutputAuthorization.ZExportPermission, export.Permissions);
        Assert.DoesNotContain(FiscalZReadingAuthorization.ClosePermission, export.Permissions);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        Assert.IsType<FiscalReportOutputAuthorizationResultHandler>(
            provider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationMiddlewareResultHandler>());

        Assert.True((await authorization.AuthorizeAsync(Principal(export), null, options.GetPolicy(FiscalReportOutputAuthorization.XExportPolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(export), null, options.GetPolicy(FiscalReportOutputAuthorization.ZExportPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(export), null, options.GetPolicy(FiscalZReadingAuthorization.ClosePolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(readOnly), null, options.GetPolicy(FiscalReportOutputAuthorization.XExportPolicyName)!)).Succeeded);
    }

    [Fact]
    public async Task XPresentationAndExportReturnDeterministicBytesAndSafeHeaders()
    {
        var record = XRecord();
        var service = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.Replayed, record)));

        var presentation = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXPresentationAsync(
            record.FiscalReportReference, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));
        var export1 = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXExportAsync(
            record.FiscalReportReference, "csv", null, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));
        var export2 = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXExportAsync(
            record.FiscalReportReference, "csv", null, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));

        Assert.Equal(StatusCodes.Status200OK, presentation.StatusCode);
        Assert.Equal("application/json; charset=utf-8", presentation.ContentType);
        Assert.Equal("private, no-store", presentation.Headers.CacheControl);
        Assert.Equal("nosniff", presentation.Headers["X-Content-Type-Options"]);
        Assert.StartsWith("\"fiscal-report-output-sha256-v1-", presentation.Headers.ETag.ToString(), StringComparison.Ordinal);
        Assert.Equal(export1.Body, export2.Body);
        Assert.Equal(export1.Headers.ETag, export2.Headers.ETag);
        Assert.Equal("text/csv; charset=utf-8", export1.ContentType);
        Assert.Contains("attachment; filename=", export1.Headers.ContentDisposition.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("beneficiary", Encoding.UTF8.GetString(presentation.Body), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WrongScopeAndMissingReportAreIndistinguishable()
    {
        var record = XRecord();
        var found = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.Replayed, record)));
        var missing = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.NotFound)));

        var denied = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXPresentationAsync(
            record.FiscalReportReference, found, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance), Guid.NewGuid());
        var absent = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXPresentationAsync(
            record.FiscalReportReference, missing, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));

        Assert.Equal(StatusCodes.Status404NotFound, denied.StatusCode);
        Assert.Equal(absent.StatusCode, denied.StatusCode);
        Assert.Equal(Encoding.UTF8.GetString(absent.Body), Encoding.UTF8.GetString(denied.Body));
    }

    [Fact]
    public async Task UnsupportedFormatMalformedSnapshotAndPersistenceFailureFailClosed()
    {
        var valid = XRecord();
        var service = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.Replayed, valid)));
        var malformed = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.Replayed, valid with { Immutable = false })));
        var unavailable = new FiscalXReadingService(new XRepository(new(FiscalXReadingOutcome.PersistenceFailure)));

        var unsupported = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXExportAsync(
            valid.FiscalReportReference, "pdf", null, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));
        var invalid = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXPresentationAsync(
            valid.FiscalReportReference, malformed, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));
        var failed = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetXPresentationAsync(
            valid.FiscalReportReference, unavailable, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));

        Assert.Equal(StatusCodes.Status400BadRequest, unsupported.StatusCode);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, invalid.StatusCode);
        Assert.Equal(StatusCodes.Status503ServiceUnavailable, failed.StatusCode);
        Assert.DoesNotContain("constraint", Encoding.UTF8.GetString(failed.Body), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", Encoding.UTF8.GetString(failed.Body), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ZOutputRequiresCurrencyScopeAndReturnsStoredCounterFacts()
    {
        var record = ZRecord();
        var service = new FiscalZReadingService(new ZRepository(new(FiscalZReadingOutcome.Replayed, record)));
        var allowed = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetZPresentationAsync(
            record.FiscalReportReference, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance));
        var denied = await ExecuteAsync(context => FiscalReportOutputEndpoint.GetZPresentationAsync(
            record.FiscalReportReference, service, new(), new(), context, NullLogger<FiscalReportOutputAudit>.Instance), currency: "USD");

        Assert.Equal(StatusCodes.Status200OK, allowed.StatusCode);
        Assert.Contains("resultingZCounterValue\":12", Encoding.UTF8.GetString(allowed.Body), StringComparison.Ordinal);
        Assert.Equal(StatusCodes.Status404NotFound, denied.StatusCode);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PosServer:Admin:ApiKeys:0:Principal"] = "fiscal-output-service",
        ["PosServer:Admin:ApiKeys:0:Key"] = "output-key",
        ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalReportOutputAuthorization.XExportPermission,
        ["PosServer:Admin:ApiKeys:0:Permissions:1"] = FiscalReportOutputAuthorization.ZExportPermission,
        ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SitePosServerId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = FiscalIdentityId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:CurrencyCodes:0"] = "PHP",
        ["PosServer:Admin:ApiKeys:1:Principal"] = "read-service",
        ["PosServer:Admin:ApiKeys:1:Key"] = "read-key",
        ["PosServer:Admin:ApiKeys:1:Permissions:0"] = FiscalXReadingAuthorization.ReadPermission,
        ["PosServer:Admin:ApiKeys:1:SitePosServerIds:0"] = SitePosServerId.ToString("D"),
        ["PosServer:Admin:ApiKeys:1:FiscalIdentityIds:0"] = FiscalIdentityId.ToString("D")
    }).Build();

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!) };
        claims.AddRange(authentication.Permissions.Select(value => new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value => new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value => new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        claims.AddRange(authentication.CurrencyScopes.Select(value => new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private static async Task<ResponseCapture> ExecuteAsync(
        Func<DefaultHttpContext, Task<IResult>> action,
        Guid? siteScope = null,
        string currency = "PHP")
    {
        var context = new DefaultHttpContext();
        using var requestServices = new ServiceCollection()
            .AddLogging()
            .AddRouting()
            .BuildServiceProvider();
        context.RequestServices = requestServices;
        context.Response.Body = new MemoryStream();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "output-correlation";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "fiscal-output-service"),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, (siteScope ?? SitePosServerId).ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, FiscalIdentityId.ToString("D")),
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, currency)
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
        var result = await action(context);
        await result.ExecuteAsync(context);
        return new(context.Response.StatusCode, context.Response.ContentType, context.Response.Headers, ((MemoryStream)context.Response.Body).ToArray());
    }

    private static FiscalXReadingRecord XRecord() => new(
        Guid.Parse("83000000-0000-4000-8000-000000000010"), "X-20260806-0001",
        Guid.Parse("83000000-0000-4000-8000-000000000011"), "operation-x", "X_READING",
        FiscalXReadingContract.ContractVersion, FiscalXReadingContract.SemanticHashVersion,
        SitePosServerId, FiscalIdentityId, Guid.Parse("83000000-0000-4000-8000-000000000012"),
        new(2026, 8, 6), DateTimeOffset.Parse("2026-08-05T16:00:00Z"), DateTimeOffset.Parse("2026-08-06T16:00:00Z"),
        "Asia/Manila", new(0, 0), "PHP", DateTimeOffset.Parse("2026-08-06T08:00:00Z"), DateTimeOffset.Parse("2026-08-06T08:00:01Z"),
        1, Amounts(), [new("cash", 1, 10_000, "PHP")], [new("senior_citizen_statutory", 1, 1_500, 0, "PHP"), new("vat_exemption_adjustment", 1, 0, 500, "PHP")],
        [new(Guid.Parse("83000000-0000-4000-8000-000000000013"), "SI", 1, 1, "SI-0001", "SI-0001", 1, [], "PHP")],
        "stored-correlation", "X-20260806-0001", true, 7, "COMMITTED");

    private static FiscalZReadingRecord ZRecord()
    {
        var x = XRecord();
        return new(
            Guid.Parse("83000000-0000-4000-8000-000000000020"), "Z-20260806-00000012",
            Guid.Parse("83000000-0000-4000-8000-000000000021"), "operation-z", "Z_READING",
            FiscalZReadingContract.ContractVersion, FiscalZReadingContract.SemanticHashVersion,
            x.SitePosServerId, x.FiscalIdentityId, x.FiscalReportingPeriodId, null, x.BusinessDayDate,
            x.PeriodStartAt, x.PeriodEndAt, x.ReportingTimezoneName, x.BusinessDayCutoffLocalTime, x.CurrencyCode,
            x.PeriodEndAt, x.PeriodEndAt.AddSeconds(1), x.PeriodEndAt.AddSeconds(1), x.QualifyingDocumentCount,
            x.Amounts, x.Tenders, x.Discounts, x.FiscalNumberRanges, new(3, 3, 11, 12, 50_000, 10_000, 60_000, 9, 10),
            "CLOSED", "stored-z-correlation", "Z-20260806-00000012", true, 7, "COMMITTED");
    }

    private static FiscalXReadingAmounts Amounts() => new(12_000, 10_000, 8_000, 960, 2_000, 0, 2_000, 1_500, 0, 0, 500, 0, 0, 0, 0, 0, 0, 0);

    private static string FindSource(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine([directory.FullName, .. parts]);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(string.Join('/', parts));
    }

    private sealed record ResponseCapture(int StatusCode, string? ContentType, IHeaderDictionary Headers, byte[] Body);

    private sealed class XRepository(FiscalXReadingResult result) : IFiscalXReadingRepository
    {
        public Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(result);
        public Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }

    private sealed class ZRepository(FiscalZReadingResult result) : IFiscalZReadingRepository
    {
        public Task<FiscalZReadingResult> CloseAsync(FiscalZReadingCommand command, CancellationToken cancellationToken = default) => Task.FromResult(result);
        public Task<FiscalZReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(result);
    }
}
