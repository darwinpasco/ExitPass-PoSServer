using System.Security.Claims;
using System.Text.Json;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class CloseableFiscalBusinessDateApiTests
{
    private static readonly Guid SiteId = Guid.Parse("79000000-0000-4000-8000-000000000101");
    private static readonly Guid OtherSiteId = Guid.Parse("79000000-0000-4000-8000-000000000102");
    private static readonly Guid IdentityId = Guid.Parse("79000000-0000-4000-8000-000000000103");
    private static readonly Guid PeriodId = Guid.Parse("79000000-0000-4000-8000-000000000104");

    [Fact]
    public void RouteIsReadOnlySingleScopeAndUsesExistingZReadAuthorization()
    {
        var source = File.ReadAllText(FindSource(
            "src", "ExitPass.PosServer.Api", "FiscalReports", "FiscalZReadingEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("/closeable-periods", source, StringComparison.Ordinal);
        Assert.Contains("CloseableFiscalBusinessDateEndpoint.ReadAsync", source, StringComparison.Ordinal);
        Assert.Contains("FiscalZReadingAuthorization.ReadPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("siteIds", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("allSites", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("siteGroupId", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EndpointReturnsAuthoritativeScopeAndPeriodContract()
    {
        var period = new CloseableFiscalBusinessDate(
            PeriodId,
            new DateOnly(2026, 9, 20),
            DateTimeOffset.Parse("2026-09-19T23:00:00Z"),
            DateTimeOffset.Parse("2026-09-20T23:00:00Z"),
            "OPEN",
            4,
            9);
        var repository = new StubRepository(new(
            SiteId,
            IdentityId,
            "PHP",
            [period],
            "closeable_fiscal_business_dates_read"));
        var service = new CloseableFiscalBusinessDateService(repository);

        var response = await ExecuteAsync(context => CloseableFiscalBusinessDateEndpoint.ReadAsync(
            SiteId, IdentityId, "PHP", service, context, CancellationToken.None));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal("private, no-store", response.CacheControl);
        using var json = JsonDocument.Parse(response.Body);
        Assert.Equal(SiteId, json.RootElement.GetProperty("sitePosServerId").GetGuid());
        Assert.Equal(IdentityId, json.RootElement.GetProperty("fiscalIdentityId").GetGuid());
        Assert.Equal("PHP", json.RootElement.GetProperty("currencyCode").GetString());
        var item = Assert.Single(json.RootElement.GetProperty("fiscalBusinessDates").EnumerateArray());
        Assert.Equal(PeriodId, item.GetProperty("fiscalReportingPeriodId").GetGuid());
        Assert.Equal("2026-09-20", item.GetProperty("fiscalBusinessDate").GetString());
        Assert.Equal(4, item.GetProperty("transactionCount").GetInt64());
        Assert.Equal(9, item.GetProperty("expectedStateVersion").GetInt64());
    }

    [Fact]
    public async Task EndpointRejectsAnotherSiteBeforePersistence()
    {
        var repository = new StubRepository(new(
            SiteId,
            IdentityId,
            "PHP",
            [],
            "closeable_fiscal_business_dates_read"));
        var service = new CloseableFiscalBusinessDateService(repository);

        var response = await ExecuteAsync(context => CloseableFiscalBusinessDateEndpoint.ReadAsync(
            OtherSiteId, IdentityId, "PHP", service, context, CancellationToken.None));

        Assert.Equal(StatusCodes.Status404NotFound, response.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
    }

    private static async Task<ResponseCapture> ExecuteAsync(Func<DefaultHttpContext, Task<IResult>> action)
    {
        var context = new DefaultHttpContext();
        using var requestServices = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        context.RequestServices = requestServices;
        context.Response.Body = new MemoryStream();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "closeable-period-reader"),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, SiteId.ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, IdentityId.ToString("D")),
            new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, "PHP")
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));

        var result = await action(context);
        await result.ExecuteAsync(context);
        return new(
            context.Response.StatusCode,
            context.Response.Headers.CacheControl.ToString(),
            ((MemoryStream)context.Response.Body).ToArray());
    }

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

    private sealed class StubRepository(CloseableFiscalBusinessDatesSnapshot result)
        : ICloseableFiscalBusinessDateRepository
    {
        public int ReadCalls { get; private set; }

        public Task<CloseableFiscalBusinessDatesSnapshot> ReadAsync(
            Guid sitePosServerId,
            Guid fiscalIdentityId,
            string currencyCode,
            CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            return Task.FromResult(result);
        }
    }

    private sealed record ResponseCapture(int StatusCode, string CacheControl, byte[] Body);
}
