using System.Security.Claims;
using System.Text;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class AuditTrailApiTests
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

        Assert.True((await authorization.AuthorizeAsync(Principal(AuditTrailAuthorization.ReadPermission), null,
            options.GetPolicy(AuditTrailAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.False((await authorization.AuthorizeAsync(Principal(AuditTrailAuthorization.ReadPermission), null,
            options.GetPolicy(AuditTrailAuthorization.ExportPolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(Principal(AuditTrailAuthorization.ExportPermission), null,
            options.GetPolicy(AuditTrailAuthorization.ExportPolicyName)!)).Succeeded);
    }

    [Fact]
    public async Task ScopeAndRangeAreValidatedBeforeTheRepositoryIsRead()
    {
        var repository = new StubRepository();
        var service = new AuditTrailService(repository);
        var wrongSite = await Execute(context => AuditTrailEndpoint.ReadAsync(
            new(Guid.NewGuid(), From, To, null, null, null, null), service, TestFiscalReportArtifacts.Service(), context, default));
        var unbounded = await Execute(context => AuditTrailEndpoint.ReadAsync(
            new(Site, null, null, null, null, null, null), service, TestFiscalReportArtifacts.Service(), context, default));
        var success = await Execute(context => AuditTrailEndpoint.ReadAsync(
            new(Site, From, To, "fiscal_document_committed", "System", "ALLOWED", "SI-00000116"), service, TestFiscalReportArtifacts.Service(), context, default));

        Assert.Equal(StatusCodes.Status404NotFound, wrongSite.StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, unbounded.StatusCode);
        Assert.Equal(StatusCodes.Status200OK, success.StatusCode);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal("SI-00000116", repository.Query!.Reference);
    }

    [Theory]
    [InlineData("json", "application/json; charset=utf-8", ".json")]
    [InlineData("csv", "text/csv; charset=utf-8", ".csv")]
    [InlineData("pdf", "application/pdf", ".pdf")]
    public async Task ExportFreezesBytesBeforeRecordingItsOwnAccessAudit(string format, string contentType, string extension)
    {
        var repository = new StubRepository();
        var response = await Execute(context => AuditTrailEndpoint.ExportAsync(
            new(Site, From, To, null, null, null, null), format, new(repository), new(), TestFiscalReportArtifacts.Service(), repository, context, default));

        Assert.Equal(StatusCodes.Status200OK, response.StatusCode);
        Assert.Equal(contentType, response.ContentType);
        Assert.Contains(extension, response.Headers.ContentDisposition.ToString(), StringComparison.Ordinal);
        Assert.Equal(1, repository.ReadCalls);
        Assert.Equal(1, repository.ExportCalls);
        Assert.Equal(repository.FrozenSha, response.Headers["X-Content-SHA256"].ToString());
        Assert.Equal(repository.FrozenSha, repository.RecordedSha);
    }

    [Fact]
    public async Task UnsupportedFormatFailsBeforeReadOrWrite()
    {
        var repository = new StubRepository();
        var response = await Execute(context => AuditTrailEndpoint.ExportAsync(
            new(Site, From, To, null, null, null, null), "xml", new(repository), new(), TestFiscalReportArtifacts.Service(), repository, context, default));
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(0, repository.ReadCalls);
        Assert.Equal(0, repository.ExportCalls);
    }

    private static async Task<Capture> Execute(Func<DefaultHttpContext, Task<IResult>> action)
    {
        var context = new DefaultHttpContext();
        using var provider = new ServiceCollection().AddLogging().AddRouting().BuildServiceProvider();
        context.RequestServices = provider;
        context.Response.Body = new MemoryStream();
        context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "audit-correlation";
        context.User = Principal(AuditTrailAuthorization.ExportPermission);
        var result = await action(context);
        await result.ExecuteAsync(context);
        return new(context.Response.StatusCode, context.Response.ContentType, context.Response.Headers,
            ((MemoryStream)context.Response.Body).ToArray());
    }

    private static ClaimsPrincipal Principal(string permission) => new(new ClaimsIdentity([
        new Claim(ClaimTypes.NameIdentifier, "central-pms"),
        new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, permission),
        new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, Site.ToString("D"))
    ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));

    private static AuditTrailReport Report() => new(AuditTrailContract.ContractVersion, Site, From, To,
        new("Registered Name", "Registered Address", "999-999-999-000", "MIN-1", "SN-1", "PITX"),
        [new("x_reading_committed", "X READING", "X Reading committed")], []);

    private static readonly Guid Site = Guid.Parse("a2000000-0000-4000-8000-000000000001");
    private static readonly DateTimeOffset From = DateTimeOffset.Parse("2026-10-16T16:00:00Z");
    private static readonly DateTimeOffset To = DateTimeOffset.Parse("2026-10-17T16:00:00Z");
    private sealed record Capture(int StatusCode, string? ContentType, IHeaderDictionary Headers, byte[] Body);

    private sealed class StubRepository : IAuditTrailRepository
    {
        public int ReadCalls { get; private set; }
        public int ExportCalls { get; private set; }
        public AuditTrailQuery? Query { get; private set; }
        public string? FrozenSha { get; private set; }
        public string? RecordedSha { get; private set; }

        public Task<AuditTrailResult> ReadAsync(AuditTrailQuery query, CancellationToken cancellationToken = default)
        {
            ReadCalls++;
            Query = query;
            return Task.FromResult(new AuditTrailResult(AuditTrailOutcome.Succeeded, Report()));
        }

        public Task RecordAccessAsync(Guid sitePosServerId, string reportKind, string subjectReference,
            string actorReference, string correlationReference, int rowCount,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task RecordExportAsync(Guid sitePosServerId, string reportKind, string subjectReference, string format,
            string actorReference, string correlationReference, string contentSha256, int rowCount,
            CancellationToken cancellationToken = default)
        {
            ExportCalls++;
            FrozenSha ??= contentSha256;
            RecordedSha = contentSha256;
            return Task.CompletedTask;
        }
    }
}
