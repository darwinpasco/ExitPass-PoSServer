using System.Security.Claims;
using ExitPass.PosServer.Api.FiscalDocuments;
using ExitPass.PosServer.Api.FiscalReports;
using ExitPass.PosServer.Runtime.FiscalReports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace ExitPass.PosServer.Api.Tests.FiscalReports;

public sealed class FiscalXReadingApiTests
{
    private static readonly Guid SiteId = Guid.Parse("72000000-0000-4000-8000-000000000001");
    private static readonly Guid IdentityId = Guid.Parse("72000000-0000-4000-8000-000000000002");
    private static readonly DateTimeOffset ObservedAt = DateTimeOffset.Parse("2026-08-04T11:00:00+08:00");

    [Fact]
    public void RoutesAreBoundedVersionedAndSeparatelyAuthorized()
    {
        var source = File.ReadAllText(FindSource("src", "ExitPass.PosServer.Api", "FiscalReports", "FiscalXReadingEndpointRouteBuilderExtensions.cs"));
        Assert.Contains("/v1/fiscal-reports/x-readings", source, StringComparison.Ordinal);
        Assert.Contains("MapPost", source, StringComparison.Ordinal);
        Assert.Contains("MapGet", source, StringComparison.Ordinal);
        Assert.Contains("GeneratePolicyName", source, StringComparison.Ordinal);
        Assert.Contains("ReadPolicyName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("z-reading", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("print", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("export", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ServerConfigurationDerivesPermissionsAndFiscalScopes()
    {
        var configuration = Configuration();
        var authentication = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(configuration, new StringValues("x-admin-key"), StringValues.Empty);
        Assert.True(authentication.Succeeded);
        Assert.Contains(FiscalXReadingAuthorization.GeneratePermission, authentication.Permissions);
        Assert.Contains(SiteId.ToString("D"), authentication.SitePosServerScopes);
        Assert.Contains(IdentityId.ToString("D"), authentication.FiscalIdentityScopes);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var policies = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;
        var authorization = provider.GetRequiredService<IAuthorizationService>();
        var principal = Principal(authentication);
        Assert.True((await authorization.AuthorizeAsync(principal, null, policies.GetPolicy(FiscalXReadingAuthorization.GeneratePolicyName)!)).Succeeded);
        Assert.True((await authorization.AuthorizeAsync(principal, null, policies.GetPolicy(FiscalXReadingAuthorization.ReadPolicyName)!)).Succeeded);
        Assert.True(FiscalXReadingAuthorization.IsInScope(principal, SiteId, IdentityId));
        Assert.False(FiscalXReadingAuthorization.IsInScope(principal, Guid.NewGuid(), IdentityId));
    }

    [Fact]
    public async Task OrdinaryFiscalPermissionAndForgedPermissionCannotGenerateXReading()
    {
        var configuration = Configuration();
        var ordinary = SalesInvoiceHeaderProfileAdminAuthorization.Authenticate(
            configuration,
            new StringValues("ordinary-fiscal-key"),
            new StringValues(FiscalXReadingAuthorization.GeneratePermission));
        Assert.True(ordinary.Succeeded);
        Assert.DoesNotContain(FiscalXReadingAuthorization.GeneratePermission, ordinary.Permissions);

        var services = new ServiceCollection(); services.AddLogging(); services.AddPosServerFiscalDocumentApi(configuration);
        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>().Value.GetPolicy(FiscalXReadingAuthorization.GeneratePolicyName)!;
        Assert.False((await provider.GetRequiredService<IAuthorizationService>().AuthorizeAsync(Principal(ordinary), null, policy)).Succeeded);
    }

    [Fact]
    public async Task ScopedGenerationAndReadbackReturnStoredRecordAndCorrelation()
    {
        var record = Record();
        var repository = new StubRepository(new(FiscalXReadingOutcome.Created, record));
        var service = new FiscalXReadingService(repository);
        var context = Context();

        var generated = await FiscalXReadingEndpoint.GenerateAsync(new("operation-x", SiteId, IdentityId, ObservedAt), service, context);
        var read = await FiscalXReadingEndpoint.GetAsync(record.FiscalReportReference, service, context);

        Assert.True(generated.Succeeded);
        Assert.Equal(StatusCodes.Status201Created, generated.HttpStatusCode);
        Assert.Equal("request-correlation", generated.CorrelationId);
        Assert.Same(record, generated.XReading);
        Assert.True(read.Succeeded);
        Assert.Same(record, read.XReading);
    }

    [Fact]
    public async Task MissingCorrelationAndWrongScopeFailBeforeMutation()
    {
        var repository = new StubRepository(new(FiscalXReadingOutcome.Created, Record()));
        var service = new FiscalXReadingService(repository);
        var missing = Context(); missing.Request.Headers.Clear();
        var forbidden = Context(siteScope: Guid.NewGuid());

        Assert.Equal(StatusCodes.Status400BadRequest, (await FiscalXReadingEndpoint.GenerateAsync(new("operation-x", SiteId, IdentityId, ObservedAt), service, missing)).HttpStatusCode);
        Assert.Equal(StatusCodes.Status403Forbidden, (await FiscalXReadingEndpoint.GenerateAsync(new("operation-x", SiteId, IdentityId, ObservedAt), service, forbidden)).HttpStatusCode);
        Assert.Equal(0, repository.GenerateCalls);
    }

    [Fact]
    public async Task SemanticConflictIsSafeAndDoesNotExposeHashOrSql()
    {
        var service = new FiscalXReadingService(new StubRepository(new(FiscalXReadingOutcome.Conflict, SafeMessage: "The operation is already bound.")));
        var response = await FiscalXReadingEndpoint.GenerateAsync(new("operation-x", SiteId, IdentityId, ObservedAt), service, Context());
        var serialized = System.Text.Json.JsonSerializer.Serialize(response);
        Assert.Equal(StatusCodes.Status409Conflict, response.HttpStatusCode);
        Assert.DoesNotContain("semantic_request_hash", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("constraint", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("postgres", serialized, StringComparison.OrdinalIgnoreCase);
    }

    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["PosServer:Admin:ApiKeys:0:Principal"] = "x-reading-service",
        ["PosServer:Admin:ApiKeys:0:Key"] = "x-admin-key",
        ["PosServer:Admin:ApiKeys:0:Permissions:0"] = FiscalXReadingAuthorization.GeneratePermission,
        ["PosServer:Admin:ApiKeys:0:Permissions:1"] = FiscalXReadingAuthorization.ReadPermission,
        ["PosServer:Admin:ApiKeys:0:SitePosServerIds:0"] = SiteId.ToString("D"),
        ["PosServer:Admin:ApiKeys:0:FiscalIdentityIds:0"] = IdentityId.ToString("D"),
        ["PosServer:Admin:ApiKeys:1:Principal"] = "ordinary-fiscal-service",
        ["PosServer:Admin:ApiKeys:1:Key"] = "ordinary-fiscal-key",
        ["PosServer:Admin:ApiKeys:1:Permissions:0"] = "fiscal_document.create"
    }).Build();

    private static ClaimsPrincipal Principal(PosServerAdminApiKeyAuthenticationResult authentication)
    {
        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, authentication.PrincipalName!) };
        claims.AddRange(authentication.Permissions.Select(value => new Claim(SalesInvoiceHeaderProfileAdminAuthorization.PermissionClaimType, value)));
        claims.AddRange(authentication.SitePosServerScopes.Select(value => new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, value)));
        claims.AddRange(authentication.FiscalIdentityScopes.Select(value => new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, value)));
        return new(new ClaimsIdentity(claims, SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
    }

    private static DefaultHttpContext Context(Guid? siteScope = null)
    {
        var context = new DefaultHttpContext(); context.Request.Headers[SalesInvoiceHeaderProfileAdminAuthorization.CorrelationHeaderName] = "request-correlation";
        context.User = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "x-reading-service"),
            new Claim(FiscalXReadingAuthorization.SitePosServerScopeClaimType, (siteScope ?? SiteId).ToString("D")),
            new Claim(FiscalXReadingAuthorization.FiscalIdentityScopeClaimType, IdentityId.ToString("D"))
        ], SalesInvoiceHeaderProfileAdminAuthorization.AuthenticationScheme));
        return context;
    }

    private static FiscalXReadingRecord Record() => new(Guid.NewGuid(), "X-20260804-TEST", Guid.NewGuid(), "operation-x", "X_READING", FiscalXReadingContract.ContractVersion, FiscalXReadingContract.SemanticHashVersion, SiteId, IdentityId, Guid.NewGuid(), new(2026, 8, 4), ObservedAt.AddHours(-1), ObservedAt.AddHours(10), "Asia/Manila", new(0, 0), "PHP", ObservedAt, ObservedAt, 0, new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0), [], [], [], "original-correlation", "X-20260804-TEST", true);

    private static string FindSource(params string[] parts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null) { var candidate = Path.Combine([directory.FullName, .. parts]); if (File.Exists(candidate)) return candidate; directory = directory.Parent; }
        throw new FileNotFoundException(string.Join('/', parts));
    }

    private sealed class StubRepository(FiscalXReadingResult result) : IFiscalXReadingRepository
    {
        public int GenerateCalls { get; private set; }
        public Task<FiscalXReadingResult> GenerateAsync(FiscalXReadingCommand command, CancellationToken cancellationToken = default) { GenerateCalls++; return Task.FromResult(result); }
        public Task<FiscalXReadingResult> GetByReferenceAsync(string fiscalReportReference, CancellationToken cancellationToken = default) => Task.FromResult(result with { Outcome = FiscalXReadingOutcome.Replayed });
    }
}
