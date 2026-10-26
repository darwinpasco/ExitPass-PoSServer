using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class StatutorySalesBookApiTests
{
    [Fact]
    public async Task ReadAndExportUseNarrowIndependentPermissions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(new ConfigurationBuilder().AddInMemoryCollection().Build());
        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();

        Assert.True((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ReadPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ReadPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ExportPolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(StatutorySalesBookAuthorization.ExportPermission), null,
            options.GetPolicy(StatutorySalesBookAuthorization.ExportPolicyName)!)).Succeeded);
    }

    [Theory]
    [InlineData("e2", StatutorySalesBookType.AnnexE2)]
    [InlineData("e3", StatutorySalesBookType.AnnexE3)]
    public async Task ReadUsesExactPosServerIdentityAndMonthScope(string annex, StatutorySalesBookType type)
    {
        var repository = new StubRepository(type);
        var response = await Execute(context => StatutorySalesBookEndpoint.ReadAsync(
            annex, Request(), new(repository), repository, context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(type, repository.Query!.Type);
        Assert.Equal(Site, repository.Query.SitePosServerId);
        Assert.Equal(Identity, repository.Query.FiscalIdentityId);
        Assert.Equal(2026, repository.Query.CalendarYear);
        Assert.Equal(10, repository.Query.CalendarMonth);
        Assert.Equal(1, repository.AccessCalls);
        Assert.Equal("private, no-store", response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", response.Headers.XContentTypeOptions.ToString());

        var wrongScope = await Execute(context => StatutorySalesBookEndpoint.ReadAsync(
            annex, Request() with { FiscalIdentityId = Guid.NewGuid() }, new(repository), repository, context,
            new Host(Environments.Production), default));
        Assert.Equal(StatusCodes.Status404NotFound, wrongScope.StatusCode);
        Assert.Equal(1, repository.ReadCalls);
    }

    [Theory]
    [InlineData("xlsx", StatutorySalesBookContract.XlsxContentType, ".xlsx", "PK")]
    [InlineData("pdf", StatutorySalesBookContract.PdfContentType, ".pdf", "%PDF-")]
    public async Task ExportReturnsCanonicalArtifactAndRecordsOnlySafeScope(string format, string contentType, string extension, string signature)
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var artifacts = TestFiscalReportArtifacts.CaptureService(out var artifactRepository);
        var response = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), format, new(repository), new(), artifacts, repository, context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(contentType, response.ContentType);
        Assert.Contains(extension, response.Headers.ContentDisposition.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(signature, System.Text.Encoding.ASCII.GetString(response.Body, 0, signature.Length), StringComparison.Ordinal);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(1, repository.ExportCalls);
        Assert.Equal("ANNEX_E2", repository.LastReportKind);
        Assert.Equal($"pos-server-v2/{Site:D}/{Identity:D}/PHP/2026-10", repository.LastSubjectReference);
        Assert.NotNull(artifactRepository.Command);
        Assert.Equal(StatutorySalesBookContract.ArtifactContractProfile, artifactRepository.Command.ArtifactVersion);
        Assert.Equal(repository.LastSubjectReference, artifactRepository.Command.ScopeReference);
        Assert.DoesNotContain("terminal", artifactRepository.Command.ScopeReference, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Synthetic Senior", repository.LastSubjectReference, StringComparison.Ordinal);
        Assert.Equal("private, no-store", response.Headers.CacheControl.ToString());
        Assert.Equal("nosniff", response.Headers.XContentTypeOptions.ToString());
    }

    [Fact]
    public async Task UnsupportedFormatAndWrongSiteFailBeforeReadOrAudit()
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var unsupported = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), "csv", new(repository), new(), TestFiscalReportArtifacts.Service(), repository, context, new Host(Environments.Production), default));
        var wrongSite = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request() with { SitePosServerId = Guid.NewGuid() }, "pdf", new(repository), new(), TestFiscalReportArtifacts.Service(), repository,
            context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status400BadRequest, unsupported.StatusCode);
        Assert.Equal(StatusCodes.Status404NotFound, wrongSite.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.ExportCalls);
    }

    [Fact]
    public async Task ExportFailsClosedWhenImmutableArtifactCannotBePersisted()
    {
        var repository = new StubRepository(StatutorySalesBookType.AnnexE2);
        var response = await Execute(context => StatutorySalesBookEndpoint.DownloadAsync(
            "e2", Request(), "xlsx", new(repository), new(), TestFiscalReportArtifacts.FailureService(), repository,
            context, new Host(Environments.Production), default));

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, repository.ExportCalls);
    }

    private static StatutorySalesBookRequest Request() => new(Site, Identity, "PHP", 2026, 10);

    private static StatutorySalesBookReport Report(StatutorySalesBookType type)
    {
        var generatedAt = DateTimeOffset.Parse("2026-10-17T08:30:00+08:00");
        var row = new StatutorySalesBookRow(Guid.Parse("91000000-0000-4000-8000-000000000010"), new DateOnly(2026, 10, 17), generatedAt,
            117, "Synthetic Senior", "SENIOR_CITIZEN_ID", "SC-TEST-001", null, "SI-00000117",
            10000, 1071, 0, 0, 2000, 8000, "PARANAQUE-SYNTHETIC-20", 2000);
        return new(
            new("PITX TEST", "Paranaque City", "999-999-999-000", "ExitPass POS Server", "1.3", "v1.3", "2026-10-17",
                "SN-TEST", "MIN-TEST", "PITX-L3-POS-01", generatedAt, "operator"),
            new(type, Site, Identity, "PHP", 2026, 10, "PITX-L3-POS-01", 1, 10000, 1071, 0, 0, 2000, 8000, generatedAt, []),
            [row]);
    }

    private static async Task<Capture> Execute(Func<DefaultHttpContext, Task<IResult>> action)
    {
        var context = new DefaultHttpContext();
        using var provider = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        context.RequestServices = provider;
        context.Response.Body = new MemoryStream();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "statutory-sales-book-test";
        context.User = Principal(StatutorySalesBookAuthorization.ExportPermission);
        var result = await action(context);
        await result.ExecuteAsync(context);
        return new(context.Response.StatusCode, context.Response.ContentType, context.Response.Headers,
            ((MemoryStream)context.Response.Body).ToArray());
    }

    private static ClaimsPrincipal Principal(string permission) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, "central-pms"),
        new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission),
        new Claim(BirSalesSummaryAuthorization.AuthorityClassClaimType, BirSalesSummaryAuthorization.ProductionAuthorityClass),
        new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, Site.ToString("D")),
        new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, Identity.ToString("D")),
        new Claim(FiscalZReadingAuthorization.CurrencyScopeClaimType, "PHP")
    ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));

    private static readonly Guid Site = Guid.Parse("91000000-0000-4000-8000-000000000001");
    private static readonly Guid Identity = Guid.Parse("91000000-0000-4000-8000-000000000002");
    private sealed record Capture(int StatusCode, string? ContentType, IHeaderDictionary Headers, byte[] Body);
    private sealed class Host(string name) : IHostEnvironment { public string EnvironmentName { get; set; } = name; public string ApplicationName { get; set; } = "tests"; public string ContentRootPath { get; set; } = "."; public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider(); }

    private sealed class StubRepository(StatutorySalesBookType type) : IStatutorySalesBookRepository, IAuditTrailRepository
    {
        public int ReadCalls { get; private set; }
        public int AccessCalls { get; private set; }
        public int ExportCalls { get; private set; }
        public StatutorySalesBookQuery? Query { get; private set; }
        public string? LastReportKind { get; private set; }
        public string? LastSubjectReference { get; private set; }
        public Task<StatutorySalesBookResult> ReadAsync(StatutorySalesBookQuery query, CancellationToken cancellationToken = default) { ReadCalls++; Query = query; return Task.FromResult(new StatutorySalesBookResult(StatutorySalesBookOutcome.Succeeded, Report(type))); }
        public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RecordAccessAsync(Guid sitePosServerId, string reportKind, string subjectReference, string actorReference, string correlationReference, int rowCount, CancellationToken cancellationToken = default) { AccessCalls++; LastReportKind = reportKind; LastSubjectReference = subjectReference; return Task.CompletedTask; }
        public Task RecordExportAsync(Guid sitePosServerId, string reportKind, string subjectReference, string format, string actorReference, string correlationReference, string contentSha256, int rowCount, CancellationToken cancellationToken = default) { ExportCalls++; LastReportKind = reportKind; LastSubjectReference = subjectReference; return Task.CompletedTask; }
    }
}
